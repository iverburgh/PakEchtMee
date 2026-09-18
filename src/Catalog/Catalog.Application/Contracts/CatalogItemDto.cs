using Catalog.Domain.Items;

namespace Catalog.Application.Contracts;

public sealed record CatalogItemDto(Guid Id, Guid CategoryId, string Name, int? Quantity, string? Unit, bool IsDeleted)
{
    public static CatalogItemDto From(CatalogItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return new CatalogItemDto(item.Id, item.CategoryId, item.Name.Value, item.Amount?.Quantity, item.Amount?.Unit, item.IsDeleted);
    }
}
