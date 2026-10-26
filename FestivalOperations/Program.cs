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
        Console.WriteLine("0. Exit");
        Console.Write("Choose an option: ");

        string? choice = Console.ReadLine()?.Trim();

        if (choice == "0")
        {
            break;
        }

        if (choice == "1")
        {
            await ListPeopleAsync(connection);
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