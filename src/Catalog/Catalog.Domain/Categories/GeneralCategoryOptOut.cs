using Shared.Validation;

namespace Catalog.Domain.Categories;

/// <summary>
/// Records that a family switched a general category off. Storing the exception rather than the choice means a new
/// general category is on for every family without touching any of them.
/// </summary>
public sealed class GeneralCategoryOptOut
{
    private GeneralCategoryOptOut(Guid familyId, Guid categoryId)
    {
        FamilyId = familyId;
        CategoryId = categoryId;
    }

    public Guid FamilyId { get; private set; }

    public Guid CategoryId { get; private set; }

    public static GeneralCategoryOptOut Create(Guid familyId, Category category)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(familyId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(category);

        if (!category.IsGeneral)
            throw new DomainValidationException($"Alleen algemene categorieën kun je aan- of uitzetten; '{category.Name}' is een eigen categorie.");

        return new GeneralCategoryOptOut(familyId, category.Id);
    }
}
