using Catalog.Application.Contracts;
using Catalog.Domain.Items;
using MediatR;
using Shared.Validation;
using Result = CSharpFunctionalExtensions.Result<Catalog.Application.Contracts.CatalogItemDto, System.Exception>;

namespace Catalog.Application.Items;

public sealed class RemoveCatalogItemHandler(ICatalogItemRepository items, TimeProvider timeProvider)
    : IRequestHandler<RemoveCatalogItemHandler.Command, Result>
{
    public sealed record Command(Guid Id) : IRequest<Result>;

    public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        CatalogItem? item = await items.FindAsync(request.Id, cancellationToken);

        if (item is null)
            return NotFoundException.For("Item", request.Id);

        item.Delete(timeProvider.GetUtcNow());
        await items.SaveChangesAsync(cancellationToken);

        return CatalogItemDto.From(item);
    }
}
