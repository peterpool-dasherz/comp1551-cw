namespace FestivalOperations.Models;

/// <summary>
/// Shared contact details and identity for every person involved in the festival.
/// </summary>

public abstract class Person
{

    /// <summary>
    /// Gets the database identifier; zero represents a new entity that has not been saved yet.
    /// </summary>
    /// <remarks>Every derived role type shares this identifier and the common contact fields.</remarks>
    public int PersonID { get; private set;}

    /// <summary>Gets the person's validated display name.</summary>
    public string Name { get; private set;}

    /// <summary>Gets the person's validated telephone number.</summary>
    public string Telephone { get; private set;}

    /// <summary>Gets the person's validated email address.</summary>
    public string Email { get; private set;}

    /// <summary>Initializes the shared identity and contact fields for a derived role.</summary>
    /// <param name="personID">The existing database ID, or zero for a new person.</param>
    /// <param name="name">The person's name.</param>
    /// <param name="telephone">The person's telephone number.</param>
    /// <param name="email">The person's email address.</param>
    protected Person(
        int personID,
        string name,
        string telephone,
        string email)
    {
        // Derived constructors route shared values through this single validation path.
        // Negative identifiers cannot refer to valid database records.
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
/// Returns a short description supplied by the concrete role class.
/// </summary>
    /// <returns>Human-readable details specific to the person's role.</returns>
    public abstract string GetRoleDescription();

/// <summary>
/// Rejects blank text and removes surrounding whitespace before the value is stored.
/// </summary>
    /// <param name="value">The text value to validate and normalize.</param>
    /// <param name="parameterName">The parameter name included in any validation exception.</param>
    /// <returns>The non-empty value with surrounding whitespace removed.</returns>
    protected static string RequireText(string value, string parameterName)
    {
        // Centralizing text checks keeps contact-field normalization consistent across roles.
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "This value can't be empty.", parameterName
            );
        }

        return value.Trim();
    }
}
