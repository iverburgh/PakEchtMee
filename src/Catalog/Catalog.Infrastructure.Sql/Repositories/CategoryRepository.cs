using Catalog.Domain.Categories;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Sql.Repositories;

/// <summary>No try/catch here on purpose: failures are turned into failed results by the MediatR pipeline.</summary>
internal sealed class CategoryRepository(CatalogDbContext context) : ICategoryRepository
{
    public async Task<IReadOnlyList<Category>> ListVisibleAsync(Guid? familyId, bool includeDeleted, CancellationToken cancellationToken)
        => await context.Categories
            .Where(category => category.FamilyId == null || (familyId != null && category.FamilyId == familyId))
            .Where(category => includeDeleted || category.DeletedAt == null)
            .OrderBy(category => category.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Category>> ListGeneralAsync(bool includeDeleted, CancellationToken cancellationToken)
        => await context.Categories
            .Where(category => category.FamilyId == null && (includeDeleted || category.DeletedAt == null))
            .OrderBy(category => category.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Category>> FindManyAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
        => await context.Categories
            .Where(category => ids.Contains(category.Id))
            .ToListAsync(cancellationToken);

    public async Task<Category?> FindAsync(Guid id, CancellationToken cancellationToken)
        => await context.Categories.FirstOrDefaultAsync(category => category.Id == id, cancellationToken);

    public async Task<bool> ExistsWithNameAsync(CategoryName name, Guid? familyId, Guid? excludingId, CancellationToken cancellationToken)
        => await context.Categories.AnyAsync(
            category => category.DeletedAt == null
                && category.FamilyId == familyId
                && category.Name == name
                && (excludingId == null || category.Id != excludingId),
            cancellationToken);

    public async Task<IReadOnlySet<Guid>> ListSwitchedOffAsync(Guid familyId, CancellationToken cancellationToken)
        => (await context.OptOuts
            .Where(optOut => optOut.FamilyId == familyId)
            .Select(optOut => optOut.CategoryId)
            .ToListAsync(cancellationToken))
            .ToHashSet();

    public async Task SwitchAsync(Guid familyId, Category category, bool on, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(category);

        GeneralCategoryOptOut? existing = await context.OptOuts.FirstOrDefaultAsync(
            optOut => optOut.FamilyId == familyId && optOut.CategoryId == category.Id,
            cancellationToken);

        if (on && existing is not null)
            context.OptOuts.Remove(existing);
        else if (!on && existing is null)
            context.OptOuts.Add(GeneralCategoryOptOut.Create(familyId, category));
    }

    public void Add(Category category) => context.Categories.Add(category);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
