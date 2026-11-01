using Catalog.Infrastructure.Sql.Seeding;

namespace Web.Seeding;

/// <summary>
/// Gives every non-development database the general catalog from the family packing list. Safe on every start: the
/// catalog seeder only inserts what is missing and never brings back a category the administrator removed.
/// </summary>
internal sealed class CatalogStartupSeeder(CatalogSeeder catalogSeeder, ILogger<CatalogStartupSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        CatalogSeedResult catalog = await catalogSeeder.SeedAsync(CatalogSeedData.Categories, cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("General catalog seed applied: {Categories} categories and {Items} items added.", catalog.CategoriesAdded, catalog.ItemsAdded);
    }
}
