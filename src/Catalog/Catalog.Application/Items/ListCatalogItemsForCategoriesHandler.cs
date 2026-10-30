using Catalog.Application.Contracts;
using Catalog.Domain;
using Catalog.Domain.Categories;
using Catalog.Domain.Items;
using MediatR;
using Result = CSharpFunctionalExtensions.Result<System.Collections.Generic.IReadOnlyList<Catalog.Application.Contracts.CatalogItemDto>, System.Exception>;

namespace Catalog.Application.Items;

/// <summary>
/// Reads the non-removed items of several categories at once; the trip list is generated from this snapshot.
/// Categories the actor's family may not use - another family's, removed, or switched off - contribute nothing.
/// </summary>
public sealed class ListCatalogItemsForCategoriesHandler(ICategoryRepository categories, ICatalogItemRepository items)
    : IRequestHandler<ListCatalogItemsForCategoriesHandler.Query, Result>
{
    public sealed record Query(CatalogActor Actor, IReadOnlyCollection<Guid> CategoryIds) : IRequest<Result>;

    public async Task<Result> Handle(Query request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.CategoryIds.Count is 0)
            return new List<CatalogItemDto>();

        IReadOnlyList<Category> requested = await categories.FindManyAsync(request.CategoryIds, cancellationToken);
        IReadOnlySet<Guid> switchedOff = request.Actor.FamilyId is { } familyId
            ? await categories.ListSwitchedOffAsync(familyId, cancellationToken)
            : new HashSet<Guid>();

        List<Guid> usable =
        [
            .. requested
                .Where(category => !category.IsDeleted
                    && category.AccessFor(request.Actor) is not CatalogAccess.None
                    && !switchedOff.Contains(category.Id))
                .Select(category => category.Id),
        ];

        if (usable.Count is 0)
            return new List<CatalogItemDto>();

        IReadOnlyList<CatalogItem> found = await items.ListByCategoriesAsync(usable, cancellationToken);

        return found.Select(CatalogItemDto.From).ToList();
    }
}
