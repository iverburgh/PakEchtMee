using Shared.Validation;

namespace Trips.Domain;

/// <summary>
/// The aggregate root for a trip. Every change to a trip item goes through here, so the uniqueness of
/// a catalog item within a trip and the legality of a status transition are decided in one place.
/// </summary>
public sealed class Trip
{
    private readonly List<TripCategory> categories = [];
    private readonly List<TripItem> items = [];

    private Trip(Guid id, Guid familyId, TripName name, TripDates dates, TripComposition composition)
    {
        Id = id;
        FamilyId = familyId;
        Name = name;
        Dates = dates;
        Composition = composition;
    }

    private Trip()
    {
        Name = null!;
        Dates = null!;
        Composition = null!;
    }

    public Guid Id { get; private set; }

    /// <summary>The family the trip belongs to; only its members may see or change it.</summary>
    public Guid FamilyId { get; private set; }

    public TripName Name { get; private set; }

    public TripDates Dates { get; private set; }

    /// <summary>Who comes along: a copy of the family's composition when the trip is created, changeable per trip.</summary>
    public TripComposition Composition { get; private set; }

    public IReadOnlyList<TripCategory> Categories => categories;

    public IReadOnlyList<TripItem> Items => items;

    /// <summary>Creates a trip for the family's composition, or <see cref="TripComposition.Default"/> when it is not given.</summary>
    public static Trip Create(Guid familyId, TripName name, TripDates dates, TripComposition? composition = null)
        => Create(Guid.CreateVersion7(), familyId, name, dates, composition);

    /// <summary>Creates a trip with a caller-chosen id, so seed data keeps the same id across runs.</summary>
    public static Trip Create(Guid id, Guid familyId, TripName name, TripDates dates, TripComposition? composition = null)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(familyId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(dates);

        return new Trip(id, familyId, name, dates, composition ?? TripComposition.Default);
    }

    /// <summary>Adds the given categories and their items; items that already exist are restored rather than duplicated.</summary>
    public void SelectCategories(IReadOnlyCollection<CategorySelection> selections)
    {
        ArgumentNullException.ThrowIfNull(selections);

        foreach (CategorySelection selection in selections)
        {
            if (!categories.Exists(category => category.CategoryId == selection.CategoryId))
                categories.Add(TripCategory.Create(Id, selection.CategoryId, selection.Name));

            foreach (CatalogItemSnapshot snapshot in selection.Items)
            {
                TripItem? existing = items.Find(item => item.SourceItemId == snapshot.SourceItemId);

                if (existing is null)
                    items.Add(TripItem.Create(Id, selection.CategoryId, selection.Name, snapshot, Composition));
                else if (existing.IsDeleted)
                    existing.Restore();
            }
        }
    }

    /// <summary>Removes a category from the trip and deletes its items. Deleting is reversible, so nothing is lost.</summary>
    public void RemoveCategory(Guid categoryId, DateTimeOffset deletedAt)
    {
        categories.RemoveAll(category => category.CategoryId == categoryId);

        foreach (TripItem item in items.Where(item => item.SourceCategoryId == categoryId))
            item.Delete(deletedAt);
    }

    /// <summary>True when removing this category would discard progress, which is what the user is asked to confirm.</summary>
    public bool HasProgressedItemsIn(Guid categoryId)
        => items.Exists(item => item.SourceCategoryId == categoryId && !item.IsDeleted && item.Status is not PackingStatus.Active);

    public TripItem AdvanceItem(Guid itemId)
    {
        TripItem item = RequireItem(itemId);
        PackingStatus? next = item.Transition.Next
            ?? throw new DomainValidationException($"'{item.Name}' is al ingeladen.");

        item.ChangeStatus(next.Value);

        return item;
    }

    public TripItem RevertItem(Guid itemId)
    {
        TripItem item = RequireItem(itemId);
        PackingStatus? previous = item.Transition.Previous
            ?? throw new DomainValidationException($"'{item.Name}' staat al op de eerste status.");

        item.ChangeStatus(previous.Value);

        return item;
    }

    /// <summary>Changes an item to an explicit status; anything other than a single step is rejected.</summary>
    public TripItem ChangeItemStatus(Guid itemId, PackingStatus target)
    {
        TripItem item = RequireItem(itemId);
        item.ChangeStatus(target);

        return item;
    }

    /// <summary>Changes who comes along and recalculates the per-head totals of the items that are still to do.</summary>
    /// <returns>How many totals changed, so the user can be told what the change did.</returns>
    public int ChangeComposition(TripComposition composition)
    {
        ArgumentNullException.ThrowIfNull(composition);

        Composition = composition;

        return items.Count(item => item.FollowComposition(composition));
    }

    /// <summary>Changes one item's quantity rule (or removes it with <c>null</c>); the catalog item is not touched.</summary>
    public TripItem ChangeItemAmount(Guid itemId, Amount? amount)
    {
        TripItem item = RequireItem(itemId);
        item.ChangeAmount(amount, Composition);

        return item;
    }

    public TripItem DeleteItem(Guid itemId, DateTimeOffset deletedAt)
    {
        TripItem item = RequireItem(itemId);
        item.Delete(deletedAt);

        return item;
    }

    public TripItem RestoreItem(Guid itemId)
    {
        TripItem item = RequireItem(itemId);
        item.Restore();

        return item;
    }

    public TripProgress Progress()
    {
        List<TripItem> live = [.. items.Where(item => !item.IsDeleted)];

        return new TripProgress(
            live.Count,
            live.Count(item => item.Status is PackingStatus.Active),
            live.Count(item => item.Status is PackingStatus.Prepared),
            live.Count(item => item.Status is PackingStatus.Packed),
            live.Count(item => item.Status is PackingStatus.Loaded));
    }

    private TripItem RequireItem(Guid itemId)
        => items.Find(item => item.Id == itemId) ?? throw NotFoundException.For("Item op de paklijst", itemId);
}
