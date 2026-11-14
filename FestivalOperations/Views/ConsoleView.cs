using System.Globalization;
using FestivalOperations.Data;
namespace FestivalOperations.Views;

public sealed class ConsoleView
{
    public string? ReadMainMenuChoice()
    {
        Console.WriteLine();
        Console.WriteLine("1. List people");
        Console.WriteLine("2. Add crew member");
        Console.WriteLine("3. Add performer");
        Console.WriteLine("4. Add vendor");
        Console.WriteLine("5. View people by role");
        Console.WriteLine("6. Edit person by ID");
        Console.WriteLine("7. Delete person by ID");
        Console.WriteLine("8. Search people by name.");
        Console.WriteLine("0. Exit");
        Console.Write("Choose an option: ");

        return Console.ReadLine()?.Trim();
    }  

    public string? ReadRoleChoice()
    {
        Console.WriteLine();
        Console.WriteLine("1. Performers");
        Console.WriteLine("2. Crew");
        Console.WriteLine("3. Vendors");
        Console.WriteLine("0. Back to main menu");
        Console.Write("Choose a role: ");

        return Console.ReadLine()?.Trim();
    }

    public void ShowInvalidRoleChoice()
    {
        Console.WriteLine("Please choose from the provided options above.");
    }

    public void ShowPeople(IReadOnlyList<PersonSummary> people)
    {
        Console.WriteLine();
        Console.WriteLine("ID | Name | Role");

        if (people.Count == 0)
        {
            Console.WriteLine("No people found.");
            return;
        }

        foreach (PersonSummary person in people)
        {
            Console.WriteLine($"{person.PersonID} | {person.Name} | {person.Role}");
        }
    }

    public void ShowPeopleByRole(
        string role,
        IReadOnlyList<PersonRoleSummary> people
    )
    {
        Console.WriteLine();
        Console.WriteLine("ID | Name | Telephone | Email | Role | Details");

        if (people.Count == 0)
        {
            Console.WriteLine($"No {role.ToLowerInvariant()} records found.");
            return;
        }

        foreach (PersonRoleSummary person in people)
        {
            Console.WriteLine(
                $"{person.PersonID} | {person.Name} | {person.Telephone} | " + 
                $"{person.Email} | {person.Role} | {person.RoleDetails}"
            );
        }
    }

    public void ShowInvalidMenuChoice()
    {
        Console.WriteLine("Please choose from the options provided above.");
    }

    public string ReadRequiredText(string prompt, int maxLength)
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
                Console.WriteLine(
                    $"This field must be {maxLength} characters or fewer."
                );
            }

            else
            {
                return value;
            }
        }
    }

    public decimal ReadCrewHourlyRate()
    {
        while (true)
        {
            Console.Write("Hourly rate (for example, 15.00): ");
            string? input = Console.ReadLine();

            if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal rate)
                && rate >= 0
                && rate <= 999999.99m)
            {
                return rate;
            }

            Console.WriteLine("Enter a rate: ");
        }
    }

    public string ReadEmploymentType()
    {
        while (true)
        {
            Console.Write("Employment type (FullTime or PartTime): ");
            string? input = Console.ReadLine()?.Trim();

            if (string.Equals(input, "FullTime", StringComparison.OrdinalIgnoreCase))
            {
                return "FullTime";
            }

            if (string.Equals(input, "PartTime", StringComparison.OrdinalIgnoreCase))
            {
                return "PartTime";
            }

            Console.WriteLine("Enter FullTime or PartTime: ");
        }
    }

    public int ReadCrewWeeklyHours(string employmentType)
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

            Console.WriteLine($"Enter a whole number from {minimumHours} to {maximumHours}.");
        }
    }

    public void ShowCrewAdded(int personID)
    {
        Console.WriteLine($"New member added with PersonID {personID}.");
    }

    public decimal ReadPerformerFee()
    {
        while (true)
        {
            Console.Write("Performance fee (for example, 500.00): ");
            string? input = Console.ReadLine();

            if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal fee)
                && fee >= 0
                && fee <= 99999999.99m)
            {
                return fee;
            }

            Console.WriteLine("Please enter a valid fee.");
        }
    }

    public List<string> ReadPerformerGenres()
    {
        while (true)
        {
            Console.Write("Genres: ");
            string? input = Console.ReadLine();

            List<string> genres = (input ?? string.Empty).Split(
                ',',
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries
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

    public void ShowPerformerAdded(int personID)
    {
        Console.WriteLine($"Performer added with PersonID {personID}.");
    }

    public List<string> ReadVendorCategories()
    {
        while (true)
        {
            Console.Write("Product categories: ");
            string? input = Console.ReadLine();

            List<string> categories = (input ?? string.Empty).Split(
                ',',
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries
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

    public void ShowVendorAdded(int personID)
    {
        Console.WriteLine($"Vendor added with PersonID {personID}.");
    }

    public int ReadPersonID(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            string? input = Console.ReadLine();

            if (int.TryParse(input, out int personID) && personID > 0)
            {
                return personID;
            }

            Console.WriteLine("Enter a valid PersonID.");
        }
    }

    public void ShowPersonNotFound(int personID)
    {
        Console.WriteLine($"No person found with PersonID {personID}.");
    }

    public bool ConfirmPersonDeletion(PersonSummary person)
    {
        Console.WriteLine($"Found: {person.Name} ({person.Role}), PersonID {person.PersonID}.");
        Console.Write("Type YES to confirm deletion: ");

        string? confirmation = Console.ReadLine()?.Trim();

        return string.Equals(
            confirmation,
            "YES",
            StringComparison.OrdinalIgnoreCase
        );
    }

    public void ShowDeletionResult(int personID, bool deleted)
    {
        if (deleted)
        {
            Console.WriteLine($"Person {personID} was deleted.");
        }

        else
        {
            Console.WriteLine($"Person {personID} was not found.");
        }
    }

    public void ShowDeletionCancelled()
    {
        Console.WriteLine("Deletion cancelled.");
    }

    public void ShowEditingPerson(string role, int personID)
    {
        Console.WriteLine($"Editing {role} with PersonID {personID}.");
    }

    public void ShowUnknownRole(string role)
    {
        Console.WriteLine($"Unknown role '{role}'.");
    }

    public void ShowPersonUpdated(int personID)
    {
        Console.WriteLine($"Person {personID} updated successfully.");
    }

    public void ShowPeopleSearchResults(IReadOnlyList<PersonSummary> people)
    {
        Console.WriteLine();
        Console.WriteLine("ID | Name | Role");

        if (people.Count == 0)
        {
            Console.WriteLine("No matching people found.");
            return;
        }

        foreach (PersonSummary person in people)
        {
            Console.WriteLine($"{person.PersonID} | {person.Name} | {person.Role}");
        }
    }
}
