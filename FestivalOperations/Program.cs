using Microsoft.Data.SqlClient;
using FestivalOperations.Data;
using FestivalOperations.Views;
using FestivalOperations.Controllers;

string? connectionString = Environment.GetEnvironmentVariable("FESTIVAL_DB_CONNECTION");

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
    var consoleView = new ConsoleView();
    var festivalController = new FestivalController(
        personRepository,
        consoleView
    );

    Console.WriteLine("Connected to FestivalOperationsDB.");
    await festivalController.RunAsync();
}

catch (SqlException ex)
{
    Console.WriteLine($"Database error: {ex.Message}");
}