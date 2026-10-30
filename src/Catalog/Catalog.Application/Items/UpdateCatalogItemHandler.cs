using Catalog.Application.Contracts;
using Catalog.Domain;
using Catalog.Domain.Categories;
using Catalog.Domain.Items;
using CSharpFunctionalExtensions;
using MediatR;
using Shared.Validation;
using Result = CSharpFunctionalExtensions.Result<Catalog.Application.Contracts.CatalogItemDto, System.Exception>;

namespace Catalog.Application.Items;

/// <summary>Renames an item and replaces its optional amount in one step, which is how the item form edits it.</summary>
public sealed class UpdateCatalogItemHandler(ICategoryRepository categories, ICatalogItemRepository items)
    : IRequestHandler<UpdateCatalogItemHandler.Command, Result>
{
    public sealed record Command(CatalogActor Actor, Guid Id, string? Name, int? Quantity, string? Unit) : IRequest<Result>;

    public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Result<(CatalogItem Item, Category Category), Exception> loaded =
            await ItemAccess.LoadForEditAsync(items, categories, request.Id, request.Actor, cancellationToken);

        if (loaded.IsFailure)
            return loaded.Error;

        CatalogItem item = loaded.Value.Item;
        ItemName name = ItemName.Create(request.Name);
        Amount? amount = Amount.Create(request.Quantity, request.Unit);

        if (await items.ExistsWithNameInCategoryAsync(item.CategoryId, name, request.Id, cancellationToken))
            return new DomainValidationException($"Er bestaat al een item met de naam '{name}' in deze categorie.");

        item.Rename(name);
        item.ChangeAmount(amount);
        await items.SaveChangesAsync(cancellationToken);

        return CatalogItemDto.From(item);
    }
}
