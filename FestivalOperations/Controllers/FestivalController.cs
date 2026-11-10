using FestivalOperations.Data;
using FestivalOperations.Views;

namespace FestivalOperations.Controllers;

public sealed class FestivalController
{
    private readonly PersonRepository _personRepository;
    private readonly ConsoleView _consoleView;

    public FestivalController(PersonRepository personRepository, ConsoleView consoleView)
    {
        _personRepository = personRepository;
        _consoleView = consoleView;
    }

    public async Task RunAsync()
    {
        while (true)
        {
            string? choice = _consoleView.ReadMainMenuChoice();

            if (choice == "0")
            {
                return;
            }

            else if (choice == "1")
            {
                await ListPeopleAsync();
            }

            else if (choice == "2")
            {
                await AddCrewAsync();
            }

            else if (choice == "3")
            {
                await AddPerformerAsync();
            }

            else if (choice == "4")
            {
                await AddVendorAsync();
            }

            else if (choice == "5")
            {
                await ViewPeopleByRoleAsync();
            }

            else if (choice == "6")
            {
                await EditPersonAsync();
            }

            else if (choice == "7")
            {
                await DeletePersonAsync();
            }

            else
            {
                _consoleView.ShowInvalidMenuChoice();
            }
        }
    }

    private async Task ListPeopleAsync()
    {
        IReadOnlyList<PersonSummary> people =
            await _personRepository.GetAllPeopleAsync();
        
        _consoleView.ShowPeople(people);
    }

    private async Task ViewPeopleByRoleAsync()
    {
        string? role = null;

        while (role is null)
        {
            string? choice = _consoleView.ReadRoleChoice();

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
                _consoleView.ShowInvalidRoleChoice();
            }
        }

        if (role == "Back")
        {
            return;
        }

        IReadOnlyList<PersonRoleSummary> people = 
            await _personRepository.GetPeopleByRoleAsync(role);
        
        _consoleView.ShowPeopleByRole(role, people);
    }

    private async Task AddCrewAsync()
    {
        string name = _consoleView.ReadRequiredText("Name: ", 150);
        string telephone = _consoleView.ReadRequiredText("Telephone: ", 30);
        string email = _consoleView.ReadRequiredText("Email: ", 254);

        decimal hourlyRate = _consoleView.ReadCrewHourlyRate();
        string employmentType = _consoleView.ReadEmploymentType();
        int weeklyHours = _consoleView.ReadCrewWeeklyHours(employmentType);

        int personID = await _personRepository.AddCrewAsync(
            name,
            telephone,
            email,
            hourlyRate,
            employmentType,
            weeklyHours
        );

        _consoleView.ShowCrewAdded(personID);
    }

    private async Task AddPerformerAsync()
    {
        string name = _consoleView.ReadRequiredText("Name: ", 150);
        string telephone = _consoleView.ReadRequiredText("Telephone: ", 30);
        string email = _consoleView.ReadRequiredText("Email: ", 254);

        decimal fee = _consoleView.ReadPerformerFee();
        List<string> genres = _consoleView.ReadPerformerGenres();

        int personID = await _personRepository.AddPerformerAsync(
            name,
            telephone,
            email,
            fee,
            genres
        );

        _consoleView.ShowPerformerAdded(personID);
    }

    private async Task AddVendorAsync()
    {
        string name = _consoleView.ReadRequiredText("Name: ", 150);
        string telephone = _consoleView.ReadRequiredText("Telephone: ", 30);
        string email = _consoleView.ReadRequiredText("Email: ", 254);

        List<string> categories = _consoleView.ReadVendorCategories();

        int personID = await _personRepository.AddVendorAsync(
            name,
            telephone,
            email,
            categories
        );

        _consoleView.ShowVendorAdded(personID);
    }

    private async Task EditPersonAsync()
    {
        int personID = _consoleView.ReadPersonID("PersonID to edit: ");

        PersonSummary? person = 
            await _personRepository.GetPersonByIDAsync(personID);
        
        if (person is null)
        {
            _consoleView.ShowPersonNotFound(personID);
            return;
        }

        string role = person.Role;
        _consoleView.ShowEditingPerson(role, personID);

        string name = _consoleView.ReadRequiredText("New name: ", 150);
        string telephone = _consoleView.ReadRequiredText("New telephone: ", 30);
        string email = _consoleView.ReadRequiredText("New email: ", 254);

        decimal fee = 0;
        List<string> genres = [];
        decimal hourlyRate = 0;
        string employmentType = "";
        int weeklyHours = 0;
        List<string> categories = [];

        if (role == "Performer")
        {
            fee = _consoleView.ReadPerformerFee();
            genres = _consoleView.ReadPerformerGenres();
        }

        else if (role == "Crew")
        {
            hourlyRate = _consoleView.ReadCrewHourlyRate();
            employmentType = _consoleView.ReadEmploymentType();
            weeklyHours = _consoleView.ReadCrewWeeklyHours(employmentType);
        }

        else if (role == "Vendor")
        {
            categories = _consoleView.ReadVendorCategories();
        }

        else
        {
            _consoleView.ShowUnknownRole(role);
            return;
        }

        bool updated = await _personRepository.UpdatePersonAsync(
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
            _consoleView.ShowPersonUpdated(personID);
        }

        else
        {
            _consoleView.ShowPersonNotFound(personID);
        }
    }

    private async Task DeletePersonAsync()
    {
        int personID = _consoleView.ReadPersonID("PersonID to delete: ");

        PersonSummary? person =
            await _personRepository.GetPersonByIDAsync(personID);
        
        if (person is null)
        {
            _consoleView.ShowPersonNotFound(personID);
            return;
        }

        if (!_consoleView.ConfirmPersonDeletion(person))
        {
            _consoleView.ShowDeletionCancelled();
            return;
        }

        bool deleted = await _personRepository.DeletePersonAsync(personID);
        _consoleView.ShowDeletionResult(personID, deleted);
    }
}