namespace Trips.Application.Contracts;

/// <summary>
/// Narrows a trip's items. Values within one kind are a union; the two kinds intersect.
/// An empty filter matches everything, including deleted items, so the client can choose what to show.
/// </summary>
public sealed record TripItemFilter(IReadOnlyCollection<Guid> CategoryIds, IReadOnlyCollection<string> Statuses)
{
    public static readonly TripItemFilter None = new([], []);

    public bool IsEmpty => CategoryIds.Count is 0 && Statuses.Count is 0;

    public bool Matches(TripItemDto item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (CategoryIds.Count > 0 && !CategoryIds.Contains(item.CategoryId))
            return false;

        return Statuses.Count is 0
            || Statuses.Any(status => string.Equals(status, item.Status, StringComparison.OrdinalIgnoreCase));
    }
}
