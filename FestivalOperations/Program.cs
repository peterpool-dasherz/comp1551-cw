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
            await AddPerformerAsync(personRepository);
        }

        else if (choice == "4")
        {
            await AddVendorAsync(personRepository);
        }

        else if (choice == "5")
        {
            await ViewPeopleByRoleAsync(personRepository);
        }

        else if (choice == "6")
        {
            await EditPersonAsync(connection);
        }

        else if (choice == "7")
        {
            await DeletePersonAsync(personRepository);
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

static async Task ViewPeopleByRoleAsync(PersonRepository personRepository)
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

    IReadOnlyList<PersonRoleSummary> people =
        await personRepository.GetPeopleByRoleAsync(role);
    
    Console.WriteLine();
    Console.WriteLine("ID | Name | Telephone | Email | Role | Details");

    if (people.Count == 0)
    {
        Console.WriteLine($"No {role.ToLowerInvariant()} records found.");
        return;
    }

    foreach (PersonRoleSummary person in people)
    {
        Console.WriteLine(
            $"{person.PersonID} | {person.Name} | {person.Telephone} | " + 
            $"{person.Email} | {person.Role} | {person.RoleDetails}"
        );
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

    Console.WriteLine($"New member added with PersonID {personID}.");
}

static async Task AddPerformerAsync(PersonRepository personRepository)
{
    string name = ReadRequired("Name: ", 150);
    string telephone = ReadRequired("Telephone: ", 30);
    string email = ReadRequired("Email: ", 254);

    decimal fee = ReadPerformerFee();
    List<string> genres = ReadGenres();

    int personID = await personRepository.AddPerformerAsync(
        name,
        telephone,
        email,
        fee,
        genres
    );

    Console.WriteLine($"Performer added with PersonID {personID}.");
}

static async Task AddVendorAsync(PersonRepository personRepository)
{
    string name = ReadRequired("Name: ", 150);
    string telephone = ReadRequired("Telephone: ", 30);
    string email = ReadRequired("Email: ", 254);
    List<string> categories = ReadCategories();

    int personID = await personRepository.AddVendorAsync(
        name,
        telephone,
        email,
        categories
    );

    Console.WriteLine($"Vendor added with PersonID {personID}.");
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

static async Task DeletePersonAsync(PersonRepository personRepository)
{
    int personID = ReadPersonID("PersonID to delete: ");

    PersonSummary? person = await personRepository.GetPersonByIDAsync(personID);

    if (person is null)
    {
        Console.WriteLine($"No person found with this ID {personID}.");
        return;
    }

    Console.WriteLine($"Found: {person.Name} ({person.Role}), PersonID {person.PersonID}.");
    Console.Write("Type YES to confirm deletion: ");

    string? confirmation = Console.ReadLine()?.Trim();

    if (!string.Equals(confirmation, "YES", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("Deletion cancelled.");
        return;
    }

    bool deleted = await personRepository.DeletePersonAsync(personID);

    if (deleted)
    {
        Console.WriteLine($"Person {personID} was deleted.");
    }
    else
    {
        Console.WriteLine($"Person {personID} was not found.");
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
