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
    {
        ArgumentNullException.ThrowIfNull(category);
        ArgumentNullException.ThrowIfNull(name);
        category.EnsureNotDeleted();

        return new CatalogItem(Guid.CreateVersion7(), category.Id, name, amount);
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

    public void MoveTo(Category category)
    {
        ArgumentNullException.ThrowIfNull(category);
        EnsureNotDeleted();
        category.EnsureNotDeleted();

        CategoryId = category.Id;
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
