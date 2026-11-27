using Microsoft.Data.SqlClient;
using FestivalOperations.Data;
using FestivalOperations.Views;
using FestivalOperations.Services;

namespace FestivalOperations.Controllers;

/// <summary>
/// Routes menu choices to the corresponding service and sends results to the console view to display them.
/// </summary>
/// <remarks>The controller coordinates the presentation and business layers without issuing SQL.</remarks>
public sealed class FestivalController
{
    private readonly FestivalService _festivalService;
    private readonly ConsoleView _consoleView;

    /// <summary>Creates the controller with the business service and console view it coordinates.</summary>
    /// <param name="festivalService">The service that applies rules and accesses stored people.</param>
    /// <param name="consoleView">The view that reads user input and prints results.</param>
    public FestivalController(FestivalService festivalService, ConsoleView consoleView)
    {
        _festivalService = festivalService;
        _consoleView = consoleView;
    }

/// <summary>
/// Displays the main menu until the user chooses to exit.
/// </summary>
/// <remarks>Database and validation exceptions are caught here so one failed action does not end the menu loop.</remarks>
    public async Task RunAsync()
    {
        // Each iteration handles one menu action, then returns to the menu unless the user exits.
        while (true)
        {
            string? choice = _consoleView.ReadMainMenuChoice();

            // Return immediately so no further input or database work occurs after exit is selected.
            if (choice == "0")
            {
                return;
            }

            try
            {
                // Dispatch menu choices to action methods; those methods keep input and service calls ordered.
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
                // A failed database action is reported, then the loop returns control to the menu.
                _consoleView.ShowDatabaseError(ex.Message);
            }

            // Show validation failures without saving invalid information. 
            catch (ArgumentException ex)
            {
                // Model and service validation exceptions become user-facing messages here.
                _consoleView.ShowValidationError(ex.Message);
            }
        }
    }

    /// <summary>Retrieves all people and asks the view to print the list.</summary>
    /// <remarks>This action is read-only and delegates both retrieval and presentation to their own layers.</remarks>
    private async Task ListPeopleAsync()
    {
        // The service returns summaries so the controller does not construct display rows itself.
        IReadOnlyList<PersonSummary> people =
            await _festivalService.GetAllPeopleAsync();
        
        _consoleView.ShowPeople(people);
    }

    /// <summary>Prompts for a role, retrieves matching people, and displays their details.</summary>
    /// <remarks>An invalid role-menu choice is retried; choosing Back returns without querying the service.</remarks>
    private async Task ViewPeopleByRoleAsync()
    {
        string? role = null;

        while (role is null)
        {
            string? choice = _consoleView.ReadRoleChoice();

            // Convert the short menu choice into the role name used by the service and database.
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
            // Returning here avoids treating the back choice as an actual role filter.
            return;
        }

        IReadOnlyList<PersonRoleSummary> people = 
            await _festivalService.GetPeopleByRoleAsync(role);
        
        _consoleView.ShowPeopleByRole(role, people);
    }

    /// <summary>Collects crew contact and work details, saves them, and displays the new ID.</summary>
    /// <remarks>Input order follows the crew model so weekly-hour validation can use the employment type.</remarks>
    private async Task AddCrewAsync()
    {
        // Collect common contact fields first; the view rejects blank and overlong values.
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

    /// <summary>Collects performer contact, fee, and genre details and saves the new performer.</summary>
    /// <remarks>Genre input is collected only after the contact fields and fee have been accepted.</remarks>
    private async Task AddPerformerAsync()
    {
        // All three roles share the same validated contact prompts.
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

    /// <summary>Collects vendor contact and category details and saves the new vendor.</summary>
    /// <remarks>Category input is validated by the view and checked again by the vendor model.</remarks>
    private async Task AddVendorAsync()
    {
        // Vendor records have shared contact fields plus a separately validated category list.
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
    /// <summary>Finds a person, collects replacement details for their role, and saves the update.</summary>
    /// <remarks>If the ID is absent, the method reports that result and returns before asking for new values.</remarks>
    private async Task EditPersonAsync()
    {
        int personID = _consoleView.ReadPersonID("PersonID to edit: ");

        PersonSummary? person = 
            await _festivalService.GetPersonByIDAsync(personID);
        
        if (person is null)
        {
            // Stop before collecting replacement values when there is no record to edit.
            _consoleView.ShowPersonNotFound(personID);
            return;
        }

        // The stored role determines which role-specific prompts are needed for the update.
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
        // Prompt for role-specific details needed for this person.
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

        // The service validates the new values before the repository changes the database.
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
            // The repository's result indicates that the ID matched a persisted row.
            _consoleView.ShowPersonUpdated(personID);
        }

        else
        {
            _consoleView.ShowPersonNotFound(personID);
        }
    }
    /// <summary>Checks that the person exists, requires confirmation, and then requests deletion.</summary>
    /// <remarks>The pre-check gives the user the person's name and role before they confirm the removal.</remarks>
    private async Task DeletePersonAsync()
    {
        int personID = _consoleView.ReadPersonID("PersonID to delete: ");

        PersonSummary? person =
            await _festivalService.GetPersonByIDAsync(personID);
        
        if (person is null)
        {
            // Never ask for confirmation or issue a delete for an unknown identifier.
            _consoleView.ShowPersonNotFound(personID);
            return;
        }

        // Do not call the delete operation unless the user explicitly confirms it.
        if (!_consoleView.ConfirmPersonDeletion(person))
        {
            _consoleView.ShowDeletionCancelled();
            return;
        }

        bool deleted = await _festivalService.DeletePersonAsync(personID);
        _consoleView.ShowDeletionResult(personID, deleted);
    }

    /// <summary>Reads a name fragment, searches through the service, and displays matching people.</summary>
    /// <remarks>The search term is required text, and an empty result is reported by the view.</remarks>
    private async Task SearchPeopleByNameAsync()
    {
        string searchTerm = _consoleView.ReadRequiredText("Name to search: ", 150);
    
        IReadOnlyList<PersonSummary> people =
            await _festivalService.SearchPeopleByNameAsync(searchTerm);

        // The view presents matches and owns the empty-result message.
        _consoleView.ShowPeopleSearchResults(people);
    }
}
