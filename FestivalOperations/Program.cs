using System.Data;
using System.Globalization;
using Microsoft.Data.SqlClient;
using FestivalOperations.Data;

string? connectionString =
    Environment.GetEnvironmentVariable("FESTIVAL_DB_CONNECTION");

if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.WriteLine("Environment variable is missing.");
    return;
}

try
{
    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();

    var personRepository = new PersonRepository(connection);

   Console.WriteLine("Connected to FestivalOperationsDB.");

   while (true)
    {
        Console.WriteLine();
        Console.WriteLine("1. List people");
        Console.WriteLine("2. Add crew member");
        Console.WriteLine("3. Add performer");
        Console.WriteLine("4. Add vendor");
        Console.WriteLine("5. View people by role");
        Console.WriteLine("6. Edit person by ID");
        Console.WriteLine("7. Delete person by ID");
        Console.WriteLine("0. Exit");
        Console.Write("Choose an option: ");

        string? choice = Console.ReadLine()?.Trim();

        if (choice == "0")
        {
            break;
        }

        else if (choice == "1")
        {
            await ListPeopleAsync(personRepository);
        }

        else if (choice == "2")
        {
            await AddCrewAsync(personRepository);
        }

        else if (choice == "3")
        {
            await AddPerformerAsync(connection);
        }

        else if (choice == "4")
        {
            await AddVendorAsync(connection);
        }

        else if (choice == "5")
        {
            await ViewPeopleByRoleAsync(connection);
        }

        else if (choice == "6")
        {
            await EditPersonAsync(connection);
        }

        else if (choice == "7")
        {
            await DeletePersonAsync(connection);
        }

        else
        {
            Console.WriteLine("Please choose an option from the list above.");
        }
    }
}

catch (SqlException ex)
{
    Console.WriteLine($"Database error: {ex.Message}");
}

static async Task ListPeopleAsync(PersonRepository personRepository)
{
    IReadOnlyList<PersonSummary> people = await personRepository.GetAllPeopleAsync();

    Console.WriteLine();
    Console.WriteLine("ID | Name | Role");

    if (people.Count == 0)
    {
        Console.WriteLine("No people found.");
        return;
    }

    foreach (PersonSummary person in people)
    {
        Console.WriteLine($"{person.PersonID} | {person.Name} | {person.Role}");
    }
}

static async Task ViewPeopleByRoleAsync(SqlConnection connection)
{
    string? role = null;

    while (role is null)
    {
        Console.WriteLine();
        Console.WriteLine("1. Performers");
        Console.WriteLine("2: Crew");
        Console.WriteLine("3. Vendors");
        Console.WriteLine("0. Back to main menu");
        Console.Write("Choose a role: ");

        string? choice = Console.ReadLine()?.Trim();

        role = choice switch
        {
            "1" => "Performer",
            "2" => "Crew",
            "3" => "Vendor",
            "0" => "Back",
            _ => null
        };

        if (role is null)
        {
            Console.WriteLine("Please choose from the options above.");
        }
    }

    if (role == "Back")
    {
        return;
    }

    const string sql = """
        SELECT
            p.PersonID,
            p.Name,
            p.Telephone,
            p.Email,
            p.Role,
            CASE p.Role
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
        LEFT JOIN dbo.Performers AS performer
            ON performer.PersonID = p.PersonID
        LEFT JOIN dbo.Crew AS crew
            ON crew.PersonID = p.PersonID
        WHERE p.Role = @Role
        ORDER BY p.PersonID;
        """;
    await using var command = new SqlCommand(sql, connection);
    command.Parameters
        .Add("@Role", SqlDbType.NVarChar, 20).Value = role;
    
    await using var reader = await command.ExecuteReaderAsync();

    bool foundPeople = false;

    Console.WriteLine();
    Console.WriteLine("ID | Name | Telephone | Email | Role | Details");

    while (await reader.ReadAsync())
    {
        foundPeople = true;

        int personID = reader.GetInt32(0);
        string name = reader.GetString(1);
        string telephone = reader.GetString(2);
        string email = reader.GetString(3);
        string personRole = reader.GetString(4);
        string roleDetails = reader.GetString(5);

        Console.WriteLine(
            $"{personID} | {name} | {telephone} | {email} | " +
            $"{personRole} | {roleDetails}"
        );
    }

    if (!foundPeople)
    {
        Console.WriteLine($"No {role.ToLowerInvariant()} records found.");
    }
}

static async Task AddCrewAsync(PersonRepository personRepository)
{
    string name = ReadRequired("Name: ", 150);
    string telephone = ReadRequired("Telephone: ", 30);
    string email = ReadRequired("Email: ", 254);

    decimal hourlyRate = ReadHourlyRate();
    string employmentType = ReadEmploymentType();
    int weeklyHours = ReadWeeklyHours(employmentType);

    int personID = await personRepository.AddCrewAsync(
        name, 
        telephone,
        email,
        hourlyRate,
        employmentType,
        weeklyHours
    );

    Console.WriteLine("New member added with PersonID {personID}.");
}

static async Task AddPerformerAsync(SqlConnection connection)
{
    string name = ReadRequired("Name: ", 150);
    string telephone = ReadRequired("Telephone: ", 30);
    string email = ReadRequired("Email: ", 254);

    decimal fee = ReadPerformerFee();
    List<string> genres = ReadGenres();

    using SqlTransaction transaction = connection.BeginTransaction();

    try
    {
        const string insertPersonSql = """
            INSERT INTO dbo.People (Name, Telephone, Email, Role)
            OUTPUT INSERTED.PersonID
            VALUES (@Name, @Telephone, @Email, N'Performer');
            """;
        
        int personID;
        
        await using (var personCommand =
            new SqlCommand(insertPersonSql, connection, transaction))
        {
            personCommand.Parameters
                .Add("@Name", SqlDbType.NVarChar, 150).Value = name;
            personCommand.Parameters
                .Add("@Telephone", SqlDbType.NVarChar, 30).Value = telephone;
            personCommand.Parameters
                .Add("@Email", SqlDbType.NVarChar, 254).Value = email;
            
            personID = (int)(await personCommand.ExecuteScalarAsync())!;

        }

        const string insertPerformerSql = """
            INSERT INTO dbo.Performers (PersonID, Fee)
            VALUES (@PersonID, @Fee);
            """;
        
        await using (var performerCommand =
            new SqlCommand(insertPerformerSql, connection, transaction))
        {
            performerCommand.Parameters
                .Add("@PersonID", SqlDbType.Int).Value = personID;
            
            var feeParameter =
                performerCommand.Parameters.Add("@Fee", SqlDbType.Decimal);
            feeParameter.Precision = 10;
            feeParameter.Scale = 2;
            feeParameter.Value = fee;

            await performerCommand.ExecuteNonQueryAsync();
        }

        foreach (string genre in genres)
        {
            const string getGenreIDSql = """
                IF NOT EXISTS (
                    SELECT 1
                    FROM dbo.Genres
                    WHERE GenreName = @GenreName
                )
                BEGIN 
                    INSERT INTO dbo.Genres (GenreName)
                    VALUES (@GenreName);
                END;

                SELECT GenreID
                FROM dbo.Genres
                WHERE GenreName = @GenreName;
                """;
            
            int genreID;

            await using (var genreCommand =
                new SqlCommand(getGenreIDSql, connection, transaction))
            {
                genreCommand.Parameters
                    .Add("@GenreName", SqlDbType.NVarChar, 100).Value = genre;
                
                genreID = (int)(await genreCommand.ExecuteScalarAsync())!;
            }

            const string linkGenreSql = """
                INSERT INTO dbo.PerformerGenres (PersonID, GenreID)
                VALUES (@PersonID, @GenreID);
                """;
            
            await using (var linkCommand =
                new SqlCommand(linkGenreSql, connection, transaction))
            {
                linkCommand.Parameters
                    .Add("@PersonID", SqlDbType.Int).Value = personID;
                linkCommand.Parameters
                    .Add("@GenreID", SqlDbType.Int).Value = genreID;
                
                await linkCommand.ExecuteNonQueryAsync();
            }
        }

        transaction.Commit();
        Console.WriteLine($"Performer added with PersonID {personID}.");
    }

    catch
    {
        transaction.Rollback();
        throw;
    }
}

static async Task AddVendorAsync(SqlConnection connection)
{
    string name = ReadRequired("Name: ", 150);
    string telephone = ReadRequired("Telephone: ", 30);
    string email = ReadRequired("Email: ", 254);
    List<string> categories = ReadCategories();

    using SqlTransaction transaction = connection.BeginTransaction();

    try
    {
        const string insertPersonSql = """
            INSERT INTO dbo.People (Name, Telephone, Email, Role)
            OUTPUT INSERTED.PersonID
            VALUES (@Name, @Telephone, @Email, N'Vendor');
            """;
        
        int personID;
        
        await using (var personCommand = 
            new SqlCommand(insertPersonSql, connection, transaction))
        {
            personCommand.Parameters
                .Add("@Name", SqlDbType.NVarChar, 150).Value = name;
            personCommand.Parameters
                .Add("@Telephone", SqlDbType.NVarChar, 30).Value = telephone;
            personCommand.Parameters
                .Add("@Email", SqlDbType.NVarChar, 254).Value = email;
            
            personID = (int)(await personCommand.ExecuteScalarAsync())!;
        }

        const string insertVendorSql = """
            INSERT INTO dbo.Vendors (PersonID)
            VALUES (@PersonID);
            """;
        
        await using (var vendorCommand =
            new SqlCommand(insertVendorSql, connection, transaction))
        {
            vendorCommand.Parameters
                .Add("@PersonID", SqlDbType.Int).Value = personID;
            
            await vendorCommand.ExecuteNonQueryAsync();
        }

        foreach (string category in categories)
        {
            const string getCategoryIDSql = """
                IF NOT EXISTS (
                    SELECT 1
                    FROM dbo.ProductCategories
                    WHERE CategoryName = @CategoryName    
                )
                BEGIN
                    INSERT INTO dbo.ProductCategories (CategoryName)
                    VALUES (@CategoryName);
                END;

                SELECT CategoryID
                FROM dbo.ProductCategories
                WHERE CategoryName = @CategoryName;
                """;
            
            int categoryID;

            await using (var categoryCommand = 
                new SqlCommand(getCategoryIDSql, connection, transaction))
            {
                categoryCommand.Parameters
                    .Add("@CategoryName", SqlDbType.NVarChar, 100).Value = category;
                
                categoryID = (int)(await categoryCommand.ExecuteScalarAsync())!;
            }

            const string linkCategorySql = """
                INSERT INTO dbo.VendorCategories (PersonID, CategoryID)
                VALUES (@PersonID, @CategoryID);
                """;
            await using (var linkCommand = 
                new SqlCommand(linkCategorySql, connection, transaction))
            {
                linkCommand.Parameters
                    .Add("@PersonID", SqlDbType.Int).Value = personID;
                linkCommand.Parameters
                    .Add("@CategoryID", SqlDbType.Int).Value = categoryID;
                
                await linkCommand.ExecuteNonQueryAsync();
            }
        }

        transaction.Commit();
        Console.WriteLine($"Vendor added.");
    }
    catch
    {
        try
        {
            transaction.Rollback();
        }
        catch
        {
            // If an error persists, keep the original database.
        }

        throw;
    }
}

static async Task EditPersonAsync(SqlConnection connection)
{
    int personID = ReadPersonID("PersonID to edit: ");

    const string getRoleSql = """
        SELECT Role
        FROM dbo.People
        WHERE PersonID = @PersonID;
        """;
    
    string? role;

    await using (var roleCommand = new SqlCommand(getRoleSql, connection))
    {
        roleCommand.Parameters
            .Add("@PersonID", SqlDbType.Int).Value = personID;
        
        role = (string?)await roleCommand.ExecuteScalarAsync();
    }

    if (role is null)
    {
        Console.WriteLine($"No person found with PersonID {personID}.");
        return;
    }

    Console.WriteLine($"Editing {role} with PersonID {personID}.");
    string name = ReadRequired("New name: ", 150);
    string telephone = ReadRequired("New telephone: ", 30);
    string email = ReadRequired("New email: ", 254);

    decimal fee = 0;
    List<string> genres = [];
    decimal hourlyRate = 0;
    string employmentType = "";
    int weeklyHours = 0;
    List<string> categories = [];

    if (role == "Performer")
    {
        fee = ReadPerformerFee();
        genres = ReadGenres();
    }

    else if (role == "Crew")
    {
        hourlyRate = ReadHourlyRate();
        employmentType = ReadEmploymentType();
        weeklyHours = ReadWeeklyHours(employmentType);
    }

    else if (role == "Vendor")
    {
        categories = ReadCategories();
    }

    else
    {
        Console.WriteLine($"Unknown role '{role}'.");
        return;
    }

    using SqlTransaction transaction = connection.BeginTransaction();

    try
    {
        const string updatePersonSql = """
            UPDATE dbo.People
            SET Name = @Name,
                Telephone = @Telephone,
                Email = @Email
            WHERE PersonID = @PersonID;
            """;
        
        await using (var personCommand = 
            new SqlCommand(updatePersonSql, connection, transaction))
        {
            personCommand.Parameters
                .Add("@Name", SqlDbType.NVarChar, 150).Value = name;
            personCommand.Parameters
                .Add("@Telephone", SqlDbType.NVarChar, 30).Value = telephone;
            personCommand.Parameters
                .Add("@Email", SqlDbType.NVarChar, 254).Value = email;
            personCommand.Parameters
                .Add("@PersonID", SqlDbType.Int).Value = personID;
            
            await personCommand.ExecuteNonQueryAsync();
        }

        if (role == "Performer")
        {
            const string updatePerformerSql = """
                UPDATE dbo.Performers
                SET Fee = @Fee
                WHERE PersonID = @PersonID;
                """;
            
            await using (var performerCommand =
                new SqlCommand(updatePerformerSql, connection, transaction))
            {
                var feeParameter =
                    performerCommand.Parameters.Add("@Fee", SqlDbType.Decimal);
                feeParameter.Precision = 10;
                feeParameter.Scale = 2;
                feeParameter.Value = fee;

                performerCommand.Parameters
                    .Add("@PersonID", SqlDbType.Int).Value = personID;
                
                await performerCommand.ExecuteNonQueryAsync();
            }

            await using (var deleteGenresCommand = 
                new SqlCommand(
                    "DELETE FROM dbo.PerformerGenres WHERE PersonID = @PersonID;",
                    connection,
                    transaction))
            {
                deleteGenresCommand.Parameters
                    .Add("@PersonID", SqlDbType.Int).Value = personID;
                await deleteGenresCommand.ExecuteNonQueryAsync();
            }

            foreach (string genre in genres)
            {
                int genreID = await GetOrCreateGenreIDAsync(
                    connection,
                    transaction, 
                    genre
                );

                await using var linkCommand = new SqlCommand(
                    """
                    INSERT INTO dbo.PerformerGenres(PersonID, GenreID)
                    VALUES (@PersonID, @GenreID);
                    """,
                    connection,
                    transaction
                );

                linkCommand.Parameters
                    .Add("@PersonID", SqlDbType.Int).Value = personID;
                linkCommand.Parameters
                    .Add("@GenreID", SqlDbType.Int).Value = genreID;
                
                await linkCommand.ExecuteNonQueryAsync();
            }
        }

        else if (role == "Crew")
        {
            const string updateCrewSql = """
                UPDATE dbo.Crew
                SET HourlyRate = @HourlyRate,
                    EmploymentType = @EmploymentType,
                    WeeklyHours = @WeeklyHours
                WHERE PersonID = @PersonID;
                """;
            
            await using var crewCommand = 
                new SqlCommand(updateCrewSql, connection, transaction);
            var rateParameter = crewCommand.Parameters.Add("@HourlyRate", SqlDbType.Decimal);
            rateParameter.Precision = 8;
            rateParameter.Scale = 2;
            rateParameter.Value = hourlyRate;

            crewCommand.Parameters
                .Add("@EmploymentType", SqlDbType.NVarChar, 10).Value = employmentType;
            crewCommand.Parameters
                .Add("@WeeklyHours", SqlDbType.Int).Value = weeklyHours;
            crewCommand.Parameters
                .Add("@PersonID", SqlDbType.Int).Value = personID;
            
            await crewCommand.ExecuteNonQueryAsync();
        }

        else if (role == "Vendor")
        {
            await using (var deleteCategoriesCommand =
                new SqlCommand(
                    "DELETE FROM dbo.VendorCategories WHERE PersonID = @PersonID;",
                    connection,
                    transaction))
            {
                deleteCategoriesCommand.Parameters
                    .Add("@PersonID", SqlDbType.Int).Value = personID;
                await deleteCategoriesCommand.ExecuteNonQueryAsync();
            }

            foreach (string category in categories)
            {
                int categoryID = await GetOrCreateCategoryIDAsync(
                    connection,
                    transaction,
                    category
                );

                await using var linkCommand = new SqlCommand(
                    """
                    INSERT INTO dbo.VendorCategories (PersonID, CategoryID)
                    VALUES (@PersonID, @CategoryID);
                    """,
                    connection,
                    transaction
                );

                linkCommand.Parameters
                    .Add("@PersonID", SqlDbType.Int).Value = personID;
                linkCommand.Parameters
                    .Add("@CategoryID", SqlDbType.Int).Value = categoryID;

                await linkCommand.ExecuteNonQueryAsync();
            }
        }

        transaction.Commit();
        Console.WriteLine($"Person {personID} updated successfully.");
    }

    catch
    {
        try
        {
            transaction.Rollback();
        }

        catch
        {
            
        }

        throw;
    }
}

static int ReadPersonID(string prompt)
{
    while (true)
    {
        Console.Write(prompt);
        string? input = Console.ReadLine();

        if (int.TryParse(input, out int personID) && personID > 0)
        {
            return personID;
        }

        Console.WriteLine("Enter a valid PersonID that is larger than zero.");
    }
}

static async Task DeletePersonAsync(SqlConnection connection)
{
    int personID = ReadPersonID("PersonID to delete: ");

    const string findPersonSql = """
        SELECT Name, Role
        FROM dbo.People
        WHERE PersonID = @PersonID;
        """;
    
    string? name = null;
    string? role = null;

    await using (var findCommand = new SqlCommand(findPersonSql, connection))
    {
        findCommand.Parameters
            .Add("@PersonID", SqlDbType.Int).Value = personID;

        await using var reader = await findCommand.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            name = reader.GetString(0);
            role = reader.GetString(1);
        }
    }

    if (name is null || role is null)
    {
        Console.WriteLine($"No person found with this ID. Please try again.");
        return;
    }

    Console.WriteLine($"Found: {name} ({role}), PersonID {personID}.");
    Console.Write("Type YES to confirm deletion.");

    string? confirmation = Console.ReadLine()?.Trim();

    if (!string.Equals(confirmation, "YES", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("Deletion cancelled.");
        return;
    }

    using SqlTransaction transaction = connection.BeginTransaction();

    try
    {
        const string deleteSql = """
            DELETE FROM dbo.People
            WHERE PersonID = @PersonID;
            """;
        
        int rowsDeleted;

        await using (var deleteCommand = 
            new SqlCommand(deleteSql, connection, transaction))
        {
            deleteCommand.Parameters
                .Add("@PersonID", SqlDbType.Int).Value = personID;
            
            rowsDeleted = await deleteCommand.ExecuteNonQueryAsync();
        }

        transaction.Commit();

        if (rowsDeleted == 1)
        {
            Console.WriteLine($"Person {personID} was deleted.");
        }

        else
        {
            Console.WriteLine($"Person {personID} was not found. Please try again.");
        }
    }

    catch
    {
        try
        {
            transaction.Rollback();
        }

        catch
        {
            
        }

        throw;
    }
}

static async Task<int> GetOrCreateGenreIDAsync(SqlConnection connection, SqlTransaction transaction, string genre)
{
    const string sql = """
        IF NOT EXISTS (
            SELECT 1 FROM dbo.Genres WHERE GenreName = @Name
        )
        BEGIN
            INSERT INTO dbo.Genres (GenreName) VALUES (@Name);
        END;

        SELECT GenreID FROM dbo.Genres WHERE GenreName = @Name;
        """;

    await using var command = new SqlCommand(sql, connection, transaction);
    command.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = genre;

    return (int)(await command.ExecuteScalarAsync())!;
}

static async Task<int> GetOrCreateCategoryIDAsync(SqlConnection connection, SqlTransaction transaction, string category)
{
    const string sql = """
        IF NOT EXISTS (
            SELECT 1
            FROM dbo.ProductCategories
            WHERE CategoryName = @Name
        )
        BEGIN
            INSERT INTO dbo.ProductCategories (CategoryName)
            VALUES (@Names);
        END;

        SELECT CategoryID
        FROM dbo.ProductCategories
        WHERE CategoryName = @Name;
        """;
    
    await using var command = new SqlCommand(sql, connection, transaction);
    command.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = category;

    return (int)(await command.ExecuteScalarAsync())!;
}

static string ReadRequired(string prompt, int maxLength)
{
    while (true)
    {
        Console.Write(prompt);
        string? value = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(value))
        {
            Console.WriteLine("This field cannot be empty.");
        }

        else if (value.Length > maxLength)
        {
            Console.WriteLine($"This field must be {maxLength} characters or fewer.");
        }

        else
        {
            return value;
        }
    }
}

static decimal ReadHourlyRate()
{
    while (true)
    {
        Console.Write("Hourly rate (for example, 15.00): ");
        string? input = Console.ReadLine();

        if (decimal.TryParse(
            input,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out decimal rate)
        && rate >= 0
        && rate <= 999999.99m)
        {
            return rate;
        }

        Console.WriteLine("Enter a rate: ");
    }
}

static string ReadEmploymentType()
{
    while (true)
    {
        Console.Write("EmploymentType: ");
        string? input = Console.ReadLine()?.Trim();

        if (string.Equals(input, "FullTime", StringComparison.OrdinalIgnoreCase))
        {
            return "FullTime";
        }

        if (string.Equals(input, "PartTime", StringComparison.OrdinalIgnoreCase))
        {
            return "PartTime";
        }

        Console.WriteLine("Enter FullTime or PartTime.");
    }
}

static int ReadWeeklyHours(string employmentType)
{
    int minimumHours = employmentType == "FullTime" ? 25 : 1;
    int maximumHours = employmentType == "FullTime" ? 40 : 24;

    while (true)
    {
        Console.Write($"Weekly hours ({minimumHours}-{maximumHours}): ");
        string? input = Console.ReadLine();

        if (int.TryParse(input, out int weeklyHours)
            && weeklyHours >= minimumHours
            && weeklyHours <= maximumHours)
        {
            return weeklyHours;
        }

        Console.WriteLine(
            $"Enter a whole number from {minimumHours} to {maximumHours}."
        );
    }
}

static decimal ReadPerformerFee()
{
    while (true)
    {
        Console.Write("Performance fee (for example, 500.00): ");
        string? input = Console.ReadLine();

        if (decimal.TryParse(
            input,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out decimal fee)
        && fee >= 0
        && fee <= 99999999.99m)
        {
            return fee;
        }

        Console.WriteLine("Enter a fee: ");
    }
}

static List<string> ReadGenres()
{
    while (true)
    {
        Console.Write("Genres: ");
        string? input = Console.ReadLine();

        List<string> genres = (input ?? string.Empty)
            .Split(
                ',',
                StringSplitOptions.TrimEntries
                    | StringSplitOptions.RemoveEmptyEntries
            )
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        
        if (genres.Count == 0)
        {
            Console.WriteLine("Enter at least one genre.");
        }

        else if (genres.Any(genre => genre.Length > 100))
        {
            Console.WriteLine("Each genre must be 100 characters or more.");
        }

        else
        {
            return genres;
        }
    }
}

static List<string> ReadCategories()
{
    while (true)
    {
        Console.Write("Product categories: ");
        string? input = Console.ReadLine();

        List<string> categories = (input ?? string.Empty)
            .Split(
                ',',
                StringSplitOptions.TrimEntries
                    | StringSplitOptions.RemoveEmptyEntries
            )
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        
        if (categories.Count == 0)
        {
            Console.WriteLine("Enter at least one category.");
        }

        else if (categories.Any(category => category.Length > 100))
        {
            Console.WriteLine("Each category must be 100 characters or fewer.");
        }

        else
        {
            return categories;
        }
    }
}