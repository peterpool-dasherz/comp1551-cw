namespace FestivalOperations.Models;
/// <summary>
/// Vendors' details at the festival
/// </summary>

public sealed class Vendor : Person
{
    public IReadOnlyList<string> ProductCategories { get; private set; }

    public Vendor(
        int personID,
        string name,
        string telephone,
        string email,
        IEnumerable<string> productCategories
    ) : base(personID, name, telephone, email)
    {
        ProductCategories = ValidateProductCategories(productCategories);
    }

    public void UpdateProductCategories(
        IEnumerable<string> productCategories
    )
    {
        ProductCategories = ValidateProductCategories(productCategories);
    }

    public override string GetRoleDescription()
    {
        return $"Vendor - categories: {string.Join(", ", ProductCategories)}";

    }

// Remove duplicates for input.
    private static IReadOnlyList<string> ValidateProductCategories(
        IEnumerable<string> productCategories
    )
    {
        ArgumentNullException.ThrowIfNull(productCategories);

        List<string> cleanedCategories = productCategories
            .Select(category => RequireText(category, nameof(productCategories)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        
        if (cleanedCategories.Count != 3)
        {
            throw new ArgumentException(
                "A vendor must have exactly three different product categories.",
                nameof(productCategories)
            );

        }

        return cleanedCategories.AsReadOnly();
    }
}