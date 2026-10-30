using Catalog.Domain;
using Catalog.Infrastructure.Sql.Seeding;
using CSharpFunctionalExtensions;
using Families.Domain;
using MediatR;
using Trips.Domain;
using Trips.Infrastructure.Sql.Seeding;

namespace Web.Seeding;

/// <summary>
/// Fills a development database with the family packing list and two trips. The catalog goes first; the trips are then
/// generated from what the catalog really holds, so a seeded category the user removed is simply left out of a new trip.
/// </summary>
internal sealed class DevelopmentSeeder(
    CatalogSeeder catalogSeeder,
    TripsSeeder tripsSeeder,
    ISender sender,
    ILogger<DevelopmentSeeder> logger)
{
    internal static readonly Guid CampingTripId = new("0196a3c2-5e10-7a41-9b2d-6f8e1c4d2a01");
    internal static readonly Guid WinterSportsTripId = new("0196a3c2-5e10-7a41-9b2d-6f8e1c4d2a02");

    internal const string CampingTripName = "Kamperen Frankrijk 2026";
    internal const string WinterSportsTripName = "Wintersport Frankrijk 2025";

    /// <summary>Summer-only items that are put on deleted for winter sports; restorable on the trip if they are needed after all.</summary>
    internal static readonly IReadOnlyList<Guid> WinterSportsSummerItemIds =
    [
        CatalogSeedData.ItemId(CatalogSeedData.KinderenBuitenkleding, "Crocs/sandalen/slippers"),
        .. new[]
        {
            "Duikringen", "Opblaasband", "Handdoek(en) zwemmen", "Zwemkleding", "Zwembandjes", "Drijfpakje", "Tafeltennisspullen",
            "Badminton rackets", "Klitterband bal", "Zwembrillen", "Zeemeerminstaarten", "Waterspuiten",
        }.Select(name => CatalogSeedData.ItemId(CatalogSeedData.KinderenVermaak, name)),
        CatalogSeedData.ItemId(CatalogSeedData.Buitenkleding, "Slippers/crocs/teva's"),
        CatalogSeedData.ItemId(CatalogSeedData.Vermaak, "Zwemkleding I & M"),
        CatalogSeedData.ItemId(CatalogSeedData.Vermaak, "Badhanddoeken"),
    ];

    internal static IReadOnlyList<Guid> CampingCategoryIds
        => [.. CatalogSeedData.Categories.Select(category => category.Id)];

    internal static IReadOnlyList<Guid> WinterSportsCategoryIds
        => [.. CatalogSeedData.Categories
            .Where(category => !CatalogSeedData.CampingCategories.Contains(category.Name))
            .Select(category => category.Id)];

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        CatalogSeedResult catalog = await catalogSeeder.SeedAsync(CatalogSeedData.Categories, cancellationToken);

        int tripsAdded = 0;

        if (await SeedTripAsync(CampingTripId, CampingTripName, CampingCategoryIds, [], cancellationToken))
            tripsAdded++;

        if (await SeedTripAsync(WinterSportsTripId, WinterSportsTripName, WinterSportsCategoryIds, WinterSportsSummerItemIds, cancellationToken))
            tripsAdded++;

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Development seed applied: {Categories} categories, {Items} items and {Trips} trips added.",
                catalog.CategoriesAdded,
                catalog.ItemsAdded,
                tripsAdded);
        }
    }

    private async Task<bool> SeedTripAsync(
        Guid id,
        string name,
        IReadOnlyList<Guid> categoryIds,
        IReadOnlyCollection<Guid> deletedSourceItemIds,
        CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<CategorySelection>, Exception> selections =
            await CategorySelections.ReadAsync(sender, CatalogActor.MemberOf(KnownFamilies.Verburgh), categoryIds, cancellationToken);

        // A broken seed must stop startup rather than leave a half-seeded database unnoticed.
        if (selections.IsFailure)
            throw new InvalidOperationException($"Reading the catalog for seed trip '{name}' failed.", selections.Error);

        return await tripsSeeder.SeedAsync(new TripSeed(id, KnownFamilies.Verburgh, name, selections.Value, deletedSourceItemIds), cancellationToken);
    }
}
