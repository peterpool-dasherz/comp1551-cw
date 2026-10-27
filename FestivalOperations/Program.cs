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

        else
        {
            Console.WriteLine("Please enter 1 or 0.");
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