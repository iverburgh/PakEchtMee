using Catalog.Domain.Items;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Sql.Repositories;

internal sealed class CatalogItemRepository(CatalogDbContext context) : ICatalogItemRepository
{
    public async Task<IReadOnlyList<CatalogItem>> ListByCategoryAsync(Guid categoryId, bool includeDeleted, CancellationToken cancellationToken)
        => await context.Items
            .Where(item => item.CategoryId == categoryId && (includeDeleted || item.DeletedAt == null))
            .OrderBy(item => item.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CatalogItem>> ListByCategoriesAsync(IReadOnlyCollection<Guid> categoryIds, CancellationToken cancellationToken)
        => await context.Items
            .Where(item => categoryIds.Contains(item.CategoryId) && item.DeletedAt == null)
            .OrderBy(item => item.Name)
            .ToListAsync(cancellationToken);

    public async Task<CatalogItem?> FindAsync(Guid id, CancellationToken cancellationToken)
        => await context.Items.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public async Task<bool> ExistsWithNameInCategoryAsync(Guid categoryId, ItemName name, Guid? excludingId, CancellationToken cancellationToken)
        => await context.Items.AnyAsync(
            item => item.CategoryId == categoryId
                && item.DeletedAt == null
                && item.Name == name
                && (excludingId == null || item.Id != excludingId),
            cancellationToken);

    public void Add(CatalogItem item) => context.Items.Add(item);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
