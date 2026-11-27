using FestivalOperations.Data;
using FestivalOperations.Models;

namespace FestivalOperations.Services;

/// <summary>
/// Applies domain validation through the models and delegates database operations to the repository.
/// </summary>
public sealed class FestivalService
{
    private readonly PersonRepository _personRepository;

    /// <summary>Creates the service with the repository used for persistent storage.</summary>
    /// <param name="personRepository">The data-access object for festival people.</param>
    public FestivalService(PersonRepository personRepository)
    {
        ArgumentNullException.ThrowIfNull(personRepository);
        _personRepository = personRepository;
    }
    /// <summary>Retrieves the basic display details for all people.</summary>
    /// <returns>Every person's ID, name, and role.</returns>
    /// <remarks>The repository owns the query; this method keeps the controller independent of SQL.</remarks>
    public Task<IReadOnlyList<PersonSummary>> GetAllPeopleAsync()
    {
        return _personRepository.GetAllPeopleAsync();
    }
    /// <summary>Retrieves people assigned to one role, including their role-specific details.</summary>
    /// <param name="role">The role to filter by.</param>
    /// <returns>The matching people and their role details.</returns>
    /// <remarks>The service forwards the role filter without embedding data-access commands.</remarks>
    public Task<IReadOnlyList<PersonRoleSummary>> GetPeopleByRoleAsync(string role)
    {
        return _personRepository.GetPeopleByRoleAsync(role);
    }
    /// <summary>Finds people whose names contain the supplied text.</summary>
    /// <param name="searchTerm">The partial name to search for.</param>
    /// <returns>The matching people, or an empty list when there are no matches.</returns>
    /// <remarks>Name matching is implemented by the repository so all filtering remains in the data layer.</remarks>
    public Task<IReadOnlyList<PersonSummary>> SearchPeopleByNameAsync(string searchTerm)
    {
        return _personRepository.SearchPeopleByNameAsync(searchTerm);
    }
    /// <summary>Looks up a person's basic details by database ID.</summary>
    /// <param name="personID">The ID to look up.</param>
    /// <returns>The person's summary when found; otherwise, <see langword="null"/>.</returns>
    /// <remarks>A missing result lets the controller handle not-found feedback before editing or deleting.</remarks>
    public Task<PersonSummary?> GetPersonByIDAsync(int personID)
    {
        return _personRepository.GetPersonByIDAsync(personID);
    }

    /// <summary>Validates crew details through the Crew model, then stores them.</summary>
    /// <param name="name">The crew member's name.</param>
    /// <param name="telephone">The crew member's telephone number.</param>
    /// <param name="email">The crew member's email address.</param>
    /// <param name="hourlyRate">The non-negative hourly pay rate.</param>
    /// <param name="employmentType">FullTime or PartTime.</param>
    /// <param name="weeklyHours">Weekly hours valid for the employment type.</param>
    /// <returns>The new database PersonID.</returns>
    /// <remarks>The Crew model enforces schedule rules; only its validated values are passed to storage.</remarks>
    public async Task<int> AddCrewAsync(
        string name,
        string telephone,
        string email,
        decimal hourlyRate,
        string employmentType,
        int weeklyHours
    )
    {
        // Constructing the model applies the domain rules before data reaches SQL.
        var crew = new Crew(
            0,
            name,
            telephone,
            email,
            hourlyRate,
            employmentType,
            weeklyHours
        );

        // Pass normalized properties from the validated entity rather than the original console strings.
        return await _personRepository.AddCrewAsync(
            crew.Name,
            crew.Telephone,
            crew.Email,
            crew.HourlyRate,
            crew.EmploymentType,
            crew.WeeklyHours
        );
    }
    /// <summary>Validates performer details and stores the performer with their genres.</summary>
    /// <param name="name">The performer's name.</param>
    /// <param name="telephone">The performer's telephone number.</param>
    /// <param name="email">The performer's email address.</param>
    /// <param name="fee">The performer's non-negative performance fee.</param>
    /// <param name="genres">Exactly two distinct performer genres.</param>
    /// <returns>The new database PersonID.</returns>
    /// <remarks>The Performer model is the source of truth for fee and genre validation.</remarks>
    public async Task<int> AddPerformerAsync(
        string name,
        string telephone,
        string email,
        decimal fee,
        List<string> genres
    )
    {
        // The model normalizes contact text and enforces the fee and two-genre rules.
        var performer = new Performer(
            0,
            name,
            telephone,
            email,
            fee,
            genres
        );

        // The repository receives the model's normalized contact fields and validated genre collection.
        return await _personRepository.AddPerformerAsync(
            performer.Name,
            performer.Telephone,
            performer.Email,
            performer.Fee,
            performer.Genres
        );
    }
    /// <summary>Validates vendor details and stores the vendor with their product categories.</summary>
    /// <param name="name">The vendor's name.</param>
    /// <param name="telephone">The vendor's telephone number.</param>
    /// <param name="email">The vendor's email address.</param>
    /// <param name="categories">Exactly three distinct product categories.</param>
    /// <returns>The new database PersonID.</returns>
    /// <remarks>The Vendor model enforces the category count before any database write begins.</remarks>
    public async Task<int> AddVendorAsync(
        string name,
        string telephone,
        string email,
        List<string> categories
    )
    {
        // The model normalizes contact text and enforces the three-category rule.
        var vendor = new Vendor(
            0,
            name,
            telephone,
            email,
            categories
        );

        // Persist the validated entity values, not the unprocessed input supplied by the controller.
        return await _personRepository.AddVendorAsync(
            vendor.Name,
            vendor.Telephone,
            vendor.Email,
            vendor.ProductCategories
        );
    }

    /// <summary>Validates updated details with the model matching the person's role, then saves them.</summary>
    /// <param name="personID">The database ID of the person to update.</param>
    /// <param name="role">The person's role: Performer, Crew, or Vendor.</param>
    /// <param name="name">The updated name.</param>
    /// <param name="telephone">The updated telephone number.</param>
    /// <param name="email">The updated email address.</param>
    /// <param name="fee">The updated performer fee.</param>
    /// <param name="genres">The updated performer genres.</param>
    /// <param name="hourlyRate">The updated crew hourly rate.</param>
    /// <param name="employmentType">The updated crew employment type.</param>
    /// <param name="weeklyHours">The updated crew weekly hours.</param>
    /// <param name="categories">The updated vendor categories.</param>
    /// <returns><see langword="true"/> if the person was updated; otherwise, <see langword="false"/>.</returns>
    /// <remarks>Only the model matching the stored role is constructed, so unrelated role rules do not run.</remarks>
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
    {
        if (role == "Performer")
        {
            // Build only the matching role model so its validation rules run before storage.
            // Other role arguments are passed as empty defaults because the repository ignores them for performers.
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
            // Crew construction also checks that weekly hours fit the selected employment type.
            // The unused performer and vendor arguments are supplied as defaults for the repository signature.
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
            // Vendor construction enforces exactly three distinct product categories.
            // Unused performer and crew values are defaults and are not written for vendor records.
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

        // An unsupported role has no valid model to construct and must not be sent to the repository.
        return false;
    }
    /// <summary>Deletes a person through the repository.</summary>
    /// <param name="personID">The ID of the person to delete.</param>
    /// <returns><see langword="true"/> when a row was deleted; otherwise, <see langword="false"/>.</returns>
    /// <remarks>The controller is responsible for obtaining user confirmation before calling this method.</remarks>
    public Task<bool> DeletePersonAsync(int personID)
    {
        return _personRepository.DeletePersonAsync(personID);
    }
}
