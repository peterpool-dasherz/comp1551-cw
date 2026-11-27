namespace FestivalOperations.Models;

/// <summary>
/// Represents a performer and enforces performer-specific fee and genre rules.
/// </summary>


public sealed class Performer : Person
{
    /// <summary>Gets the performer's non-negative fee.</summary>
    /// <remarks>The private setter ensures fee changes pass through model validation.</remarks>
    public decimal Fee { get; private set;}

    /// <summary>Gets the performer's two distinct genres.</summary>
    /// <remarks>The read-only interface prevents callers changing the validated list directly.</remarks>
    public IReadOnlyList<string> Genres { get; private set; }

    /// <summary>Creates a performer after validating the shared and performer-specific data.</summary>
    /// <param name="personID">The existing database ID, or zero for a new performer.</param>
    /// <param name="name">The performer's name.</param>
    /// <param name="telephone">The performer's telephone number.</param>
    /// <param name="email">The performer's email address.</param>
    /// <param name="fee">The non-negative performance fee.</param>
    /// <param name="genres">The two distinct genres associated with the performer.</param>
    public Performer(
        int personID,
        string name,
        string telephone,
        string email,
        decimal fee,
        IEnumerable<string> genres) : base(personID, name, telephone, email)
    {
        // Validate role-specific values during construction so invalid performers cannot be created.
        Fee = ValidateFee(fee);
        Genres = ValidateGenres(genres);
    }

    /// <summary>Replaces the fee and genre list after applying the same rules as construction.</summary>
    /// <param name="fee">The performer's new fee.</param>
    /// <param name="genres">The performer's new genre list.</param>
    public void UpdatePerformerDetails(
        decimal fee,
        IEnumerable<string> genres)
    {
        // Re-run the same checks used by construction whenever mutable details are replaced.
        Fee = ValidateFee(fee);
        Genres = ValidateGenres(genres);
    }

    /// <summary>Builds the role description used when displaying this performer.</summary>
    /// <returns>The performer label followed by the genre names.</returns>
    public override string GetRoleDescription()
    {
        return $"Performer - genres: {string.Join(",", Genres)}";
    }

    /// <summary>Ensures the performance fee is not negative.</summary>
    /// <param name="fee">The fee to validate.</param>
    /// <returns>The validated fee.</returns>
    private static decimal ValidateFee(decimal fee)
    {
        if (fee < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fee),
                "The performer fee cannot be negative."
            );
        }
        
        return fee;
    }

    /// <summary>Normalizes genre names, removes case-insensitive duplicates, and enforces the required count.</summary>
    /// <param name="genres">The genre names supplied for the performer.</param>
    /// <returns>A read-only list containing exactly two distinct, trimmed genres.</returns>
    private static IReadOnlyList<string> ValidateGenres(IEnumerable<string> genres)
    {
        ArgumentNullException.ThrowIfNull(genres);
        // Materialize normalized unique values once before checking the required count.
        List<string> cleanedGenres = genres
            // RequireText also rejects blank entries before the count is checked.
            .Select(genre => RequireText(genre, nameof(genres)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        
        if (cleanedGenres.Count != 2)
        {
            // The coursework rule requires exactly two different genres per performer.
            throw new ArgumentException(
                "A performer must have exactly two different genres.",
                nameof(genres)
            );

        }
        
        // Prevent later changes to the backing list from bypassing validation.
        return cleanedGenres.AsReadOnly();
    }
}
