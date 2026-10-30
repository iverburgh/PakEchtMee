using Catalog.Application.Categories;
using Catalog.Application.Contracts;
using Catalog.Application.Items;
using CSharpFunctionalExtensions;
using MediatR;
using Trips.Domain;

namespace Web;

/// <summary>
/// Reads categories and their items from the catalog and turns them into the snapshots a trip is generated from.
/// It lives in the composition root because it is the only place that may see both bounded contexts.
/// </summary>
internal static class CategorySelections
{
    /// <summary>Returns a selection for every requested category that exists and is not removed; unknown ids are left out.</summary>
    public static async Task<Result<IReadOnlyList<CategorySelection>, Exception>> ReadAsync(
        ISender sender,
        IReadOnlyList<Guid> categoryIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(categoryIds);

        Result<IReadOnlyList<CategoryDto>, Exception> categories =
            await sender.Send(new ListCategoriesHandler.Query(), cancellationToken);

        if (categories.IsFailure)
            return categories.Error;

        Result<IReadOnlyList<CatalogItemDto>, Exception> items =
            await sender.Send(new ListCatalogItemsForCategoriesHandler.Query(categoryIds), cancellationToken);

        if (items.IsFailure)
            return items.Error;

        List<CategorySelection> selections =
        [
            .. categories.Value
                .Where(category => categoryIds.Contains(category.Id))
                .Select(category => new CategorySelection(
                    category.Id,
                    category.Name,
                    [.. items.Value
                        .Where(item => item.CategoryId == category.Id)
                        .Select(item => new CatalogItemSnapshot(item.Id, item.Name, item.Quantity, item.Unit))]))
        ];

        return selections;
    }
}
