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
                Console.WriteLine($"This field must be {maxLength}");
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
        int maximumHours = employmentType == "PartTime" ? 40 : 24;

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
}
