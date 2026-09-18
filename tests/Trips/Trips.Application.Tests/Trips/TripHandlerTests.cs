using CSharpFunctionalExtensions;
using Moq;
using Shared.Validation;
using Shouldly;
using Trips.Application.Contracts;
using Trips.Application.Trips;
using Trips.Domain;
using Xunit;

namespace Trips.Application.Tests.Trips;

public sealed class TripHandlerTests
{
    private readonly TripsHarness harness = new();

    [Fact]
    public async Task Creating_a_trip_with_dates_stores_it()
    {
        Result<TripSummaryDto, Exception> result = await harness.Mediator.Send(
            new CreateTripHandler.Command("Summer France", new DateOnly(2026, 7, 4), new DateOnly(2026, 7, 18)),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Summer France");
        result.Value.EndDate.ShouldBe(new DateOnly(2026, 7, 18));
        harness.Repository.Verify(repository => repository.Add(It.IsAny<Trip>()), Times.Once);
        harness.Repository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Creating_a_trip_without_dates_is_allowed()
    {
        Result<TripSummaryDto, Exception> result = await harness.Mediator.Send(
            new CreateTripHandler.Command("Day at the beach", null, null), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.StartDate.ShouldBeNull();
    }

    [Fact]
    public async Task Creating_a_trip_with_an_end_before_the_start_is_rejected()
    {
        Result<TripSummaryDto, Exception> result = await harness.Mediator.Send(
            new CreateTripHandler.Command("Summer France", new DateOnly(2026, 7, 18), new DateOnly(2026, 7, 4)),
            TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<DomainValidationException>();
        harness.Repository.Verify(repository => repository.Add(It.IsAny<Trip>()), Times.Never);
    }

    [Fact]
    public async Task A_failing_repository_becomes_a_failed_result()
    {
        harness.Repository
            .Setup(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("database unavailable"));

        Result<TripSummaryDto, Exception> result = await harness.Mediator.Send(
            new CreateTripHandler.Command("Summer France", null, null), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<InvalidOperationException>();
    }

    [Fact]
    public async Task Listing_puts_ongoing_and_upcoming_trips_first()
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        Trip past = Trip.Create(TripName.Create("Last winter"), TripDates.Create(today.AddDays(-30), today.AddDays(-20)));
        Trip ongoing = Trip.Create(TripName.Create("Right now"), TripDates.Create(today.AddDays(-1), today.AddDays(2)));
        Trip upcoming = Trip.Create(TripName.Create("Next month"), TripDates.Create(today.AddDays(30), today.AddDays(40)));
        Trip undated = Trip.Create(TripName.Create("Someday"), TripDates.None);

        harness.Repository
            .Setup(repository => repository.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([past, undated, upcoming, ongoing]);

        Result<IReadOnlyList<TripSummaryDto>, Exception> result = await harness.Mediator.Send(
            new ListTripsHandler.Query(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Select(trip => trip.Name).ShouldBe(["Right now", "Next month", "Someday", "Last winter"]);
    }

    [Fact]
    public async Task Reading_an_unknown_trip_reports_not_found()
    {
        Result<TripDetailsDto, Exception> result = await harness.Mediator.Send(
            new GetTripHandler.Query(Guid.CreateVersion7()), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<NotFoundException>();
    }

    [Fact]
    public async Task Selecting_categories_generates_the_packing_list()
    {
        Trip trip = harness.GivenTrip();
        Guid toiletries = Guid.CreateVersion7();
        Guid electronics = Guid.CreateVersion7();

        Result<TripDetailsDto, Exception> result = await harness.Mediator.Send(
            new SelectTripCategoriesHandler.Command(
                trip.Id,
                [TripsHarness.ACategory(toiletries, "Toiletries", 4), TripsHarness.ACategory(electronics, "Electronics", 3)]),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Count.ShouldBe(7);
        result.Value.Items.ShouldAllBe(item => item.Status == "Active");
        result.Value.Progress.Total.ShouldBe(7);
    }

    [Fact]
    public async Task Selecting_no_categories_is_rejected()
    {
        Trip trip = harness.GivenTrip();

        Result<TripDetailsDto, Exception> result = await harness.Mediator.Send(
            new SelectTripCategoriesHandler.Command(trip.Id, []), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<DomainValidationException>();
    }

    [Fact]
    public async Task Selecting_categories_for_an_unknown_trip_reports_not_found()
    {
        Result<TripDetailsDto, Exception> result = await harness.Mediator.Send(
            new SelectTripCategoriesHandler.Command(Guid.CreateVersion7(), [TripsHarness.ACategory(Guid.CreateVersion7(), "Toiletries", 1)]),
            TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<NotFoundException>();
    }

    [Fact]
    public async Task Removing_a_category_deletes_its_items_and_reports_the_progress()
    {
        Trip trip = harness.GivenTrip();
        Guid toiletries = Guid.CreateVersion7();
        trip.SelectCategories([TripsHarness.ACategory(toiletries, "Toiletries", 3), TripsHarness.ACategory(Guid.CreateVersion7(), "Electronics", 2)]);

        Result<TripDetailsDto, Exception> result = await harness.Mediator.Send(
            new RemoveTripCategoryHandler.Command(trip.Id, toiletries), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Categories.Count.ShouldBe(1);
        result.Value.Progress.Total.ShouldBe(2);
        result.Value.Items.Count(item => item.IsDeleted).ShouldBe(3);
    }

    [Fact]
    public async Task A_category_with_progress_is_flagged_so_the_client_can_ask_for_confirmation()
    {
        Trip trip = harness.GivenTrip();
        Guid toiletries = Guid.CreateVersion7();
        trip.SelectCategories([TripsHarness.ACategory(toiletries, "Toiletries", 2)]);
        trip.AdvanceItem(trip.Items[0].Id);

        Result<TripDetailsDto, Exception> result = await harness.Mediator.Send(
            new GetTripHandler.Query(trip.Id), TestContext.Current.CancellationToken);

        result.Value.Categories.Single().HasProgressedItems.ShouldBeTrue();
    }
}
