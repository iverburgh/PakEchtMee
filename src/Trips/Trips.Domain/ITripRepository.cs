namespace Trips.Domain;

public interface ITripRepository
{
    /// <summary>
    /// Loads a trip of the given family with its categories and items, which is the unit every trip command works on.
    /// A trip of another family is not found, so no handler can act on it by accident.
    /// </summary>
    Task<Trip?> FindAsync(Guid id, Guid familyId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Trip>> ListAsync(Guid familyId, CancellationToken cancellationToken);

    void Add(Trip trip);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
