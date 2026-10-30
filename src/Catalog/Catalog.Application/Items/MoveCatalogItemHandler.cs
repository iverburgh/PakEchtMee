using Catalog.Application.Contracts;
using Catalog.Domain;
using Catalog.Domain.Categories;
using Catalog.Domain.Items;
using CSharpFunctionalExtensions;
using MediatR;
using Shared.Validation;
using Result = CSharpFunctionalExtensions.Result<Catalog.Application.Contracts.CatalogItemDto, System.Exception>;

namespace Catalog.Application.Items;

public sealed class MoveCatalogItemHandler(ICategoryRepository categories, ICatalogItemRepository items)
    : IRequestHandler<MoveCatalogItemHandler.Command, Result>
{
    public sealed record Command(CatalogActor Actor, Guid Id, Guid TargetCategoryId) : IRequest<Result>;

    public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Result<(CatalogItem Item, Category Category), Exception> loaded =
            await ItemAccess.LoadForEditAsync(items, categories, request.Id, request.Actor, cancellationToken);

        if (loaded.IsFailure)
            return loaded.Error;

        (CatalogItem item, Category current) = loaded.Value;
        Category? target = await categories.FindAsync(request.TargetCategoryId, cancellationToken);

        if (CatalogAccessRules.Check(target, request.TargetCategoryId, request.Actor, CatalogAccess.Edit) is { } denied)
            return denied;

        if (await items.ExistsWithNameInCategoryAsync(target!.Id, item.Name, request.Id, cancellationToken))
            return new DomainValidationException($"Er bestaat al een item met de naam '{item.Name}' in categorie '{target.Name}'.");

        item.MoveTo(current, target);
        await items.SaveChangesAsync(cancellationToken);

        return CatalogItemDto.From(item);
    }
}
