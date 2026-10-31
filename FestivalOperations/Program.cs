using System.Data;
using System.Globalization;
using Microsoft.Data.SqlClient;

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

   Console.WriteLine("Connected to FestivalOperationsDB.");

   while (true)
    {
        Console.WriteLine();
        Console.WriteLine("1. List people");
        Console.WriteLine("2. Add crew member");
        Console.WriteLine("3. Add performer");
        Console.WriteLine("4. Add vendor");
        Console.WriteLine("5. View people by role");
        Console.WriteLine("0. Exit");
        Console.Write("Choose an option: ");

        string? choice = Console.ReadLine()?.Trim();

        if (choice == "0")
        {
            break;
        }

        else if (choice == "1")
        {
            await ListPeopleAsync(connection);
        }

        else if (choice == "2")
        {
            await AddCrewAsync(connection);
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

static async Task ListPeopleAsync(SqlConnection connection)
{
    const string sql = """
        SELECT PersonID, Name, Role
        FROM dbo.People
        ORDER BY PersonID;
        """;

    await using var command = new SqlCommand(sql, connection);
    await using var reader = await command.ExecuteReaderAsync();

    bool foundPeople = false;

    Console.WriteLine();
    Console.WriteLine("ID | Name | Role");

    while (await reader.ReadAsync())
    {
        foundPeople = true;

        int personID = reader.GetInt32(0);
        string name = reader.GetString(1);
        string role = reader.GetString(2);

        Console.WriteLine($"{personID} | {name} | {role}");
    }

    if (!foundPeople)
    {
        Console.WriteLine("No people found.");
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

static async Task AddCrewAsync(SqlConnection connection)
{
    string name = ReadRequired("Name: ", 150);
    string telephone = ReadRequired("Telephone: ", 30);
    string email = ReadRequired("Email: ", 254);

    decimal hourlyRate = ReadHourlyRate();
    string employmentType = ReadEmploymentType();
    int weeklyHours = ReadWeeklyHours(employmentType);

    using SqlTransaction transaction = connection.BeginTransaction();

    try
    {
        const string insertPersonSql = """
            INSERT INTO dbo.People (Name, Telephone, Email, Role)
            OUTPUT INSERTED.PersonID
            VALUES (@Name, @Telephone, @Email, N'Crew');
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
    

    const string insertCrewSql = """
        INSERT INTO dbo.Crew
            (PersonID, HourlyRate, EmploymentType, WeeklyHours)
        VALUES
            (@PersonID, @HourlyRate, @EmploymentType, @WeeklyHours);
        """;

    await using (var crewCommand = 
        new SqlCommand(insertCrewSql, connection, transaction))
    {
        crewCommand.Parameters
            .Add("@PersonID", SqlDbType.Int).Value = personID;
        
        var rateParameter =
            crewCommand.Parameters.Add("@HourlyRate", SqlDbType.Decimal);
        rateParameter.Precision = 8;
        rateParameter.Scale = 2;
        rateParameter.Value = hourlyRate;

        crewCommand.Parameters
            .Add("@EmploymentType", SqlDbType.NVarChar, 10).Value = employmentType;
        crewCommand.Parameters
            .Add("@WeeklyHours", SqlDbType.Int).Value = weeklyHours;
        
        await crewCommand.ExecuteNonQueryAsync();
    }

    transaction.Commit();
    Console.WriteLine($"Crew member added with PersonID {personID}.");
    } 
    
    catch
    {
        transaction.Rollback();
        throw;
    }
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