namespace FestivalOperations.Models;

/// <summary>
/// Contact details of everybody that is involved in the festival.
/// </summary>

public abstract class Person
{

    // Unsaved entity marks a new person, the database will assign its permanent ID.
    public int PersonID { get; private set;}
    public string Name { get; private set;}
    public string Telephone { get; private set;}
    public string Email { get; private set;}

    protected Person(
        int personID,
        string name,
        string telephone,
        string email)
    {
        if (personID < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(personID),
                "An error occurred."
            );
        }

        PersonID = personID;
        Name = RequireText(name, nameof(name));
        Telephone = RequireText(telephone, nameof(telephone));
        Email = RequireText(email, nameof(email));
    }

/// <summary>
/// Returns role-specific info through the implementation of a class.
/// </summary>

    public abstract string GetRoleDescription();

/// <summary>
/// Rejects blank text and trims whitespace before the information is stored in the database.
/// </summary>
    protected static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "This value can't be empty.", parameterName
            );
        }

        return value.Trim();
    }
}