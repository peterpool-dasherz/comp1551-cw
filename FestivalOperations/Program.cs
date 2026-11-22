using Microsoft.Data.SqlClient;
using FestivalOperations.Data;
using FestivalOperations.Views;
using FestivalOperations.Controllers;
using FestivalOperations.Services;

// Read the database connection from the current environment.
string? connectionString = Environment.GetEnvironmentVariable("FESTIVAL_DB_CONNECTION");

if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.WriteLine("Environment variable is missing.");
    return;
}

// Open the connection and link the application's layers together.
try
{
    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();

    var personRepository = new PersonRepository(connection);
    var festivalService = new FestivalService(personRepository);
    var consoleView = new ConsoleView();

    var festivalController = new FestivalController(festivalService, consoleView);
    await festivalController.RunAsync();
}

catch (SqlException ex)
{
    Console.WriteLine($"Database error: {ex.Message}");
}
