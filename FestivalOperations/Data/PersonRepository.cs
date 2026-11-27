using System.Data;
using Microsoft.Data.SqlClient;

namespace FestivalOperations.Data;
/// <summary>
/// Carries the basic person fields used by list and lookup operations.
/// </summary>
public sealed record PersonSummary(
    int PersonID,
    string Name,
    string Role
);
/// <summary>
/// Carries contact information and formatted role details for display in the console.
/// </summary>

public sealed record PersonRoleSummary(
    int PersonID,
    string Name,
    string Telephone,
    string Email,
    string Role,
    string RoleDetails
);
/// <summary>
/// Runs SQL commands for people and their role-specific records; business validation belongs to the service and models.
/// </summary>
public sealed class PersonRepository
{
    private readonly SqlConnection _connection;

    /// <summary>Creates the repository using the open SQL connection owned by the application.</summary>
    /// <param name="connection">The connection shared by repository commands.</param>
    public PersonRepository(SqlConnection connection)
    {
        _connection = connection;
    }

    /// <summary>Loads the ID, name, and role for every person in ID order.</summary>
    /// <returns>A list of basic person summaries; an empty list when the table has no rows.</returns>
    /// <remarks>The selected column order matches the PersonSummary constructor below.</remarks>
    /// <remarks>Ordering by the primary key makes repeated listings stable and predictable.</remarks>
    public async Task<IReadOnlyList<PersonSummary>> GetAllPeopleAsync()
    {
        const string sql = """
            SELECT PersonID, Name, Role
            FROM dbo.People
            ORDER BY PersonID;
            """;

        var people = new List<PersonSummary>();

        // Read rows one at a time and map the selected columns into the display model.
        await using var command = new SqlCommand(sql, _connection);
        // Keep the reader scoped to this method so the shared connection can be reused afterward.
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            people.Add(new PersonSummary(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2)
            ));
        }

        return people;
    }

    /// <summary>Finds people whose names contain the supplied search text.</summary>
    /// <param name="searchTerm">The partial name to search for.</param>
    /// <returns>The matching summaries, or an empty list when no names match.</returns>
    /// <remarks>The query returns the same compact summary shape used by the all-people listing.</remarks>
    /// <remarks>A partial match allows the user to search without entering a complete name.</remarks>
    public async Task<IReadOnlyList<PersonSummary>> SearchPeopleByNameAsync(string searchTerm)
    {
        // CHARINDEX checks for a substring, and the collation makes the name match case-insensitive.
        const string sql = """
            SELECT PersonID, Name, Role
            FROM dbo.People
            WHERE CHARINDEX(
                @SearchTerm,
                Name COLLATE Latin1_General_100_CI_AS 
            ) > 0
            ORDER BY PersonID;
            """;
        
        var people = new List<PersonSummary>();

        // Parameterization treats the search text as data instead of executable SQL.
        await using var command = new SqlCommand(sql, _connection);
        command.Parameters.Add("@SearchTerm", SqlDbType.NVarChar, 150).Value = searchTerm;

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            people.Add(new PersonSummary(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2)
            ));
        }

        return people;
    }

    /// <summary>Loads people for one role and formats the fields that belong to that role.</summary>
    /// <param name="role">The role used to filter people.</param>
    /// <returns>Matching people with their contact details and role-specific display text.</returns>
    /// <remarks>This query returns a display-ready string so the view does not need role-specific SQL.</remarks>
    /// <remarks>Subtype joins are optional because each person belongs to only one role table.</remarks>
    public async Task<IReadOnlyList<PersonRoleSummary>> GetPeopleByRoleAsync(
        string role)
    {
        // Format role-specific details for display; linked genres and categories are combined into one string.
        const string sql = """
            SELECT
                p.PersonID,
                p.Name,
                p.Telephone,
                p.Email,
                p.Role,
                CASE p.Role
                    -- Performers can have multiple genres, so aggregate their link-table rows into one value.
                    WHEN N'Performer' THEN CONCAT(
                        N'Fee: ',
                        CONVERT(nvarchar(30), performer.Fee),
                        N'; Genres: ',
                        COALESCE(
                            (
                                SELECT STRING_AGG(g.GenreName, N', ')
                                FROM dbo.PerformerGenres AS pg
                                INNER JOIN dbo.Genres AS g
                                    ON g.GenreID = pg.GenreID
                                WHERE pg.PersonID = p.PersonID
                            ),
                            N'None'
                        )
                    )
                    WHEN N'Crew' THEN CONCAT(
                        N'Hourly rate: ',
                        CONVERT(nvarchar(30), crew.HourlyRate),
                        N'; Employment: ',
                        crew.EmploymentType,
                        N'; Weekly hours: ',
                        crew.WeeklyHours
                    )
                    -- Vendors also have multiple categories; show None if no linked category is present.
                    WHEN N'Vendor' THEN CONCAT(
                        N'Categories: ',
                        COALESCE(
                            (
                                SELECT STRING_AGG(category.CategoryName, N', ')
                                FROM dbo.VendorCategories AS vc
                                INNER JOIN dbo.ProductCategories AS category
                                    ON category.CategoryID = vc.CategoryID
                                WHERE vc.PersonID = p.PersonID
                            ),
                            N'None'
                        )
                    )
                END AS RoleDetails
            FROM dbo.People AS p
            -- Left joins preserve the person row while attaching optional subtype data.
            LEFT JOIN dbo.Performers AS performer
                ON performer.PersonID = p.PersonID
            LEFT JOIN dbo.Crew AS crew
                ON crew.PersonID = p.PersonID
            WHERE p.Role = @Role
            ORDER BY p.PersonID;
            """;

        var people = new List<PersonRoleSummary>();

        // The role is supplied as a parameter so only rows for the chosen role are returned.
        await using var command = new SqlCommand(sql, _connection);
        command.Parameters
            .Add("@Role", SqlDbType.NVarChar, 20).Value = role;

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            // Column positions follow the SELECT list: ID, contact fields, role, then formatted details.
            people.Add(new PersonRoleSummary(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetString(5)
            ));
        }

        return people;
    }

    /// <summary>Inserts the shared person row and crew-specific row in one transaction.</summary>
    /// <param name="name">The crew member's name.</param>
    /// <param name="telephone">The crew member's telephone number.</param>
    /// <param name="email">The crew member's email address.</param>
    /// <param name="hourlyRate">The crew member's hourly rate.</param>
    /// <param name="employmentType">The crew member's employment type.</param>
    /// <param name="weeklyHours">The crew member's validated weekly hours.</param>
    /// <returns>The generated PersonID used by both inserted rows.</returns>
    /// <remarks>Both inserts share a transaction so a crew record cannot exist without its person row.</remarks>
    /// <remarks>The service validates the values first; this method handles persistence and key linking.</remarks>
    public async Task<int> AddCrewAsync(
        string name,
        string telephone,
        string email,
        decimal hourlyRate,
        string employmentType,
        int weeklyHours)
    {
        // Save the shared person row and crew row together so a failure cannot leave a person without crew details.
        using SqlTransaction transaction = _connection.BeginTransaction();

        try
        {
            const string insertPersonSql = """
                INSERT INTO dbo.People (Name, Telephone, Email, Role)
                -- Return the database-generated key to use for the linked Crew row.
                OUTPUT INSERTED.PersonID
                VALUES (@Name, @Telephone, @Email, N'Crew');
                """;

            int personID;

            await using (var personCommand = new SqlCommand(insertPersonSql, _connection, transaction))
            {
                // Bind common fields with the same SQL types and maximum lengths as the schema.
                personCommand.Parameters
                    .Add("@Name", SqlDbType.NVarChar, 150).Value = name;
                personCommand.Parameters
                    .Add("@Telephone", SqlDbType.NVarChar, 30).Value = telephone;
                personCommand.Parameters
                    .Add("@Email", SqlDbType.NVarChar, 254).Value = email;

                personID = (int)(await personCommand.ExecuteScalarAsync())!;
            }

            // Store crew-specific fields under the PersonID created by the first insert.
            const string insertCrewSql = """
                INSERT INTO dbo.Crew
                    (PersonID, HourlyRate, EmploymentType, WeeklyHours)
                VALUES
                    (@PersonID, @HourlyRate, @EmploymentType, @WeeklyHours);
                """;

            await using (var crewCommand =
                new SqlCommand(insertCrewSql, _connection, transaction))
            {
                crewCommand.Parameters
                    .Add("@PersonID", SqlDbType.Int).Value = personID;

                var rateParameter = crewCommand.Parameters.Add("@HourlyRate", SqlDbType.Decimal);
                // Match the Crew.HourlyRate decimal definition in the database schema.
                rateParameter.Precision = 8;
                rateParameter.Scale = 2;
                rateParameter.Value = hourlyRate;

                crewCommand.Parameters
                    .Add("@EmploymentType", SqlDbType.NVarChar, 10).Value = employmentType;
                crewCommand.Parameters
                    .Add("@WeeklyHours", SqlDbType.Int).Value = weeklyHours;

                await crewCommand.ExecuteNonQueryAsync();
            }

            // Commit only after both the shared and crew-specific rows have been inserted.
            transaction.Commit();
            return personID;
        }

        catch
        {
            try
            {
                transaction.Rollback();
            }

            catch
            {
                // Preserve the insert error if SQL Server cannot roll back the transaction.
            }

            throw;
        }
    }

    /// <summary>Inserts a performer and all of their genre links in one transaction.</summary>
    /// <param name="name">The performer's name.</param>
    /// <param name="telephone">The performer's telephone number.</param>
    /// <param name="email">The performer's email address.</param>
    /// <param name="fee">The performer's validated fee.</param>
    /// <param name="genres">The performer's validated genre list.</param>
    /// <returns>The generated PersonID for the new performer.</returns>
    /// <remarks>Genre names are resolved to shared IDs before rows are written to the link table.</remarks>
    /// <remarks>All person, performer, and genre-link inserts succeed or roll back together.</remarks>
    public async Task<int> AddPerformerAsync(
        string name,
        string telephone,
        string email,
        decimal fee,
        IReadOnlyList<string> genres)
    {
        // Keep the person, performer details, and genre links in one atomic operation.
        using SqlTransaction transaction = _connection.BeginTransaction();

        try
        {
            const string insertPersonSql = """
                INSERT INTO dbo.People(Name, Telephone, Email, Role)
                -- Capture the generated PersonID for the performer and genre link rows.
                OUTPUT INSERTED.PersonID
                VALUES (@Name, @Telephone, @Email, N'Performer');
                """;
            int personID;
            await using (var personCommand =
                new SqlCommand(insertPersonSql, _connection, transaction))
            {
                personCommand.Parameters
                    .Add("@Name", SqlDbType.NVarChar, 150).Value = name;
                personCommand.Parameters
                    .Add("@Telephone", SqlDbType.NVarChar, 30).Value = telephone;
                personCommand.Parameters
                    .Add("@Email", SqlDbType.NVarChar, 254).Value = email;

                personID = (int)(await personCommand.ExecuteScalarAsync())!;
            }

            // Store fee separately from the common contact fields because it applies only to performers.
            const string insertPerformerSql = """
                INSERT INTO dbo.Performers (PersonID, Fee)
                VALUES (@PersonID, @Fee);
                """;

            await using (var performerCommand =
                new SqlCommand(insertPerformerSql, _connection, transaction))
            {
                performerCommand.Parameters
                    .Add("@PersonID", SqlDbType.Int).Value = personID;

                var feeParameter = performerCommand.Parameters.Add("@Fee", SqlDbType.Decimal);
                // Match the precision and scale declared for Performer.Fee in the database schema.
                feeParameter.Precision = 10;
                feeParameter.Scale = 2;
                feeParameter.Value = fee;

                await performerCommand.ExecuteNonQueryAsync();
            }

            // Reuse or create each shared genre row, then connect it to this performer.
            foreach (string genre in genres)
            {
                int genreID = await GetOrCreateGenreIDAsync(transaction, genre);

                const string linkGenreSql = """
                    INSERT INTO dbo.PerformerGenres (PersonID, GenreID)
                    VALUES (@PersonID, @GenreID);
                    """;

                await using var linkCommand =
                    new SqlCommand(linkGenreSql, _connection, transaction);

                linkCommand.Parameters
                    .Add("@PersonID", SqlDbType.Int).Value = personID;
                linkCommand.Parameters
                    .Add("@GenreID", SqlDbType.Int).Value = genreID;

                await linkCommand.ExecuteNonQueryAsync();
            }

            // The person, performer row, and every genre link become visible together.
            transaction.Commit();
            return personID;
        }

        catch
        {
            try
            {
                transaction.Rollback();
            }
            catch
            {
                // Preserve the original database error if rollback also fails.
            }

            throw;
        }
    }

    /// <summary>Finds a shared genre row or inserts it, then returns its database ID.</summary>
    /// <param name="transaction">The transaction that contains the lookup or insert.</param>
    /// <param name="genre">The genre name to find or create.</param>
    /// <returns>The ID of the existing or newly inserted genre.</returns>
    /// <remarks>Using a shared lookup row allows several performers to reference the same genre.</remarks>
    private async Task<int> GetOrCreateGenreIDAsync(
        SqlTransaction transaction,
        string genre
    )
    {
        const string sql = """
            -- Reuse shared lookup values so different performers can reference the same genre row.
            IF NOT EXISTS (
                SELECT 1
                FROM dbo.Genres
                WHERE GenreName = @GenreName
            )
            BEGIN
                INSERT INTO dbo.Genres (GenreName)
                VALUES (@GenreName);
            END;

            -- Return the key whether the genre was already present or was just inserted.
            SELECT GenreID
            FROM dbo.Genres
            WHERE GenreName = @GenreName;
            """;

        await using var command = new SqlCommand(sql, _connection, transaction);
        command.Parameters
            .Add("@GenreName", SqlDbType.NVarChar, 100).Value = genre;

        return (int)(await command.ExecuteScalarAsync())!;

    }

    /// <summary>Inserts the shared person row, vendor row, and category links atomically.</summary>
    /// <param name="name">The vendor's name.</param>
    /// <param name="telephone">The vendor's telephone number.</param>
    /// <param name="email">The vendor's email address.</param>
    /// <param name="categories">The vendor's validated product categories.</param>
    /// <returns>The generated PersonID for the new vendor.</returns>
    /// <remarks>Each category is resolved to a shared ID before a vendor-category link is inserted.</remarks>
    /// <remarks>The transaction keeps the vendor and all requested links consistent on failure.</remarks>
    public async Task<int> AddVendorAsync(
        string name,
        string telephone, 
        string email,
        IReadOnlyList<string> categories
    )
    {
        // Keep the person, vendor row, and category links in one atomic operation.
        using SqlTransaction transaction = _connection.BeginTransaction();

        try
        {
            const string insertPersonSql = """
                INSERT INTO dbo.People (Name, Telephone, Email, Role)
                -- Capture the generated PersonID for the linked Vendor row.
                OUTPUT INSERTED.PersonID
                VALUES (@Name, @Telephone, @Email, N'Vendor');
                """;
            
            int personID;

            await using (var personCommand = 
                new SqlCommand(insertPersonSql, _connection, transaction))
            {
                personCommand.Parameters
                    .Add("@Name", SqlDbType.NVarChar, 150).Value = name;
                personCommand.Parameters
                    .Add("@Telephone", SqlDbType.NVarChar, 30).Value = telephone;
                personCommand.Parameters 
                    .Add("@Email", SqlDbType.NVarChar, 254).Value = email;
                
                personID = (int)(await personCommand.ExecuteScalarAsync())!;
            }

            // The vendor table stores role membership; its shared contact fields remain in People.
            const string insertVendorSql = """
                INSERT INTO dbo.Vendors (PersonID)
                VALUES (@PersonID);
                """;
            
            await using (var vendorCommand = 
                new SqlCommand(insertVendorSql, _connection, transaction))
            {
                vendorCommand.Parameters
                    .Add("@PersonID", SqlDbType.Int).Value = personID;
                
                await vendorCommand.ExecuteNonQueryAsync();
            }

            // Reuse or create each shared category row, then connect it to this vendor.
            foreach (string category in categories)
            {
                int categoryID = await GetOrCreateCategoryIDAsync(
                    transaction,
                    category
                );

                const string linkCategorySql = """
                    INSERT INTO dbo.VendorCategories (PersonID, CategoryID)
                    VALUES (@PersonID, @CategoryID);
                    """;
                
                await using var linkCommand =
                    new SqlCommand(linkCategorySql, _connection, transaction);
                
                linkCommand.Parameters
                    .Add("@PersonID", SqlDbType.Int).Value = personID;
                linkCommand.Parameters
                    .Add("@CategoryID", SqlDbType.Int).Value = categoryID;
                
                await linkCommand.ExecuteNonQueryAsync();
            }

            // Do not commit until the vendor and all category links are complete.
            transaction.Commit();
            return personID;
        }

        catch
        {
            try
            {
                transaction.Rollback();
            }

            catch
            {
                // Preserve the original database error if rollback also fails.
            }

            throw;
        }
    }

    /// <summary>Finds a shared product category or inserts it, then returns its database ID.</summary>
    /// <param name="transaction">The transaction that contains the lookup or insert.</param>
    /// <param name="category">The category name to find or create.</param>
    /// <returns>The ID of the existing or newly inserted category.</returns>
    /// <remarks>Shared category rows avoid storing duplicate spellings for every vendor.</remarks>
    private async Task<int> GetOrCreateCategoryIDAsync(
        SqlTransaction transaction,
        string category
    )
    {
        const string sql = """
            -- Shared category rows are reused so vendors can reference the same category.
            IF NOT EXISTS (
                SELECT 1
                FROM dbo.ProductCategories
                WHERE CategoryName = @CategoryName
            )
            BEGIN
                INSERT INTO dbo.ProductCategories (CategoryName)
                VALUES (@CategoryName);
            END;

            -- Return the key for both the existing-row and new-row cases.
            SELECT CategoryID
            FROM dbo.ProductCategories
            WHERE CategoryName = @CategoryName;
            """;
        
        await using var command = new SqlCommand(sql, _connection, transaction);
        command.Parameters
            .Add("@CategoryName", SqlDbType.NVarChar, 100).Value = category;
        
        return (int)(await command.ExecuteScalarAsync())!;
    }

    /// <summary>Finds a person's basic summary by database ID.</summary>
    /// <param name="personID">The ID to look up.</param>
    /// <returns>The summary if found; otherwise, <see langword="null"/>.</returns>
    /// <remarks>Returning null gives the service and controller a normal not-found path.</remarks>
    public async Task<PersonSummary?> GetPersonByIDAsync(int personID)
    {
        const string sql = """
            SELECT PersonID, Name, Role
            FROM dbo.People
            WHERE PersonID = @PersonID;
            """;

        await using var command = new SqlCommand(sql, _connection);
        command.Parameters
            .Add("@PersonID", SqlDbType.Int).Value = personID;
        
        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new PersonSummary(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.GetString(2)
        );
    }

    /// <summary>Deletes the person row with the supplied ID.</summary>
    /// <param name="personID">The ID of the person to delete.</param>
    /// <returns><see langword="true"/> if one row was deleted; otherwise, <see langword="false"/>.</returns>
    /// <remarks>The caller performs the user confirmation before invoking this data operation.</remarks>
    public async Task<bool> DeletePersonAsync(int personID)
    {
        const string sql = """
            DELETE FROM dbo.People
            WHERE PersonID = @PersonID;
            """;

        await using var command = new SqlCommand(sql, _connection);
        command.Parameters
            .Add("@PersonID", SqlDbType.Int).Value = personID;
        
        // ExecuteNonQueryAsync reports affected rows, which distinguishes a missing ID from a deletion.
        int rowsDeleted = await command.ExecuteNonQueryAsync();
        return rowsDeleted == 1;
    }

    /// <summary>Updates common person fields and the data belonging to the selected role.</summary>
    /// <param name="personID">The ID of the person to update.</param>
    /// <param name="role">The person's role: Performer, Crew, or Vendor.</param>
    /// <param name="name">The updated name.</param>
    /// <param name="telephone">The updated telephone number.</param>
    /// <param name="email">The updated email address.</param>
    /// <param name="fee">The updated performer fee; ignored for other roles.</param>
    /// <param name="genres">The updated performer genres; ignored for other roles.</param>
    /// <param name="hourlyRate">The updated crew rate; ignored for other roles.</param>
    /// <param name="employmentType">The updated crew type; ignored for other roles.</param>
    /// <param name="weeklyHours">The updated crew hours; ignored for other roles.</param>
    /// <param name="categories">The updated vendor categories; ignored for other roles.</param>
    /// <returns><see langword="true"/> if the person was found and updated; otherwise, <see langword="false"/>.</returns>
    /// <remarks>The role selects one subtype update path while the common contact fields are shared.</remarks>
    /// <remarks>Replacing link-table rows keeps genres and categories in sync with the submitted lists.</remarks>
    public async Task<bool> UpdatePersonAsync(
        int personID,
        string role,
        string name,
        string telephone,
        string email,
        decimal fee,
        IReadOnlyList<string> genres,
        decimal hourlyRate,
        string employmentType,
        int weeklyHours,
        IReadOnlyList<string> categories
    )
    {
        // Apply shared and role-specific changes atomically so the record cannot be left partly updated.
        using SqlTransaction transaction = _connection.BeginTransaction();

        try
        {
            const string updatePersonSql = """
                UPDATE dbo.People
                SET Name = @Name,
                    Telephone = @Telephone,
                    Email = @Email
                WHERE PersonID = @PersonID;
                """;

            // Update common contact fields first; the row count signals whether this ID exists.
            int rowsUpdated;

            await using (var personCommand =
                new SqlCommand(updatePersonSql, _connection, transaction))
            {
                personCommand.Parameters
                    .Add("@Name", SqlDbType.NVarChar, 150).Value = name;
                personCommand.Parameters
                    .Add("@Telephone", SqlDbType.NVarChar, 30).Value = telephone;
                personCommand.Parameters
                    .Add("@Email", SqlDbType.NVarChar, 254).Value = email;
                personCommand.Parameters
                    .Add("@PersonID", SqlDbType.Int).Value = personID;

                rowsUpdated = await personCommand.ExecuteNonQueryAsync();

            }

            // Do not update role-specific tables if the PersonID does not exist.
            if (rowsUpdated == 0)
            {
                transaction.Rollback();
                return false;
            }

            if (role == "Performer")
            {
                // Update the performer's fee, then replace the link rows to match the submitted genre list.
                const string updatePerformerSql = """
                    UPDATE dbo.Performers
                    SET Fee = @Fee
                    WHERE PersonID = @PersonID;
                    """;

                await using (var performerCommand =
                    new SqlCommand(updatePerformerSql, _connection, transaction))
                {
                    var feeParameter = performerCommand.Parameters.Add("@Fee", SqlDbType.Decimal);
                    feeParameter.Precision = 10;
                    feeParameter.Scale = 2;
                    feeParameter.Value = fee;

                    performerCommand.Parameters
                        .Add("@PersonID", SqlDbType.Int).Value = personID;

                    await performerCommand.ExecuteNonQueryAsync();
                }
                // Replace the existing links so only the submitted genres remain.
                const string deleteGenresSql = """
                    DELETE FROM dbo.PerformerGenres
                    WHERE PersonID = @PersonID;
                    """;

                await using (var deleteGenresCommand =
                    new SqlCommand(deleteGenresSql, _connection, transaction))
                {
                    deleteGenresCommand.Parameters
                        .Add("@PersonID", SqlDbType.Int).Value = personID;

                    await deleteGenresCommand.ExecuteNonQueryAsync();
                }

                // Rebuild this performer's genre links from the submitted list.
                foreach (string genre in genres)
                {
                    int genreID = await GetOrCreateGenreIDAsync(transaction, genre);

                    const string linkGenreSql = """
                        INSERT INTO dbo.PerformerGenres (PersonID, GenreID)
                        VALUES (@PersonID, @GenreID);
                        """;

                    await using var linkCommand =
                        new SqlCommand(linkGenreSql, _connection, transaction);

                    linkCommand.Parameters
                        .Add("@PersonID", SqlDbType.Int).Value = personID;
                    linkCommand.Parameters
                        .Add("@GenreID", SqlDbType.Int).Value = genreID;

                    await linkCommand.ExecuteNonQueryAsync();
                }
            }

            else if (role == "Crew")
            {
                // Crew has one role row, so update its pay and schedule fields directly.
                const string updateCrewSql = """
                    UPDATE dbo.Crew
                    SET HourlyRate = @HourlyRate,
                        EmploymentType = @EmploymentType,
                        WeeklyHours = @WeeklyHours
                    WHERE PersonID = @PersonID;
                    """;

                await using var crewCommand =
                    new SqlCommand(updateCrewSql, _connection, transaction);

                var rateParameter =
                    crewCommand.Parameters.Add("@HourlyRate", SqlDbType.Decimal);
                rateParameter.Precision = 8;
                rateParameter.Scale = 2;
                rateParameter.Value = hourlyRate;

                crewCommand.Parameters
                    .Add("@EmploymentType", SqlDbType.NVarChar, 10).Value =
                        employmentType;
                crewCommand.Parameters
                    .Add("@WeeklyHours", SqlDbType.Int).Value = weeklyHours;
                crewCommand.Parameters
                    .Add("@PersonID", SqlDbType.Int).Value = personID;

                await crewCommand.ExecuteNonQueryAsync();
            }

            else if (role == "Vendor")
            {
                // Category memberships use a link table, so remove the old links before inserting the new set.
                // Replace the existing links so only the submitted categories remain. 
                const string deleteCategoriesSql = """
                    DELETE FROM dbo.VendorCategories
                    WHERE PersonID = @PersonID;
                    """;

                await using (var deleteCategoriesCommand =
                    new SqlCommand(deleteCategoriesSql, _connection, transaction))
                {
                    deleteCategoriesCommand.Parameters
                        .Add("@PersonID", SqlDbType.Int).Value = personID;

                    await deleteCategoriesCommand.ExecuteNonQueryAsync();
                }

                // Rebuild this vendor's category links from the submitted list.
                foreach (string category in categories)
                {
                    int categoryID = await GetOrCreateCategoryIDAsync(
                        transaction,
                        category
                    );

                    const string linkCategorySql = """
                        INSERT INTO dbo.VendorCategories (PersonID, CategoryID)
                        VALUES (@PersonID, @CategoryID);
                        """;

                    await using var linkCommand =
                        new SqlCommand(linkCategorySql, _connection, transaction);

                    linkCommand.Parameters
                        .Add("@PersonID", SqlDbType.Int).Value = personID;
                    linkCommand.Parameters
                        .Add("@CategoryID", SqlDbType.Int).Value = categoryID;

                    await linkCommand.ExecuteNonQueryAsync();
                }
            }

            else
            {
                throw new ArgumentException("Unknown person role.", nameof(role));
            }

            // Commit common fields and all role-specific changes as one successful update.
            transaction.Commit();
            return true;
        }

        catch
        {
            try
            {
                transaction.Rollback();
            }
            catch
            {
                // Keep the original database error if rollback also fails.
            }

            throw;
        }
    }
}
