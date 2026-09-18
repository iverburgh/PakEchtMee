using Shared.Validation;

namespace Trips.Domain;

/// <summary>One line on a trip's packing list. Status changes always go through the owning <see cref="Trip"/>.</summary>
public sealed class TripItem
{
    private TripItem(Guid id, Guid tripId, Guid sourceCategoryId, Guid sourceItemId, string categoryName, string name, Amount? amount)
    {
        Id = id;
        TripId = tripId;
        SourceCategoryId = sourceCategoryId;
        SourceItemId = sourceItemId;
        CategoryName = categoryName;
        Name = name;
        Amount = amount;
        Status = PackingStatus.Active;
    }

    private TripItem()
    {
        CategoryName = null!;
        Name = null!;
    }

    public Guid Id { get; private set; }

    public Guid TripId { get; private set; }

    /// <summary>Correlates back to the catalog category so re-adding a category does not duplicate items.</summary>
    public Guid SourceCategoryId { get; private set; }

    /// <summary>Correlates back to the catalog item; it is not a foreign key.</summary>
    public Guid SourceItemId { get; private set; }

    public string CategoryName { get; private set; }

    public string Name { get; private set; }

    public Amount? Amount { get; private set; }

    public PackingStatus Status { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public bool IsDeleted => DeletedAt is not null;

    public PackingStatusTransition Transition => new(Status);

    internal static TripItem Create(Guid tripId, Guid sourceCategoryId, string categoryName, CatalogItemSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return new TripItem(
            Guid.CreateVersion7(),
            tripId,
            sourceCategoryId,
            snapshot.SourceItemId,
            Guard.RequiredText(categoryName, 60, "Categorienaam"),
            Guard.RequiredText(snapshot.Name, 100, "Itemnaam"),
            Amount.Create(snapshot.Quantity, snapshot.Unit));
    }

    internal void ChangeStatus(PackingStatus target)
    {
        EnsureNotDeleted();

        if (target == Status)
            return;

        if (!Transition.IsSingleStepTo(target))
            throw new DomainValidationException($"'{Name}' kan niet in één keer naar die status; er kan maar één stap tegelijk gezet worden.");

        Status = target;
    }

    internal void Delete(DateTimeOffset deletedAt)
    {
        if (IsDeleted)
            return;

        DeletedAt = deletedAt;
    }

    internal void Restore() => DeletedAt = null;

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
            throw new DomainValidationException($"'{Name}' is verwijderd; zet het eerst terug voordat je de status wijzigt.");
    }
}
