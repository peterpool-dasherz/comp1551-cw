namespace FestivalOperations.Models;

/// <summary>
/// Contact details of everybody that is involved in the festival.
/// </summary>

public abstract class Person
{
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

    public abstract string GetRoleDescription();

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