using Catalog.Application.Contracts;
using Catalog.Domain;
using Catalog.Domain.Categories;
using Catalog.Domain.Items;
using MediatR;
using Result = CSharpFunctionalExtensions.Result<System.Collections.Generic.IReadOnlyList<Catalog.Application.Contracts.CatalogItemDto>, System.Exception>;

namespace Catalog.Application.Items;

public sealed class ListCatalogItemsHandler(ICategoryRepository categories, ICatalogItemRepository items)
    : IRequestHandler<ListCatalogItemsHandler.Query, Result>
{
    public sealed record Query(CatalogActor Actor, Guid CategoryId, bool IncludeDeleted = false) : IRequest<Result>;

    public async Task<Result> Handle(Query request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Category? category = await categories.FindAsync(request.CategoryId, cancellationToken);

        if (CatalogAccessRules.Check(category, request.CategoryId, request.Actor, CatalogAccess.Read) is { } denied)
            return denied;

        IReadOnlyList<CatalogItem> found = await items.ListByCategoryAsync(request.CategoryId, request.IncludeDeleted, cancellationToken);

        return found.Select(CatalogItemDto.From).ToList();
    }
}
