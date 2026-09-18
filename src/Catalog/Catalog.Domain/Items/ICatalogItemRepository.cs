namespace Catalog.Domain.Items;

public interface ICatalogItemRepository
{
    Task<IReadOnlyList<CatalogItem>> ListByCategoryAsync(Guid categoryId, bool includeDeleted, CancellationToken cancellationToken);

    /// <summary>Non-removed items of the given categories, used to generate a trip packing list.</summary>
    Task<IReadOnlyList<CatalogItem>> ListByCategoriesAsync(IReadOnlyCollection<Guid> categoryIds, CancellationToken cancellationToken);

    Task<CatalogItem?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Case-insensitive name check among non-removed items of one category, optionally ignoring one item.</summary>
    Task<bool> ExistsWithNameInCategoryAsync(Guid categoryId, ItemName name, Guid? excludingId, CancellationToken cancellationToken);

    void Add(CatalogItem item);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
