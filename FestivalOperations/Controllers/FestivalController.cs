using Microsoft.Data.SqlClient;
using FestivalOperations.Data;
using FestivalOperations.Views;
using FestivalOperations.Services;

namespace FestivalOperations.Controllers;

/// <summary>
/// Routes menu choices to the corresponding service and connect with the views to display them.
/// </summary>
public sealed class FestivalController
{
    private readonly FestivalService _festivalService;
    private readonly ConsoleView _consoleView;

    public FestivalController(FestivalService festivalService, ConsoleView consoleView)
    {
        _festivalService = festivalService;
        _consoleView = consoleView;
    }

/// <summary>
/// Displays the main menu until the user choose to exit it.
/// </summary>
    public async Task RunAsync()
    {
        while (true)
        {
            string? choice = _consoleView.ReadMainMenuChoice();

            if (choice == "0")
            {
                return;
            }

            try
            {
                if (choice == "1")
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

                else if (choice == "8")
                {
                    await SearchPeopleByNameAsync();
                }

                else
                {
                    _consoleView.ShowInvalidMenuChoice();
                }
            }

            catch (SqlException ex)
            {
                _consoleView.ShowDatabaseError(ex.Message);
            }

            // Show validation failures without saving invalid information. 
            catch (ArgumentException ex)
            {
                _consoleView.ShowValidationError(ex.Message);
            }
        }
    }

    private async Task ListPeopleAsync()
    {
        IReadOnlyList<PersonSummary> people =
            await _festivalService.GetAllPeopleAsync();
        
        _consoleView.ShowPeople(people);
    }

/// <summary>
/// Filter the search result by role.
/// </summary>

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
            await _festivalService.GetPeopleByRoleAsync(role);
        
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

        int personID = await _festivalService.AddCrewAsync(
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

        int personID = await _festivalService.AddPerformerAsync(
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

        int personID = await _festivalService.AddVendorAsync(
            name,
            telephone,
            email,
            categories
        );

        _consoleView.ShowVendorAdded(personID);
    }
/// <summary>
/// Reads an ID and edit the details of the corresponding person.
/// </summary>
    private async Task EditPersonAsync()
    {
        int personID = _consoleView.ReadPersonID("PersonID to edit: ");

        PersonSummary? person = 
            await _festivalService.GetPersonByIDAsync(personID);
        
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

        bool updated = await _festivalService.UpdatePersonAsync(
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
/// <summary>
/// Checks if the person exists and confirm deletions before removing the record.
/// </summary>
    private async Task DeletePersonAsync()
    {
        int personID = _consoleView.ReadPersonID("PersonID to delete: ");

        PersonSummary? person =
            await _festivalService.GetPersonByIDAsync(personID);
        
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

        bool deleted = await _festivalService.DeletePersonAsync(personID);
        _consoleView.ShowDeletionResult(personID, deleted);
    }

/// <summary>
/// Filter the search by names.
/// </summary>

    private async Task SearchPeopleByNameAsync()
    {
        string searchTerm = _consoleView.ReadRequiredText("Name to search: ", 150);
    
        IReadOnlyList<PersonSummary> people =
            await _festivalService.SearchPeopleByNameAsync(searchTerm);

        _consoleView.ShowPeopleSearchResults(people);
    }
}
