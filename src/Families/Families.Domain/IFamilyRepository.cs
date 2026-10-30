namespace Families.Domain;

public interface IFamilyRepository
{
    /// <summary>Loads a family with its members.</summary>
    Task<Family?> FindAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Family>> ListAsync(CancellationToken cancellationToken);

    /// <summary>The family the address is a member of, if any; this is the lookup behind every API request.</summary>
    Task<Family?> FindByMemberAsync(EmailAddress email, CancellationToken cancellationToken);

    /// <summary>Case-insensitive name check among families, optionally ignoring one family.</summary>
    Task<bool> ExistsWithNameAsync(FamilyName name, Guid? excludingId, CancellationToken cancellationToken);

    void Add(Family family);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
