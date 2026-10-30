using Families.Domain;
using Microsoft.EntityFrameworkCore;

namespace Families.Infrastructure.Sql.Repositories;

/// <summary>No try/catch here on purpose: failures are turned into failed results by the MediatR pipeline.</summary>
internal sealed class FamilyRepository(FamiliesDbContext context) : IFamilyRepository
{
    public async Task<Family?> FindAsync(Guid id, CancellationToken cancellationToken)
        => await context.Families
            .Include(family => family.Members)
            .FirstOrDefaultAsync(family => family.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Family>> ListAsync(CancellationToken cancellationToken)
        => await context.Families
            .Include(family => family.Members)
            .OrderBy(family => family.Name)
            .ToListAsync(cancellationToken);

    public async Task<Family?> FindByMemberAsync(EmailAddress email, CancellationToken cancellationToken)
        => await context.Families
            .Include(family => family.Members)
            .FirstOrDefaultAsync(family => family.Members.Any(member => member.Email == email), cancellationToken);

    public async Task<bool> ExistsWithNameAsync(FamilyName name, Guid? excludingId, CancellationToken cancellationToken)
        => await context.Families.AnyAsync(
            family => family.Name == name && (excludingId == null || family.Id != excludingId),
            cancellationToken);

    public void Add(Family family) => context.Families.Add(family);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
