using Microsoft.EntityFrameworkCore;
using Trips.Domain;

namespace Trips.Infrastructure.Sql.Repositories;

internal sealed class TripRepository(TripsDbContext context) : ITripRepository
{
    public async Task<Trip?> FindAsync(Guid id, Guid familyId, CancellationToken cancellationToken)
        => await context.Trips
            .Include(trip => trip.Categories)
            .Include(trip => trip.Items)
            .FirstOrDefaultAsync(trip => trip.Id == id && trip.FamilyId == familyId, cancellationToken);

    public async Task<IReadOnlyList<Trip>> ListAsync(Guid familyId, CancellationToken cancellationToken)
        => await context.Trips
            .Include(trip => trip.Items)
            .Where(trip => trip.FamilyId == familyId)
            .ToListAsync(cancellationToken);

    public void Add(Trip trip) => context.Trips.Add(trip);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
