using Shared.Validation;

namespace Trips.Domain;

/// <summary>A category selected for a trip. The name is a snapshot, so renaming the catalog later leaves the trip untouched.</summary>
public sealed class TripCategory
{
    private TripCategory(Guid tripId, Guid categoryId, string name)
    {
        TripId = tripId;
        CategoryId = categoryId;
        Name = name;
    }

    private TripCategory() => Name = null!;

    public Guid TripId { get; private set; }

    public Guid CategoryId { get; private set; }

    public string Name { get; private set; }

    internal static TripCategory Create(Guid tripId, Guid categoryId, string name)
        => new(tripId, categoryId, Guard.RequiredText(name, 60, "Categorienaam"));
}
