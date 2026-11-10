using System.Globalization;
using Microsoft.Data.SqlClient;
using FestivalOperations.Data;
using FestivalOperations.Views;

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
    var consoleView = new ConsoleView();

   Console.WriteLine("Connected to FestivalOperationsDB.");

   while (true)
    {
        string? choice = consoleView.ReadMainMenuChoice();

        if (choice == "0")
        {
            break;
        }

        else if (choice == "1")
        {
            await ListPeopleAsync(personRepository, consoleView);
        }

        else if (choice == "2")
        {
            await AddCrewAsync(personRepository, consoleView);
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
            await ViewPeopleByRoleAsync(personRepository, consoleView);
        }

        else if (choice == "6")
        {
            await EditPersonAsync(personRepository);
        }

        else if (choice == "7")
        {
            await DeletePersonAsync(personRepository);
        }

        else
        {
            consoleView.ShowInvalidMenuChoice();
        }
    }
}

catch (SqlException ex)
{
    Console.WriteLine($"Database error: {ex.Message}");
}

static async Task ListPeopleAsync(PersonRepository personRepository, ConsoleView consoleView)
{
    IReadOnlyList<PersonSummary> people = await personRepository.GetAllPeopleAsync();
    consoleView.ShowPeople(people);
}

static async Task ViewPeopleByRoleAsync(PersonRepository personRepository, ConsoleView consoleView)
{
    string? role = null;

    while (role is null)
    {
       string? choice = consoleView.ReadRoleChoice();

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
            consoleView.ShowInvalidRoleChoice();
        }
    }

    if (role == "Back")
    {
        return;
    }

    IReadOnlyList<PersonRoleSummary> people =
        await personRepository.GetPeopleByRoleAsync(role);
    
    consoleView.ShowPeopleByRole(role, people);
}

static async Task AddCrewAsync(PersonRepository personRepository, ConsoleView consoleView)
{
    string name = consoleView.ReadRequiredText("Name: ", 150);
    string telephone = consoleView.ReadRequiredText("Telephone: ", 30);
    string email = consoleView.ReadRequiredText("Email: ", 254);

    decimal hourlyRate = consoleView.ReadCrewHourlyRate();
    string employmentType = consoleView.ReadEmploymentType();
    int weeklyHours = consoleView.ReadCrewWeeklyHours(employmentType);

    int personID = await personRepository.AddCrewAsync(
        name, 
        telephone,
        email,
        hourlyRate,
        employmentType,
        weeklyHours
    );

    consoleView.ShowCrewAdded(personID);
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

static async Task EditPersonAsync(PersonRepository personRepository)
{
    int personID = ReadPersonID("PersonID to edit: ");

    PersonSummary? person = await personRepository.GetPersonByIDAsync(personID);

    if (person is null)
    {
        Console.WriteLine($"No person found with PersonID {personID}.");
        return;
    }

    string role = person.Role;

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

    bool updated = await personRepository.UpdatePersonAsync(
        personID,
        role,
        name,
        telephone,
        email,
        fee,
        genres,
        hourlyRate,
        employmentType,
        weeklyHours,
        categories
    );

    Console.WriteLine(
        updated
            ? $"Person {personID} updated successfully."
            : $"Person {personID} was not found."
    );
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
            Console.WriteLine("Each genre must be 100 characters or fewer.");
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
