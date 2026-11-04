using Catalog.Application.Contracts;
using Catalog.Domain;
using Catalog.Domain.Categories;
using Catalog.Domain.Items;
using MediatR;
using Shared.Validation;
using Result = CSharpFunctionalExtensions.Result<Catalog.Application.Contracts.CatalogItemDto, System.Exception>;

namespace Catalog.Application.Items;

public sealed class CreateCatalogItemHandler(ICategoryRepository categories, ICatalogItemRepository items)
    : IRequestHandler<CreateCatalogItemHandler.Command, Result>
{
    public sealed record Command(CatalogActor Actor, Guid CategoryId, string? Name, int? Quantity, string? Unit, string? Basis = null)
        : IRequest<Result>;

    public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Category? category = await categories.FindAsync(request.CategoryId, cancellationToken);

        if (CatalogAccessRules.Check(category, request.CategoryId, request.Actor, CatalogAccess.Edit) is { } denied)
            return denied;

        ItemName name = ItemName.Create(request.Name);
        Amount? amount = Amount.Create(request.Quantity, request.Unit, QuantityBases.Parse(request.Basis));

        if (await items.ExistsWithNameInCategoryAsync(category!.Id, name, null, cancellationToken))
            return new DomainValidationException($"Er bestaat al een item met de naam '{name}' in categorie '{category.Name}'.");

        CatalogItem item = CatalogItem.Create(category, name, amount);
        items.Add(item);
        await items.SaveChangesAsync(cancellationToken);

        return CatalogItemDto.From(item);
    }
}
