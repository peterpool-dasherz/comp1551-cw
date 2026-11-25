using FestivalOperations.Data;
using FestivalOperations.Models;

namespace FestivalOperations.Services;

/// <summary>
/// Applies business rules and move data storage to the person repository.
/// </summary>
public sealed class FestivalService
{
    private readonly PersonRepository _personRepository;

    public FestivalService(PersonRepository personRepository)
    {
        ArgumentNullException.ThrowIfNull(personRepository);
        _personRepository = personRepository;
    }
/// <summary>
/// Returns the summaries of all people in the repository.
/// </summary>

    public Task<IReadOnlyList<PersonSummary>> GetAllPeopleAsync()
    {
        return _personRepository.GetAllPeopleAsync();
    }
/// <summary>
/// Returns people using their role and their role-specific details as a filter.
/// </summary>
    public Task<IReadOnlyList<PersonRoleSummary>> GetPeopleByRoleAsync(string role)
    {
        return _personRepository.GetPeopleByRoleAsync(role);
    }
/// <summary>
/// Returns people using their names (and names that contain the search term).
/// </summary>

    public Task<IReadOnlyList<PersonSummary>> SearchPeopleByNameAsync(string searchTerm)
    {
        return _personRepository.SearchPeopleByNameAsync(searchTerm);
    }
/// <summary>
/// Returns a person's details summary by their ID.
/// </summary>
    public Task<PersonSummary?> GetPersonByIDAsync(int personID)
    {
        return _personRepository.GetPersonByIDAsync(personID);
    }

/// <summary>
/// Validates the Crew details before saving into the repository.
/// </summary>
    public async Task<int> AddCrewAsync(
        string name,
        string telephone,
        string email,
        decimal hourlyRate,
        string employmentType,
        int weeklyHours
    )
    // Crew entity construction needs to validates its details before repository storage.
    {
        var crew = new Crew(
            0,
            name,
            telephone,
            email,
            hourlyRate,
            employmentType,
            weeklyHours
        );

        return await _personRepository.AddCrewAsync(
            crew.Name,
            crew.Telephone,
            crew.Email,
            crew.HourlyRate,
            crew.EmploymentType,
            crew.WeeklyHours
        );
    }
/// <summary>
/// Validates and saves a performer with their details and their genres.
/// </summary>

    public async Task<int> AddPerformerAsync(
        string name,
        string telephone,
        string email,
        decimal fee,
        List<string> genres
    )
    // The model normalizes contact details and validates the fee and genre count of the performer.
    {
        var performer = new Performer(
            0,
            name,
            telephone,
            email,
            fee,
            genres
        );

        return await _personRepository.AddPerformerAsync(
            performer.Name,
            performer.Telephone,
            performer.Email,
            performer.Fee,
            performer.Genres
        );
    }
/// <summary>
/// Validates and saves a vendor with their detauls and their product categories.
/// </summary>

    public async Task<int> AddVendorAsync(
        string name,
        string telephone,
        string email,
        List<string> categories
    )
    // The model normalizes contact details and validates the required product categories for the vendor.
    {
        var vendor = new Vendor(
            0,
            name,
            telephone,
            email,
            categories
        );

        return await _personRepository.AddVendorAsync(
            vendor.Name,
            vendor.Telephone,
            vendor.Email,
            vendor.ProductCategories
        );
    }

/// <summary>
/// Validates updated details before saving.
/// </summary>
    public async Task<bool> UpdatePersonAsync(
        int personID,
        string role,
        string name,
        string telephone,
        string email,
        decimal fee,
        List<string> genres,
        decimal hourlyRate,
        string employmentType,
        int weeklyHours,
        List<string> categories
    )
    // Use the matching model to validate the fields for the person's role. 
    {
        if (role == "Performer")
        {
            var performer = new Performer(
                personID,
                name,
                telephone,
                email,
                fee,
                genres
            );

            return await _personRepository.UpdatePersonAsync(
                personID,
                role,
                performer.Name,
                performer.Telephone,
                performer.Email,
                performer.Fee,
                performer.Genres.ToList(),
                0,
                string.Empty,
                0,
                new List<string>()
            );
        }

        if (role == "Crew")
        {
            var crew = new Crew(
                personID,
                name,
                telephone,
                email,
                hourlyRate,
                employmentType,
                weeklyHours
            );

            return await _personRepository.UpdatePersonAsync(
                personID,
                role,
                crew.Name,
                crew.Telephone,
                crew.Email,
                0,
                new List<string>(),
                crew.HourlyRate,
                crew.EmploymentType,
                crew.WeeklyHours,
                new List<string>()
            );
        }

        if (role == "Vendor")
        {
            var vendor = new Vendor(
                personID,
                name,
                telephone,
                email,
                categories
            );

            return await _personRepository.UpdatePersonAsync(
                personID,
                role,
                vendor.Name,
                vendor.Telephone,
                vendor.Email,
                0,
                new List<string>(),
                0,
                string.Empty,
                0,
                vendor.ProductCategories.ToList()
            );
        }

        return false;
    }
/// <summary>
/// Deletes the person with the specified ID through the repository.
/// </summary>
    public Task<bool> DeletePersonAsync(int personID)
    {
        return _personRepository.DeletePersonAsync(personID);
    }
}
