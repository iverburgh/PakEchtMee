using Catalog.Domain.Categories;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Sql.Repositories;

/// <summary>No try/catch here on purpose: failures are turned into failed results by the MediatR pipeline.</summary>
internal sealed class CategoryRepository(CatalogDbContext context) : ICategoryRepository
{
    public async Task<IReadOnlyList<Category>> ListAsync(bool includeDeleted, CancellationToken cancellationToken)
        => await context.Categories
            .Where(category => includeDeleted || category.DeletedAt == null)
            .OrderBy(category => category.Name)
            .ToListAsync(cancellationToken);

    public async Task<Category?> FindAsync(Guid id, CancellationToken cancellationToken)
        => await context.Categories.FirstOrDefaultAsync(category => category.Id == id, cancellationToken);

    public async Task<bool> ExistsWithNameAsync(CategoryName name, Guid? excludingId, CancellationToken cancellationToken)
        => await context.Categories.AnyAsync(
            category => category.DeletedAt == null
                && category.Name == name
                && (excludingId == null || category.Id != excludingId),
            cancellationToken);

    public void Add(Category category) => context.Categories.Add(category);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
