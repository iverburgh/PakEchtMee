using Catalog.Application.Contracts;
using Catalog.Domain.Items;
using MediatR;
using Shared.Validation;
using Result = CSharpFunctionalExtensions.Result<Catalog.Application.Contracts.CatalogItemDto, System.Exception>;

namespace Catalog.Application.Items;

public sealed class RestoreCatalogItemHandler(ICatalogItemRepository items)
    : IRequestHandler<RestoreCatalogItemHandler.Command, Result>
{
    public sealed record Command(Guid Id) : IRequest<Result>;

    public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        CatalogItem? item = await items.FindAsync(request.Id, cancellationToken);

        if (item is null)
            return NotFoundException.For("Item", request.Id);

        if (await items.ExistsWithNameInCategoryAsync(item.CategoryId, item.Name, request.Id, cancellationToken))
            return new DomainValidationException($"Er bestaat al een item met de naam '{item.Name}' in deze categorie, dus dit item kan niet onder die naam worden teruggezet.");

        item.Restore();
        await items.SaveChangesAsync(cancellationToken);

        return CatalogItemDto.From(item);
    }
}
