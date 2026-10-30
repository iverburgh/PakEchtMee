using Catalog.Domain;
using Catalog.Domain.Categories;
using Catalog.Domain.Items;
using CSharpFunctionalExtensions;
using Shared.Validation;

namespace Catalog.Application.Items;

/// <summary>Loads an item together with its category and applies the category's access rule to the item.</summary>
internal static class ItemAccess
{
    public static async Task<Result<(CatalogItem Item, Category Category), Exception>> LoadForEditAsync(
        ICatalogItemRepository items,
        ICategoryRepository categories,
        Guid itemId,
        CatalogActor actor,
        CancellationToken cancellationToken)
    {
        CatalogItem? item = await items.FindAsync(itemId, cancellationToken);

        if (item is null)
            return NotFoundException.For("Item", itemId);

        Category? category = await categories.FindAsync(item.CategoryId, cancellationToken);

        // An item of another family is reported as a missing item, not as a missing category.
        return CatalogAccessRules.Check(category, item.CategoryId, actor, CatalogAccess.Edit) switch
        {
            NotFoundException => NotFoundException.For("Item", itemId),
            { } denied => denied,
            null => (item, category!),
        };
    }
}
