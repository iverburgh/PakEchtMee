using Microsoft.EntityFrameworkCore;
using Shared.Tests;
using Shouldly;
using Trips.Domain;
using Trips.Infrastructure.Sql.Repositories;
using Xunit;

namespace Trips.Infrastructure.Sql.Tests;

[IntegrationTest]
public sealed class TripRepositoryTests(TripsDatabaseFixture fixture) : IClassFixture<TripsDatabaseFixture>
{
    private static CategorySelection ACategory(Guid categoryId, string name, int itemCount)
        => new(categoryId, name, [.. Enumerable.Range(1, itemCount).Select(index => new CatalogItemSnapshot(Guid.CreateVersion7(), $"{name} {index}", index, "piece"))]);

    private async Task<Guid> GivenTripWithItemsAsync(Guid categoryId, int itemCount = 3)
    {
        await using TripsDbContext context = fixture.CreateContext();
        TripRepository repository = new(context);
        Trip trip = Trip.Create(TripName.Create($"Trip {Guid.NewGuid():N}"), TripDates.None);
        trip.SelectCategories([ACategory(categoryId, "Toiletries", itemCount)]);
        repository.Add(trip);
        await repository.SaveChangesAsync(TestContext.Current.CancellationToken);

        return trip.Id;
    }

    [Fact]
    public async Task A_generated_list_is_stored_with_its_categories_and_amounts()
    {
        Guid categoryId = Guid.CreateVersion7();
        Guid tripId = await GivenTripWithItemsAsync(categoryId, 4);

        await using TripsDbContext context = fixture.CreateContext();
        Trip? trip = await new TripRepository(context).FindAsync(tripId, TestContext.Current.CancellationToken);

        trip.ShouldNotBeNull();
        trip.Items.Count.ShouldBe(4);
        trip.Categories.Count.ShouldBe(1);
        trip.Items.ShouldAllBe(item => item.Status == PackingStatus.Active);
        trip.Items[0].Amount!.Unit.ShouldBe("piece");
    }

    [Fact]
    public async Task An_unknown_trip_returns_nothing()
    {
        await using TripsDbContext context = fixture.CreateContext();

        (await new TripRepository(context).FindAsync(Guid.CreateVersion7(), TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    [Fact]
    public async Task Status_changes_are_persisted()
    {
        Guid tripId = await GivenTripWithItemsAsync(Guid.CreateVersion7(), 1);
        Guid itemId;

        await using (TripsDbContext context = fixture.CreateContext())
        {
            TripRepository repository = new(context);
            Trip trip = (await repository.FindAsync(tripId, TestContext.Current.CancellationToken))!;
            itemId = trip.Items[0].Id;
            trip.AdvanceItem(itemId);
            trip.AdvanceItem(itemId);
            await repository.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using TripsDbContext verification = fixture.CreateContext();
        Trip reloaded = (await new TripRepository(verification).FindAsync(tripId, TestContext.Current.CancellationToken))!;

        reloaded.Items[0].Status.ShouldBe(PackingStatus.Packed);
    }

    [Fact]
    public async Task Deleting_and_restoring_an_item_survives_a_reload()
    {
        Guid tripId = await GivenTripWithItemsAsync(Guid.CreateVersion7(), 2);
        Guid itemId;

        await using (TripsDbContext context = fixture.CreateContext())
        {
            TripRepository repository = new(context);
            Trip trip = (await repository.FindAsync(tripId, TestContext.Current.CancellationToken))!;
            itemId = trip.Items[0].Id;
            trip.AdvanceItem(itemId);
            trip.DeleteItem(itemId, DateTimeOffset.UtcNow);
            await repository.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using (TripsDbContext context = fixture.CreateContext())
        {
            TripRepository repository = new(context);
            Trip trip = (await repository.FindAsync(tripId, TestContext.Current.CancellationToken))!;
            trip.Progress().Total.ShouldBe(1);
            trip.RestoreItem(itemId);
            await repository.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using TripsDbContext verification = fixture.CreateContext();
        Trip reloaded = (await new TripRepository(verification).FindAsync(tripId, TestContext.Current.CancellationToken))!;

        reloaded.Progress().Total.ShouldBe(2);
        reloaded.Items.Single(item => item.Id == itemId).Status.ShouldBe(PackingStatus.Prepared);
    }

    [Fact]
    public async Task Removing_a_category_deletes_its_items_in_the_database()
    {
        Guid categoryId = Guid.CreateVersion7();
        Guid tripId = await GivenTripWithItemsAsync(categoryId, 3);

        await using (TripsDbContext context = fixture.CreateContext())
        {
            TripRepository repository = new(context);
            Trip trip = (await repository.FindAsync(tripId, TestContext.Current.CancellationToken))!;
            trip.RemoveCategory(categoryId, DateTimeOffset.UtcNow);
            await repository.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using TripsDbContext verification = fixture.CreateContext();
        Trip reloaded = (await new TripRepository(verification).FindAsync(tripId, TestContext.Current.CancellationToken))!;

        reloaded.Categories.ShouldBeEmpty();
        reloaded.Items.ShouldAllBe(item => item.IsDeleted);
    }

    [Fact]
    public async Task Selecting_a_category_on_a_persisted_trip_appends_its_items()
    {
        Guid tripId;

        await using (TripsDbContext context = fixture.CreateContext())
        {
            TripRepository repository = new(context);
            Trip trip = Trip.Create(TripName.Create($"Trip {Guid.NewGuid():N}"), TripDates.None);
            repository.Add(trip);
            await repository.SaveChangesAsync(TestContext.Current.CancellationToken);
            tripId = trip.Id;
        }

        await using (TripsDbContext context = fixture.CreateContext())
        {
            TripRepository repository = new(context);
            Trip trip = (await repository.FindAsync(tripId, TestContext.Current.CancellationToken))!;
            trip.SelectCategories([ACategory(Guid.CreateVersion7(), "Toiletries", 3)]);
            await repository.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using TripsDbContext verification = fixture.CreateContext();
        Trip reloaded = (await new TripRepository(verification).FindAsync(tripId, TestContext.Current.CancellationToken))!;

        reloaded.Items.Count.ShouldBe(3);
        reloaded.Categories.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Two_overlapping_updates_surface_as_a_concurrency_failure()
    {
        Guid tripId = await GivenTripWithItemsAsync(Guid.CreateVersion7(), 1);

        await using TripsDbContext first = fixture.CreateContext();
        await using TripsDbContext second = fixture.CreateContext();
        TripRepository firstRepository = new(first);
        TripRepository secondRepository = new(second);

        Trip firstTrip = (await firstRepository.FindAsync(tripId, TestContext.Current.CancellationToken))!;
        Trip secondTrip = (await secondRepository.FindAsync(tripId, TestContext.Current.CancellationToken))!;

        firstTrip.AdvanceItem(firstTrip.Items[0].Id);
        await firstRepository.SaveChangesAsync(TestContext.Current.CancellationToken);

        secondTrip.AdvanceItem(secondTrip.Items[0].Id);

        await Should.ThrowAsync<DbUpdateConcurrencyException>(
            () => secondRepository.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Listing_returns_the_stored_trips_with_their_items()
    {
        await GivenTripWithItemsAsync(Guid.CreateVersion7(), 2);

        await using TripsDbContext context = fixture.CreateContext();
        IReadOnlyList<Trip> trips = await new TripRepository(context).ListAsync(TestContext.Current.CancellationToken);

        trips.ShouldNotBeEmpty();
        trips.ShouldContain(trip => trip.Progress().Total == 2);
    }
}
