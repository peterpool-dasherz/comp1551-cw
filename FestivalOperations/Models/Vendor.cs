namespace FestivalOperations.Models;
/// <summary>
/// Represents a vendor and enforces the required product-category rules.
/// </summary>

public sealed class Vendor : Person
{
    /// <summary>Gets the vendor's three distinct product categories.</summary>
    /// <remarks>The list is read-only so callers cannot alter it after validation.</remarks>
    public IReadOnlyList<string> ProductCategories { get; private set; }

    /// <summary>Creates a vendor after validating shared contact data and product categories.</summary>
    /// <param name="personID">The existing database ID, or zero for a new vendor.</param>
    /// <param name="name">The vendor's name.</param>
    /// <param name="telephone">The vendor's telephone number.</param>
    /// <param name="email">The vendor's email address.</param>
    /// <param name="productCategories">The three distinct product categories.</param>
    public Vendor(
        int personID,
        string name,
        string telephone,
        string email,
        IEnumerable<string> productCategories
    ) : base(personID, name, telephone, email)
    {
        // Store categories only after the complete collection has passed validation.
        ProductCategories = ValidateProductCategories(productCategories);
    }

    /// <summary>Replaces the product categories after validating the new list.</summary>
    /// <param name="productCategories">The vendor's new categories.</param>
    public void UpdateProductCategories(
        IEnumerable<string> productCategories
    )
    {
        // Category updates use the same count and uniqueness rules as vendor creation.
        ProductCategories = ValidateProductCategories(productCategories);
    }

    /// <summary>Builds the role description used when displaying this vendor.</summary>
    /// <returns>The vendor label followed by its product categories.</returns>
    public override string GetRoleDescription()
    {
        return $"Vendor - categories: {string.Join(", ", ProductCategories)}";

    }

    /// <summary>Trims category names, removes case-insensitive duplicates, and enforces a count of three.</summary>
    /// <param name="productCategories">The category names supplied for the vendor.</param>
    /// <returns>A read-only list of exactly three distinct categories.</returns>
    private static IReadOnlyList<string> ValidateProductCategories(
        IEnumerable<string> productCategories
    )
    {
        ArgumentNullException.ThrowIfNull(productCategories);

        // Build a normalized unique list before enforcing the required category count.
        List<string> cleanedCategories = productCategories
            // Normalize each category before comparing names and checking the required count.
            .Select(category => RequireText(category, nameof(productCategories)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        
        if (cleanedCategories.Count != 3)
        {
            // Each vendor must be associated with exactly three distinct product categories.
            throw new ArgumentException(
                "A vendor must have exactly three different product categories.",
                nameof(productCategories)
            );

        }

        // Expose an immutable view so callers cannot invalidate the checked count later.
        return cleanedCategories.AsReadOnly();
    }
}
