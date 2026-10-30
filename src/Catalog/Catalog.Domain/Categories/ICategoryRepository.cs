namespace Catalog.Domain.Categories;

public interface ICategoryRepository
{
    /// <summary>The family's own categories (when a family is given) together with every general category.</summary>
    Task<IReadOnlyList<Category>> ListVisibleAsync(Guid? familyId, bool includeDeleted, CancellationToken cancellationToken);

    Task<IReadOnlyList<Category>> ListGeneralAsync(bool includeDeleted, CancellationToken cancellationToken);

    Task<IReadOnlyList<Category>> FindManyAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    Task<Category?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Case-insensitive name check among non-removed categories of one scope - a family, or the general catalog when
    /// <paramref name="familyId"/> is <c>null</c> - optionally ignoring one category.
    /// </summary>
    Task<bool> ExistsWithNameAsync(CategoryName name, Guid? familyId, Guid? excludingId, CancellationToken cancellationToken);

    /// <summary>The general categories the family switched off.</summary>
    Task<IReadOnlySet<Guid>> ListSwitchedOffAsync(Guid familyId, CancellationToken cancellationToken);

    /// <summary>Switches a general category on or off for one family; saved by <see cref="SaveChangesAsync"/>.</summary>
    Task SwitchAsync(Guid familyId, Category category, bool on, CancellationToken cancellationToken);

    void Add(Category category);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
