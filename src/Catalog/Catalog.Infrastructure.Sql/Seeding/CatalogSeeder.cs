using System.Globalization;
using Catalog.Domain.Categories;
using Catalog.Domain.Items;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Catalog.Infrastructure.Sql.Seeding;

public sealed record CatalogSeedResult(int CategoriesAdded, int ItemsAdded);

/// <summary>
/// Inserts <see cref="CatalogSeedData"/> records that do not exist yet. Existing records are matched by id including
/// removed ones, so whatever was renamed, moved or removed stays that way; name clashes in the general catalog are skipped.
/// </summary>
public sealed class CatalogSeeder(CatalogDbContext context, ILogger<CatalogSeeder> logger)
{
    public async Task<CatalogSeedResult> SeedAsync(IReadOnlyList<SeedCategory> seed, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(seed);

        List<Guid> categoryIds = [.. seed.Select(category => category.Id)];
        List<Guid> itemIds = [.. seed.SelectMany(category => category.Items).Select(item => item.Id)];

        Dictionary<Guid, Category> existingCategories = await context.Categories
            .Where(category => categoryIds.Contains(category.Id))
            .ToDictionaryAsync(category => category.Id, cancellationToken);

        HashSet<Guid> existingItemIds = [.. await context.Items
            .Where(item => itemIds.Contains(item.Id))
            .Select(item => item.Id)
            .ToListAsync(cancellationToken)];

        // The seed is the general catalog, so only general names can clash; a family may use the same names for its own.
        List<string> liveCategoryNames = await context.Categories
            .Where(category => category.DeletedAt == null && category.FamilyId == null)
            .Select(category => category.Name.Value)
            .ToListAsync(cancellationToken);

        int categoriesAdded = 0;
        int itemsAdded = 0;

        foreach (SeedCategory seedCategory in seed)
        {
            Category? category = existingCategories.GetValueOrDefault(seedCategory.Id);

            if (category is null)
            {
                if (liveCategoryNames.Exists(name => NamesCollide(name, seedCategory.Name)))
                {
                    logger.LogWarning("Seed category {Category} skipped: a category with that name already exists.", seedCategory.Name);
                    continue;
                }

                category = Category.Create(seedCategory.Id, CategoryName.Create(seedCategory.Name));
                context.Categories.Add(category);
                categoriesAdded++;
            }

            if (category.IsDeleted)
                continue;

            itemsAdded += await AddMissingItemsAsync(category, seedCategory, existingItemIds, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);

        return new CatalogSeedResult(categoriesAdded, itemsAdded);
    }

    private async Task<int> AddMissingItemsAsync(
        Category category,
        SeedCategory seedCategory,
        HashSet<Guid> existingItemIds,
        CancellationToken cancellationToken)
    {
        List<SeedItem> missing = [.. seedCategory.Items.Where(item => !existingItemIds.Contains(item.Id))];

        if (missing.Count is 0)
            return 0;

        // Only an existing category can hold user-created items that a seed item would clash with.
        List<string> liveItemNames = await context.Items
            .Where(item => item.CategoryId == category.Id && item.DeletedAt == null)
            .Select(item => item.Name.Value)
            .ToListAsync(cancellationToken);

        int added = 0;

        foreach (SeedItem seedItem in missing)
        {
            if (liveItemNames.Exists(name => NamesCollide(name, seedItem.Name)))
            {
                logger.LogWarning(
                    "Seed item {Item} in {Category} skipped: an item with that name already exists.",
                    seedItem.Name,
                    seedCategory.Name);
                continue;
            }

            context.Items.Add(CatalogItem.Create(seedItem.Id, category, ItemName.Create(seedItem.Name), Amount.Create(seedItem.Quantity, null, seedItem.Basis)));
            added++;
        }

        return added;
    }

    /// <summary>Mirrors the database collation (case- and accent-insensitive), so a skip here matches a unique-index clash there.</summary>
    private static bool NamesCollide(string left, string right)
        => CultureInfo.InvariantCulture.CompareInfo.Compare(left, right, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) == 0;
}
