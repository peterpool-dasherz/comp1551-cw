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
}
