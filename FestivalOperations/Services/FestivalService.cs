using FestivalOperations.Data;
using FestivalOperations.Models;

namespace FestivalOperations.Services;

public sealed class FestivalService
{
    private readonly PersonRepository _personRepository;

    public FestivalService(PersonRepository personRepository)
    {
        ArgumentNullException.ThrowIfNull(personRepository);
        _personRepository = personRepository;
    }

    public Task<IReadOnlyList<PersonSummary>> GetAllPeopleAsync()
    {
        return _personRepository.GetAllPeopleAsync();
    }

    public Task<IReadOnlyList<PersonRoleSummary>> GetPeopleByRoleAsync(string role)
    {
        return _personRepository.GetPeopleByRoleAsync(role);
    }

    public Task<IReadOnlyList<PersonSummary>> SearchPeopleByNameAsync(string searchTerm)
    {
        return _personRepository.SearchPeopleByNameAsync(searchTerm);
    }

    public Task<PersonSummary?> GetPersonByIDAsync(int personID)
    {
        return _personRepository.GetPersonByIDAsync(personID);
    }

    public async Task<int> AddCrewAsync(
        string name,
        string telephone,
        string email,
        decimal hourlyRate,
        string employmentType,
        int weeklyHours
    )
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

    public async Task<int> AddPerformerAsync(
        string name,
        string telephone,
        string email,
        decimal fee,
        List<string> genres
    )
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

    public async Task<int> AddVendorAsync(
        string name,
        string telephone,
        string email,
        List<string> categories
    )
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

    public Task<bool> DeletePersonAsync(int personID)
    {
        return _personRepository.DeletePersonAsync(personID);
    }
}
