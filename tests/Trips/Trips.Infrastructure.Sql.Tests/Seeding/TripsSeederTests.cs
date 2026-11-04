using Microsoft.EntityFrameworkCore;
using Shared.Tests;
using Shouldly;
using Trips.Domain;
using Trips.Infrastructure.Sql.Seeding;
using Xunit;

namespace Trips.Infrastructure.Sql.Tests.Seeding;

/// <summary>Every test gets its own database, because the seed uses the same trip id in every run.</summary>
[IntegrationTest]
public sealed class TripsSeederTests(SqlServerTestContainerFixture server)
{
    private static readonly Guid Family = Guid.CreateVersion7();

    private static readonly Guid TripId = Guid.CreateVersion7();
    private static readonly Guid CategoryId = Guid.CreateVersion7();
    private static readonly Guid SwimmingId = Guid.CreateVersion7();
    private static readonly Guid JacketId = Guid.CreateVersion7();
    private static readonly Guid GlovesId = Guid.CreateVersion7();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static TripSeed AWinterTrip() => new(
        TripId,
        Family,
        "Wintersport Frankrijk 2025",
        TripComposition.Create(2, 3),
        [new CategorySelection(
            CategoryId,
            "Kinderen – Vermaak op locatie",
            [
                new CatalogItemSnapshot(SwimmingId, "Zwembandjes", null, null),
                new CatalogItemSnapshot(JacketId, "Jassen", 1, null, QuantityBasis.PerChild),
                new CatalogItemSnapshot(GlovesId, "Wanten", 3, "paar"),
            ])],
        [SwimmingId]);

    private async Task<Func<TripsDbContext>> NewDatabaseAsync()
    {
        string connectionString = await server.CreateDatabaseAsync($"trips_seed_{Guid.NewGuid():N}");
        TripsDbContext CreateContext() => new(new DbContextOptionsBuilder<TripsDbContext>().UseSqlServer(connectionString).Options);

        await using TripsDbContext context = CreateContext();
        await context.Database.MigrateAsync(CancellationToken);

        return CreateContext;
    }

    private static async Task<bool> SeedAsync(Func<TripsDbContext> createContext)
    {
        await using TripsDbContext context = createContext();

        return await new TripsSeeder(context, TimeProvider.System).SeedAsync(AWinterTrip(), CancellationToken);
    }

    private static async Task<Trip> LoadAsync(Func<TripsDbContext> createContext)
    {
        await using TripsDbContext context = createContext();

        return await context.Trips
            .Include(trip => trip.Categories)
            .Include(trip => trip.Items)
            .SingleAsync(trip => trip.Id == TripId, CancellationToken);
    }

    [Fact]
    public async Task A_new_trip_gets_its_items_with_the_listed_ones_deleted()
    {
        Func<TripsDbContext> createContext = await NewDatabaseAsync();

        bool added = await SeedAsync(createContext);

        added.ShouldBeTrue();
        Trip trip = await LoadAsync(createContext);
        trip.Name.Value.ShouldBe("Wintersport Frankrijk 2025");
        trip.FamilyId.ShouldBe(Family);
        trip.Dates.ShouldBe(TripDates.None);
        trip.Categories.Count.ShouldBe(1);
        trip.Items.Count.ShouldBe(3);
        trip.Items.Single(item => item.SourceItemId == SwimmingId).IsDeleted.ShouldBeTrue();
        trip.Items.Where(item => item.SourceItemId != SwimmingId).ShouldAllBe(item => !item.IsDeleted && item.Status == PackingStatus.Active);
        trip.Items.Single(item => item.SourceItemId == GlovesId).Amount!.ToString().ShouldBe("3 paar");
    }

    [Fact]
    public async Task A_new_trip_gets_the_seeded_composition_and_totals()
    {
        Func<TripsDbContext> createContext = await NewDatabaseAsync();

        await SeedAsync(createContext);

        Trip trip = await LoadAsync(createContext);
        trip.Composition.ShouldBe(TripComposition.Create(2, 3));
        trip.Items.Single(item => item.SourceItemId == JacketId).Total.ShouldBe(3);
    }

    [Fact]
    public async Task A_second_run_changes_nothing()
    {
        Func<TripsDbContext> createContext = await NewDatabaseAsync();
        await SeedAsync(createContext);

        bool added = await SeedAsync(createContext);

        added.ShouldBeFalse();
        await using TripsDbContext context = createContext();
        (await context.Trips.CountAsync(CancellationToken)).ShouldBe(1);
        (await context.Set<TripItem>().CountAsync(CancellationToken)).ShouldBe(3);
    }

    [Fact]
    public async Task Packing_progress_survives_a_second_run()
    {
        Func<TripsDbContext> createContext = await NewDatabaseAsync();
        await SeedAsync(createContext);
        await using (TripsDbContext context = createContext())
        {
            Trip trip = await context.Trips.Include(trip => trip.Items).SingleAsync(trip => trip.Id == TripId, CancellationToken);
            Guid jacket = trip.Items.Single(item => item.SourceItemId == JacketId).Id;
            trip.AdvanceItem(jacket);
            trip.AdvanceItem(jacket);
            await context.SaveChangesAsync(CancellationToken);
        }

        await SeedAsync(createContext);

        Trip reloaded = await LoadAsync(createContext);
        reloaded.Items.Single(item => item.SourceItemId == JacketId).Status.ShouldBe(PackingStatus.Packed);
    }

    [Fact]
    public async Task Two_trips_can_be_seeded_through_one_context()
    {
        Func<TripsDbContext> createContext = await NewDatabaseAsync();

        await using (TripsDbContext context = createContext())
        {
            TripsSeeder seeder = new(context, TimeProvider.System);
            await seeder.SeedAsync(AWinterTrip() with { Categories = [] }, CancellationToken);
            await seeder.SeedAsync(AWinterTrip() with { Id = Guid.CreateVersion7(), Name = "Kamperen Frankrijk 2026", Categories = [] }, CancellationToken);
        }

        await using TripsDbContext check = createContext();
        (await check.Trips.CountAsync(CancellationToken)).ShouldBe(2);
    }

    [Fact]
    public async Task A_trip_without_categories_is_created_empty()
    {
        Func<TripsDbContext> createContext = await NewDatabaseAsync();

        await using (TripsDbContext context = createContext())
        {
            await new TripsSeeder(context, TimeProvider.System).SeedAsync(AWinterTrip() with { Categories = [] }, CancellationToken);
        }

        Trip trip = await LoadAsync(createContext);
        trip.Items.ShouldBeEmpty();
    }
}
