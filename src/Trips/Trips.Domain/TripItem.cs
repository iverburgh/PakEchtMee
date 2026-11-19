using Shared.Validation;

namespace Trips.Domain;

/// <summary>One line on a trip's packing list. Status changes always go through the owning <see cref="Trip"/>.</summary>
public sealed class TripItem
{
    private TripItem(
        Guid id,
        Guid tripId,
        Guid sourceCategoryId,
        Guid sourceItemId,
        string categoryName,
        string name,
        Amount? amount,
        TripComposition composition,
        TripDates dates)
    {
        Id = id;
        TripId = tripId;
        SourceCategoryId = sourceCategoryId;
        SourceItemId = sourceItemId;
        CategoryName = categoryName;
        Name = name;
        Amount = amount;
        Total = amount?.TotalFor(composition, dates);
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

    /// <summary>The quantity rule: copied from the catalog, and changeable on the trip while the item is still to do.</summary>
    public Amount? Amount { get; private set; }

    /// <summary>
    /// How many to pack, calculated from <see cref="Amount"/> and the trip's composition and dates. Stored rather than
    /// derived, so an item that has progressed keeps the total it was packed with when who or when changes later.
    /// </summary>
    public int? Total { get; private set; }

    public PackingStatus Status { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public bool IsDeleted => DeletedAt is not null;

    public PackingStatusTransition Transition => new(Status);

    internal static TripItem Create(
        Guid tripId,
        Guid sourceCategoryId,
        string categoryName,
        CatalogItemSnapshot snapshot,
        TripComposition composition,
        TripDates dates)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(composition);
        ArgumentNullException.ThrowIfNull(dates);

        return new TripItem(
            Guid.CreateVersion7(),
            tripId,
            sourceCategoryId,
            snapshot.SourceItemId,
            Guard.RequiredText(categoryName, 60, "Categorienaam"),
            Guard.RequiredText(snapshot.Name, 100, "Itemnaam"),
            Amount.Create(snapshot.Quantity, snapshot.Unit, snapshot.Basis, snapshot.Period),
            composition,
            dates);
    }

    /// <summary>Replaces the quantity rule; only while the item is still to do, because a packed total must stay what was packed.</summary>
    internal void ChangeAmount(Amount? amount, TripComposition composition, TripDates dates)
    {
        EnsureNotDeleted();

        if (Status is not PackingStatus.Active)
            throw new DomainValidationException($"'{Name}' is al opgepakt; het aantal kan alleen bij Te doen worden gewijzigd.");

        Amount = amount;
        Total = amount?.TotalFor(composition, dates);
    }

    /// <summary>
    /// Follows a new composition and dates. Only items still to do whose rule depends on who or when change; everything
    /// else keeps its total.
    /// </summary>
    /// <returns><c>true</c> when the total changed.</returns>
    internal bool FollowPlan(TripComposition composition, TripDates dates)
    {
        if (IsDeleted || Status is not PackingStatus.Active || Amount is null || !Amount.DependsOnPlan)
            return false;

        int total = Amount.TotalFor(composition, dates);

        if (total == Total)
            return false;

        Total = total;

        return true;
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
