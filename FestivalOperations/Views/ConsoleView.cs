using System.Globalization;
using FestivalOperations.Data;
namespace FestivalOperations.Views;

/// <summary>
/// Handles console prompts and their output ONLY, it does not access the database.
/// </summary>
public sealed class ConsoleView
{
    /// <summary>
    /// Displays the main menu for the application and returns the entered choice, or null at the end of the input.
    /// </summary>
    /// <returns>The trimmed menu choice, or <see langword="null"/> when the input stream ends.</returns>
    /// <remarks>This method only reads and displays console text; menu actions belong to the controller.</remarks>
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
    /// <summary>Displays the role menu and returns the entered choice, or null at end of input.</summary>
    /// <returns>The trimmed menu choice, or <see langword="null"/> if input has ended.</returns>
    /// <remarks>The controller converts this short choice into a role name and handles the back option.</remarks>
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

    /// <summary>Explains that the selected role menu option is not recognized.</summary>
    /// <remarks>Keeping the retry message in the view avoids putting presentation text in the controller.</remarks>
    public void ShowInvalidRoleChoice()
    {
        Console.WriteLine("Please choose from the provided options above.");
    }
    /// <summary>Prints each person's ID, name, and role, or an empty-result message.</summary>
    /// <param name="people">The people returned by the service.</param>
    /// <remarks>The view receives already-loaded summaries and performs no database query.</remarks>
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
    /// <summary>Prints contact and role details for each matching person.</summary>
    /// <param name="role">The role used for the search and empty-result message.</param>
    /// <param name="people">The matching people and their formatted role details.</param>
    /// <remarks>Role details are formatted by the data layer; this method only presents the returned values.</remarks>
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

    /// <summary>Prompts the user to choose one of the displayed main-menu options.</summary>
    /// <remarks>This method reports the invalid choice; the main menu loop remains active in the controller.</remarks>
    public void ShowInvalidMenuChoice()
    {
        Console.WriteLine("Please choose from the options provided above.");
    }
    /// <summary>Repeats the prompt until non-empty text within the length limit is entered.</summary>
    /// <param name="prompt">The prompt displayed before reading input.</param>
    /// <param name="maxLength">The maximum accepted number of characters.</param>
    /// <returns>The trimmed text value.</returns>
    /// <remarks>All names and contact fields use this helper so blank or overlong input is rejected consistently.</remarks>
    public string ReadRequiredText(string prompt, int maxLength)
    {
        while (true)
        {
            Console.Write(prompt);
            string? value = Console.ReadLine()?.Trim();

            if (string.IsNullOrWhiteSpace(value))
            {
                // Reject whitespace-only input as well as an empty line.
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

    /// <summary>Reads a non-negative crew hourly rate that fits the database decimal range.</summary>
    /// <returns>The validated hourly rate.</returns>
    /// <remarks>Invariant parsing expects a dot decimal separator regardless of the computer's locale.</remarks>
    public decimal ReadCrewHourlyRate()
    {
        while (true)
        {
            Console.Write("Hourly rate (for example, 15.00): ");
            string? input = Console.ReadLine();

            // InvariantCulture keeps decimal input consistent with the database's decimal format.
            if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal rate)
                && rate >= 0
                && rate <= 999999.99m)
            {
                return rate;
            }

            Console.WriteLine("Enter a rate: ");
        }
    }

    /// <summary>Accepts FullTime or PartTime without regard to casing and returns canonical spelling.</summary>
    /// <returns>Either FullTime or PartTime.</returns>
    /// <remarks>The normalized value is later used to select the correct weekly-hours range.</remarks>
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

    /// <summary>Reads hours that fall within the range for the selected employment type.</summary>
    /// <param name="employmentType">The previously selected employment type.</param>
    /// <returns>The validated weekly hours.</returns>
    /// <remarks>The bounds match the crew business rules enforced by the Crew model.</remarks>
    public int ReadCrewWeeklyHours(string employmentType)
    {
        // Full-time and part-time crew have different allocated working hour ranges.
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

    /// <summary>Confirms that a crew member was saved and displays the generated ID.</summary>
    /// <param name="personID">The ID returned by the repository.</param>
    /// <remarks>The ID is shown so the user can identify the new record in later operations.</remarks>
    public void ShowCrewAdded(int personID)
    {
        Console.WriteLine($"New member added with PersonID {personID}.");
    }
    /// <summary>Reads a non-negative performer fee that fits the database decimal range.</summary>
    /// <returns>The validated performance fee.</returns>
    /// <remarks>The fee uses invariant-culture parsing so values match the expected decimal format.</remarks>
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

    /// <summary>Splits comma-separated input and accepts exactly two distinct, valid genres.</summary>
    /// <returns>The two trimmed genre names.</returns>
    /// <remarks>Names differing only by letter casing count as duplicates and do not satisfy the required count.</remarks>
    public List<string> ReadPerformerGenres()
    {
        while (true)
        {
            Console.Write("Enter exactly 2 genres:  ");
            string? input = Console.ReadLine();

            // Remove empty entries and trim each item before checking distinctness and count.
            List<string> genres = (input ?? string.Empty).Split(
                ',',
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries
            )
            // The comparison deliberately ignores casing, while preserving the first spelling entered.
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

            if (genres.Count != 2)
            {
                Console.WriteLine("Enter exactly 2 genres.");
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

    /// <summary>Confirms that a performer was saved and displays the generated ID.</summary>
    /// <param name="personID">The ID returned by the repository.</param>
    /// <remarks>The controller calls this only after the service and repository complete successfully.</remarks>
    public void ShowPerformerAdded(int personID)
    {
        Console.WriteLine($"Performer added with PersonID {personID}.");
    }

    /// <summary>Splits comma-separated input and accepts exactly three distinct categories.</summary>
    /// <returns>The three trimmed category names.</returns>
    /// <remarks>Blank entries are discarded and category matching ignores letter casing.</remarks>
    public List<string> ReadVendorCategories()
    {
        while (true)
        {
            Console.Write("Enter exactly 3 product categories: ");
            string? input = Console.ReadLine();

            // Normalize each entry before applying the case-insensitive duplicate check.
            List<string> categories = (input ?? string.Empty).Split(
                ',',
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries
            )
            // Treat differently-cased spellings of the same category as one selection.
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        
            if (categories.Count != 3)
            {
                Console.WriteLine("Enter exactly 3 different categories.");
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

    /// <summary>Confirms that a vendor was saved and displays the generated ID.</summary>
    /// <param name="personID">The ID returned by the repository.</param>
    /// <remarks>The generated identifier can be used to select this record for editing or deletion.</remarks>
    public void ShowVendorAdded(int personID)
    {
        Console.WriteLine($"Vendor added with PersonID {personID}.");
    }
    /// <summary>Repeats until the user enters a positive integer person ID.</summary>
    /// <param name="prompt">The prompt displayed before reading the ID.</param>
    /// <returns>The positive ID entered by the user.</returns>
    /// <remarks>IDs must be positive because zero is reserved for new, unsaved model entities.</remarks>
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

    /// <summary>Reports that no person exists for the requested ID.</summary>
    /// <param name="personID">The ID that was not found.</param>
    /// <remarks>The caller can then return to the menu without attempting an update or delete.</remarks>
    public void ShowPersonNotFound(int personID)
    {
        Console.WriteLine($"No person found with PersonID {personID}.");
    }

    /// <summary>Displays the selected person and asks for explicit deletion confirmation.</summary>
    /// <param name="person">The person whose details are shown for confirmation.</param>
    /// <returns><see langword="true"/> only when the user enters YES; otherwise, <see langword="false"/>.</returns>
    /// <remarks>Confirmation is separate from deletion so the controller can cancel safely.</remarks>
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
/// <summary>
/// Displays whether the person was deleted or not found.
/// </summary>

    /// <summary>Prints whether the delete operation removed the requested person.</summary>
    /// <param name="personID">The ID the controller attempted to delete.</param>
    /// <param name="deleted">Whether the repository reported a deleted row.</param>
    /// <remarks>The repository's affected-row result distinguishes a successful delete from a missing ID.</remarks>
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

    /// <summary>Confirms that a deletion was cancelled before reaching the service.</summary>
    /// <remarks>No database change is made when the user declines confirmation.</remarks>
    public void ShowDeletionCancelled()
    {
        Console.WriteLine("Deletion cancelled.");
    }

    /// <summary>Displays which role and ID are about to be edited.</summary>
    /// <param name="role">The person's stored role.</param>
    /// <param name="personID">The person's ID.</param>
    /// <remarks>The controller obtains the role from the stored record before prompting for role-specific data.</remarks>
    public void ShowEditingPerson(string role, int personID)
    {
        Console.WriteLine($"Editing {role} with PersonID {personID}.");
    }

    /// <summary>Reports a role value that the application does not support.</summary>
    /// <param name="role">The unsupported role value.</param>
    /// <remarks>This protects the user from being told an update succeeded for an unrecognized role.</remarks>
    public void ShowUnknownRole(string role)
    {
        Console.WriteLine($"Unknown role '{role}'.");
    }

    /// <summary>Confirms that the person update completed successfully.</summary>
    /// <param name="personID">The ID of the updated person.</param>
    /// <remarks>This message is displayed only when the service reports that the ID was found and updated.</remarks>
    public void ShowPersonUpdated(int personID)
    {
        Console.WriteLine($"Person {personID} updated successfully.");
    }
    /// <summary>Prints matching people or explains that the search returned no results.</summary>
    /// <param name="people">The people returned by the name search.</param>
    /// <remarks>The view does not interpret search terms; filtering is performed by the repository.</remarks>
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

    /// <summary>Displays a database error message supplied by the controller.</summary>
    /// <param name="message">The error details to display.</param>
    /// <remarks>The view formats the message but leaves exception handling to the controller.</remarks>
    public void ShowDatabaseError(string message)
    {
        Console.WriteLine($"Database error: {message}");
    }

    /// <summary>Displays a validation error without exposing database operations to the view.</summary>
    /// <param name="message">The validation details to display.</param>
    /// <remarks>The controller catches validation exceptions and passes only their message to this method.</remarks>
    public void ShowValidationError(string message)
    {
        Console.WriteLine($"Invalid input: {message}");
    }
}
