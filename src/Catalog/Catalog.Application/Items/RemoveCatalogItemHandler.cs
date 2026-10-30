using Catalog.Application.Contracts;
using Catalog.Domain;
using Catalog.Domain.Categories;
using Catalog.Domain.Items;
using CSharpFunctionalExtensions;
using MediatR;
using Result = CSharpFunctionalExtensions.Result<Catalog.Application.Contracts.CatalogItemDto, System.Exception>;

namespace Catalog.Application.Items;

public sealed class RemoveCatalogItemHandler(ICategoryRepository categories, ICatalogItemRepository items, TimeProvider timeProvider)
    : IRequestHandler<RemoveCatalogItemHandler.Command, Result>
{
    public sealed record Command(CatalogActor Actor, Guid Id) : IRequest<Result>;

    public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Result<(CatalogItem Item, Category Category), Exception> loaded =
            await ItemAccess.LoadForEditAsync(items, categories, request.Id, request.Actor, cancellationToken);

        if (loaded.IsFailure)
            return loaded.Error;

        CatalogItem item = loaded.Value.Item;
        item.Delete(timeProvider.GetUtcNow());
        await items.SaveChangesAsync(cancellationToken);

        return CatalogItemDto.From(item);
    }
}
