namespace Catalog.Domain.Categories;

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> ListAsync(bool includeDeleted, CancellationToken cancellationToken);

    Task<Category?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Case-insensitive name check among non-removed categories, optionally ignoring one category.</summary>
    Task<bool> ExistsWithNameAsync(CategoryName name, Guid? excludingId, CancellationToken cancellationToken);

    void Add(Category category);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
