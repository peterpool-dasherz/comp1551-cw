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
            await AddPerformerAsync(personRepository, consoleView);
        }

        else if (choice == "4")
        {
            await AddVendorAsync(personRepository, consoleView);
        }

        else if (choice == "5")
        {
            await ViewPeopleByRoleAsync(personRepository, consoleView);
        }

        else if (choice == "6")
        {
            await EditPersonAsync(personRepository, consoleView);
        }

        else if (choice == "7")
        {
            await DeletePersonAsync(personRepository, consoleView);
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

static async Task AddPerformerAsync(PersonRepository personRepository, ConsoleView consoleView)
{
    string name = consoleView.ReadRequiredText("Name: ", 150);
    string telephone = consoleView.ReadRequiredText("Telephone: ", 30);
    string email = consoleView.ReadRequiredText("Email: ", 254);

    decimal fee = consoleView.ReadPerformerFee();
    List<string> genres = consoleView.ReadPerformerGenres();

    int personID = await personRepository.AddPerformerAsync(
        name,
        telephone,
        email,
        fee,
        genres
    );

    consoleView.ShowPerformerAdded(personID);
}

static async Task AddVendorAsync(PersonRepository personRepository, ConsoleView consoleView)
{
    string name = consoleView.ReadRequiredText("Name: ", 150);
    string telephone =consoleView.ReadRequiredText("Telephone: ", 30);
    string email = consoleView.ReadRequiredText("Email: ", 254);
    List<string> categories = consoleView.ReadVendorCategories();

    int personID = await personRepository.AddVendorAsync(
        name,
        telephone,
        email,
        categories
    );

    consoleView.ShowVendorAdded(personID);
}

static async Task EditPersonAsync(PersonRepository personRepository, ConsoleView consoleView)
{
    int personID = consoleView.ReadPersonID("PersonID to edit: ");

    PersonSummary? person = await personRepository.GetPersonByIDAsync(personID);

    if (person is null)
    {
        consoleView.ShowPersonNotFound(personID);
        return;
    }

    string role = person.Role;
    consoleView.ShowEditingPerson(role, personID);

    string name = consoleView.ReadRequiredText("New name: ", 150);
    string telephone = consoleView.ReadRequiredText("New telephone: ", 30);
    string email = consoleView.ReadRequiredText("New email: ", 254);

    decimal fee = 0;
    List<string> genres = [];
    decimal hourlyRate = 0;
    string employmentType = "";
    int weeklyHours = 0;
    List<string> categories = [];

    if (role == "Performer")
    {
        fee = consoleView.ReadPerformerFee();
        genres = consoleView.ReadPerformerGenres();
    }

    else if (role == "Crew")
    {
        hourlyRate = consoleView.ReadCrewHourlyRate();
        employmentType = consoleView.ReadEmploymentType();
        weeklyHours = consoleView.ReadCrewWeeklyHours(employmentType);
    }

    else if (role == "Vendor")
    {
        categories = consoleView.ReadVendorCategories();
    }

    else
    {
        consoleView.ShowUnknownRole(role);
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

    if (updated)
    {
        consoleView.ShowPersonUpdated(personID);
    }

    else
    {
        consoleView.ShowPersonNotFound(personID);
    }
}

static async Task DeletePersonAsync(PersonRepository personRepository, ConsoleView consoleView)
{
    int personID = consoleView.ReadPersonID("PersonID to delete: ");

    PersonSummary? person = await personRepository.GetPersonByIDAsync(personID);

    if (person is null)
    {
        consoleView.ShowPersonNotFound(personID);
        return;
    }

    if (!consoleView.ConfirmPersonDeletion(person))
    {
        consoleView.ShowDeletionCancelled();
        return;
    }

    bool deleted = await personRepository.DeletePersonAsync(personID);
    consoleView.ShowDeletionResult(personID, deleted);
}
