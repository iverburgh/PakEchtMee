using Catalog.Application.Contracts;
using Catalog.Domain.Items;
using MediatR;
using Result = CSharpFunctionalExtensions.Result<System.Collections.Generic.IReadOnlyList<Catalog.Application.Contracts.CatalogItemDto>, System.Exception>;

namespace Catalog.Application.Items;

/// <summary>Reads the non-removed items of several categories at once; the trip list is generated from this snapshot.</summary>
public sealed class ListCatalogItemsForCategoriesHandler(ICatalogItemRepository items)
    : IRequestHandler<ListCatalogItemsForCategoriesHandler.Query, Result>
{
    public sealed record Query(IReadOnlyCollection<Guid> CategoryIds) : IRequest<Result>;

    public async Task<Result> Handle(Query request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.CategoryIds.Count is 0)
            return new List<CatalogItemDto>();

        IReadOnlyList<CatalogItem> found = await items.ListByCategoriesAsync(request.CategoryIds, cancellationToken);

        return found.Select(CatalogItemDto.From).ToList();
    }
}
