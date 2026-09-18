using Microsoft.EntityFrameworkCore;
using Trips.Domain;

namespace Trips.Infrastructure.Sql.Repositories;

internal sealed class TripRepository(TripsDbContext context) : ITripRepository
{
    public async Task<Trip?> FindAsync(Guid id, CancellationToken cancellationToken)
        => await context.Trips
            .Include(trip => trip.Categories)
            .Include(trip => trip.Items)
            .FirstOrDefaultAsync(trip => trip.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Trip>> ListAsync(CancellationToken cancellationToken)
        => await context.Trips
            .Include(trip => trip.Items)
            .ToListAsync(cancellationToken);

    public void Add(Trip trip) => context.Trips.Add(trip);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
