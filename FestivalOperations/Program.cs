using Microsoft.Data.SqlClient;
using FestivalOperations.Data;
using FestivalOperations.Views;
using FestivalOperations.Controllers;
using FestivalOperations.Services;

// Read the database connection from the current environment so credentials stay outside the source files.
string? connectionString = Environment.GetEnvironmentVariable("FESTIVAL_DB_CONNECTION");

if (string.IsNullOrWhiteSpace(connectionString))
{
    // Stop before constructing any database-backed services when configuration is unavailable.
    Console.WriteLine("Environment variable is missing.");
    return;
}

// Open the connection and link the application's layers together.
try
{
    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();

    // Build the data, business, presentation, and controller objects from the inside out.
    var personRepository = new PersonRepository(connection);
    var festivalService = new FestivalService(personRepository);
    var consoleView = new ConsoleView();

    var festivalController = new FestivalController(festivalService, consoleView);
    await festivalController.RunAsync();
}

catch (SqlException ex)
{
    // Report connection and query failures at the application's outer boundary.
    Console.WriteLine($"Database error: {ex.Message}");
}
