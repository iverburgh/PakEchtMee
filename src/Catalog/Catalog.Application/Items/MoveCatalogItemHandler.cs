using Catalog.Application.Contracts;
using Catalog.Domain.Categories;
using Catalog.Domain.Items;
using MediatR;
using Shared.Validation;
using Result = CSharpFunctionalExtensions.Result<Catalog.Application.Contracts.CatalogItemDto, System.Exception>;

namespace Catalog.Application.Items;

public sealed class MoveCatalogItemHandler(ICategoryRepository categories, ICatalogItemRepository items)
    : IRequestHandler<MoveCatalogItemHandler.Command, Result>
{
    public sealed record Command(Guid Id, Guid TargetCategoryId) : IRequest<Result>;

    public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        CatalogItem? item = await items.FindAsync(request.Id, cancellationToken);

        if (item is null)
            return NotFoundException.For("Item", request.Id);

        Category? target = await categories.FindAsync(request.TargetCategoryId, cancellationToken);

        if (target is null)
            return NotFoundException.For("Categorie", request.TargetCategoryId);

        if (await items.ExistsWithNameInCategoryAsync(target.Id, item.Name, request.Id, cancellationToken))
            return new DomainValidationException($"Er bestaat al een item met de naam '{item.Name}' in categorie '{target.Name}'.");

        item.MoveTo(target);
        await items.SaveChangesAsync(cancellationToken);

        return CatalogItemDto.From(item);
    }
}
