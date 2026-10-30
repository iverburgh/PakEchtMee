using Catalog.Domain.Categories;
using Shared.Validation;

namespace Catalog.Domain.Items;

/// <summary>A catalog item. It always belongs to exactly one non-removed category; removal is a soft delete.</summary>
public sealed class CatalogItem
{
    private CatalogItem(Guid id, Guid categoryId, ItemName name, Amount? amount)
    {
        Id = id;
        CategoryId = categoryId;
        Name = name;
        Amount = amount;
    }

    private CatalogItem() => Name = null!;

    public Guid Id { get; private set; }

    public Guid CategoryId { get; private set; }

    public ItemName Name { get; private set; }

    public Amount? Amount { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public bool IsDeleted => DeletedAt is not null;

    public static CatalogItem Create(Category category, ItemName name, Amount? amount)
        => Create(Guid.CreateVersion7(), category, name, amount);

    /// <summary>Creates an item with a caller-chosen id, so seed data keeps the same id across runs.</summary>
    public static CatalogItem Create(Guid id, Category category, ItemName name, Amount? amount)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty);
        ArgumentNullException.ThrowIfNull(category);
        ArgumentNullException.ThrowIfNull(name);
        category.EnsureNotDeleted();

        return new CatalogItem(id, category.Id, name, amount);
    }

    public void Rename(ItemName name)
    {
        ArgumentNullException.ThrowIfNull(name);
        EnsureNotDeleted();

        Name = name;
    }

    public void ChangeAmount(Amount? amount)
    {
        EnsureNotDeleted();

        Amount = amount;
    }

    /// <summary>
    /// Moves the item from <paramref name="current"/> to <paramref name="target"/>. Both must share a scope, so a general
    /// item never ends up in one family's catalog and a family's item never becomes visible to every family.
    /// </summary>
    public void MoveTo(Category current, Category target)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(target);

        if (current.Id != CategoryId)
            throw new ArgumentException($"Item '{Name}' does not belong to category '{current.Name}'.", nameof(current));

        EnsureNotDeleted();
        target.EnsureNotDeleted();

        if (current.FamilyId != target.FamilyId)
            throw new DomainValidationException("Een item kan niet tussen algemene en eigen categorieën worden verplaatst.");

        CategoryId = target.Id;
    }

    public void Delete(DateTimeOffset deletedAt)
    {
        if (IsDeleted)
            return;

        DeletedAt = deletedAt;
    }

    public void Restore() => DeletedAt = null;

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
            throw new DomainValidationException($"Item '{Name}' is verwijderd.");
    }
}
