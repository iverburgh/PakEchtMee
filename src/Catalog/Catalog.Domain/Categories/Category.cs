using Shared.Validation;

namespace Catalog.Domain.Categories;

/// <summary>A catalog category. Removal is a soft delete so past trips and restores keep working.</summary>
public sealed class Category
{
    private Category(Guid id, CategoryName name)
    {
        Id = id;
        Name = name;
    }

    private Category() => Name = null!;

    public Guid Id { get; private set; }

    public CategoryName Name { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public bool IsDeleted => DeletedAt is not null;

    public static Category Create(CategoryName name) => Create(Guid.CreateVersion7(), name);

    /// <summary>Creates a category with a caller-chosen id, so seed data keeps the same id across runs.</summary>
    public static Category Create(Guid id, CategoryName name)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty);
        ArgumentNullException.ThrowIfNull(name);

        return new Category(id, name);
    }

    public void Rename(CategoryName name)
    {
        ArgumentNullException.ThrowIfNull(name);
        EnsureNotDeleted();

        Name = name;
    }

    public void Delete(DateTimeOffset deletedAt)
    {
        if (IsDeleted)
            return;

        DeletedAt = deletedAt;
    }

    public void Restore() => DeletedAt = null;

    internal void EnsureNotDeleted()
    {
        if (IsDeleted)
            throw new DomainValidationException($"Categorie '{Name}' is verwijderd.");
    }
}
