namespace FestivalOperations.Models;

/// <summary>
/// Performers' class
/// </summary>


public sealed class Performer : Person
{
    public decimal Fee { get; private set;}
    public IReadOnlyList<string> Genres { get; private set; }

    public Performer(
        int personID,
        string name,
        string telephone,
        string email,
        decimal fee,
        IEnumerable<string> genres) : base(personID, name, telephone, email)
    {
        Fee = ValidateFee(fee);
        Genres = ValidateGenres(genres);
    }

    public void UpdatePerformerDetails(
        decimal fee,
        IEnumerable<string> genres)
    {
        Fee = ValidateFee(fee);
        Genres = ValidateGenres(genres);
    }

    public override string GetRoleDescription()
    {
        return $"Performer - genres: {string.Join(",", Genres)}";
    }

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

    private static IReadOnlyList<string> ValidateGenres(IEnumerable<string> genres)
    {
        ArgumentNullException.ThrowIfNull(genres);
        List<string> cleanedGenres = genres
            .Select(genre => RequireText(genre, nameof(genres)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        
        if (cleanedGenres.Count == 0)
        {
            throw new ArgumentException(
                "A performer must have at least one genre.",
                nameof(genres)
            );

        }
        
        return cleanedGenres.AsReadOnly();
    }
}