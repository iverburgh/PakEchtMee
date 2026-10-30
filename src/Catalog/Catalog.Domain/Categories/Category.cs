using Shared.Validation;

namespace Catalog.Domain.Categories;

/// <summary>
/// A catalog category: either a family's own, or general (no family) and maintained by an administrator.
/// Removal is a soft delete so past trips and restores keep working.
/// </summary>
public sealed class Category
{
    private Category(Guid id, CategoryName name, Guid? familyId)
    {
        Id = id;
        Name = name;
        FamilyId = familyId;
    }

    private Category() => Name = null!;

    public Guid Id { get; private set; }

    public CategoryName Name { get; private set; }

    /// <summary>The owning family; <c>null</c> for a general category.</summary>
    public Guid? FamilyId { get; private set; }

    public bool IsGeneral => FamilyId is null;

    public DateTimeOffset? DeletedAt { get; private set; }

    public bool IsDeleted => DeletedAt is not null;

    /// <summary>Creates a general category.</summary>
    public static Category Create(CategoryName name) => Create(Guid.CreateVersion7(), name);

    /// <summary>Creates a general category with a caller-chosen id, so seed data keeps the same id across runs.</summary>
    public static Category Create(Guid id, CategoryName name)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty);
        ArgumentNullException.ThrowIfNull(name);

        return new Category(id, name, null);
    }

    public static Category CreateForFamily(Guid familyId, CategoryName name)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(familyId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(name);

        return new Category(Guid.CreateVersion7(), name, familyId);
    }

    /// <summary>
    /// The single place that decides catalog access: a family edits its own categories and never sees another
    /// family's; general categories are edited by administrators and read by members.
    /// </summary>
    public CatalogAccess AccessFor(CatalogActor actor)
    {
        ArgumentNullException.ThrowIfNull(actor);

        return this switch
        {
            { IsGeneral: true } when actor.IsAdministrator => CatalogAccess.Edit,
            { IsGeneral: true } when actor.FamilyId is not null => CatalogAccess.Read,
            { IsGeneral: false } when FamilyId == actor.FamilyId => CatalogAccess.Edit,
            _ => CatalogAccess.None,
        };
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
