namespace Trips.Domain;

public interface ITripRepository
{
    /// <summary>Loads a trip with its categories and items, which is the unit every trip command works on.</summary>
    Task<Trip?> FindAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Trip>> ListAsync(CancellationToken cancellationToken);

    void Add(Trip trip);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
