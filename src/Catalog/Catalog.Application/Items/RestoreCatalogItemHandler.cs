using Catalog.Application.Contracts;
using Catalog.Domain;
using Catalog.Domain.Categories;
using Catalog.Domain.Items;
using CSharpFunctionalExtensions;
using MediatR;
using Shared.Validation;
using Result = CSharpFunctionalExtensions.Result<Catalog.Application.Contracts.CatalogItemDto, System.Exception>;

namespace Catalog.Application.Items;

public sealed class RestoreCatalogItemHandler(ICategoryRepository categories, ICatalogItemRepository items)
    : IRequestHandler<RestoreCatalogItemHandler.Command, Result>
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

        if (await items.ExistsWithNameInCategoryAsync(item.CategoryId, item.Name, request.Id, cancellationToken))
            return new DomainValidationException($"Er bestaat al een item met de naam '{item.Name}' in deze categorie, dus dit item kan niet onder die naam worden teruggezet.");

        item.Restore();
        await items.SaveChangesAsync(cancellationToken);

        return CatalogItemDto.From(item);
    }
}
