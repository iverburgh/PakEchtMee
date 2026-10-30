using Microsoft.EntityFrameworkCore;
using Trips.Domain;

namespace Trips.Infrastructure.Sql.Seeding;

/// <summary>A trip to seed: the catalog snapshots to generate it from, and the source items to put on deleted.</summary>
public sealed record TripSeed(
    Guid Id,
    string Name,
    IReadOnlyList<CategorySelection> Categories,
    IReadOnlyCollection<Guid> DeletedSourceItemIds);

/// <summary>
/// Inserts a seeded trip once. An existing trip - matched by id - is never touched again, so packing progress and
/// any category or item changes the user made survive every restart.
/// </summary>
public sealed class TripsSeeder(TripsDbContext context, TimeProvider timeProvider)
{
    /// <returns><c>true</c> when the trip was inserted, <c>false</c> when it already existed.</returns>
    public async Task<bool> SeedAsync(TripSeed seed, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(seed);

        if (await context.Trips.AnyAsync(trip => trip.Id == seed.Id, cancellationToken))
            return false;

        // A fresh instance rather than TripDates.None: EF tracks an owned value per owner, so a shared instance
        // cannot be attached to a second trip in the same context.
        Trip trip = Trip.Create(seed.Id, TripName.Create(seed.Name), TripDates.Create(null, null));

        if (seed.Categories.Count > 0)
            trip.SelectCategories(seed.Categories);

        DateTimeOffset now = timeProvider.GetUtcNow();

        foreach (TripItem item in trip.Items.Where(item => seed.DeletedSourceItemIds.Contains(item.SourceItemId)).ToList())
            trip.DeleteItem(item.Id, now);

        context.Trips.Add(trip);
        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
