using CSharpFunctionalExtensions;
using Moq;
using Shared.Validation;
using Shouldly;
using Trips.Application.Contracts;
using Trips.Application.Items;
using Trips.Application.Trips;
using Trips.Domain;
using Xunit;

namespace Trips.Application.Tests.Items;

public sealed class TripQuantityHandlerTests
{
    private readonly TripsHarness harness = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private Trip GivenTripWithJackets(out TripItem jackets)
    {
        Trip trip = harness.GivenTrip();
        trip.ChangeComposition(TripComposition.Create(2, 3));
        CatalogItemSnapshot snapshot = new(Guid.CreateVersion7(), "Jassen", 1, null, QuantityBasis.PerChild);
        trip.SelectCategories([new CategorySelection(Guid.CreateVersion7(), "Kinderen – Buitenkleding", [snapshot])]);
        jackets = trip.Items.Single();

        return trip;
    }

    [Fact]
    public async Task Creating_a_trip_takes_the_given_composition()
    {
        Result<TripSummaryDto, Exception> result = await harness.Mediator.Send(
            new CreateTripHandler.Command(TripsHarness.Family, "Summer France", null, null, 2, 3), CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        harness.Repository.Verify(
            repository => repository.Add(It.Is<Trip>(trip => trip.Composition == TripComposition.Create(2, 3))), Times.Once);
    }

    [Fact]
    public async Task Creating_a_trip_with_an_empty_composition_is_rejected()
    {
        Result<TripSummaryDto, Exception> result = await harness.Mediator.Send(
            new CreateTripHandler.Command(TripsHarness.Family, "Summer France", null, null, 0, 0), CancellationToken);

        result.Error.ShouldBeOfType<DomainValidationException>();
        harness.Repository.Verify(repository => repository.Add(It.IsAny<Trip>()), Times.Never);
    }

    [Fact]
    public async Task Changing_the_composition_recalculates_and_reports_the_changes()
    {
        Trip trip = GivenTripWithJackets(out TripItem jackets);

        Result<TripCompositionChangedDto, Exception> result = await harness.Mediator.Send(
            new ChangeTripCompositionHandler.Command(TripsHarness.Family, trip.Id, 2, 2), CancellationToken);

        result.Value.ChangedTotals.ShouldBe(1);
        result.Value.Trip.Adults.ShouldBe(2);
        result.Value.Trip.Children.ShouldBe(2);
        result.Value.Trip.Items.Single(item => item.Id == jackets.Id).Total.ShouldBe(2);
        harness.Repository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Changing_the_composition_of_an_unknown_trip_is_not_found()
    {
        Result<TripCompositionChangedDto, Exception> result = await harness.Mediator.Send(
            new ChangeTripCompositionHandler.Command(TripsHarness.Family, Guid.CreateVersion7(), 2, 2), CancellationToken);

        result.Error.ShouldBeOfType<NotFoundException>();
    }

    [Fact]
    public async Task An_invalid_trip_composition_is_rejected_and_not_saved()
    {
        Trip trip = GivenTripWithJackets(out TripItem jackets);

        Result<TripCompositionChangedDto, Exception> result = await harness.Mediator.Send(
            new ChangeTripCompositionHandler.Command(TripsHarness.Family, trip.Id, 0, 0), CancellationToken);

        result.Error.ShouldBeOfType<DomainValidationException>();
        jackets.Total.ShouldBe(3);
        harness.Repository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task A_failing_repository_while_changing_the_composition_becomes_a_failure()
    {
        Trip trip = GivenTripWithJackets(out _);
        harness.Repository
            .Setup(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("database down"));

        Result<TripCompositionChangedDto, Exception> result = await harness.Mediator.Send(
            new ChangeTripCompositionHandler.Command(TripsHarness.Family, trip.Id, 2, 2), CancellationToken);

        result.Error.ShouldBeOfType<InvalidOperationException>();
    }

    [Fact]
    public async Task Changing_an_items_quantity_returns_the_new_total()
    {
        Trip trip = GivenTripWithJackets(out TripItem jackets);

        Result<TripItemDto, Exception> result = await harness.Mediator.Send(
            new ChangeTripItemAmountHandler.Command(TripsHarness.Family, trip.Id, jackets.Id, 2, null, "PerChild"), CancellationToken);

        result.Value.Quantity.ShouldBe(2);
        result.Value.Basis.ShouldBe("PerChild");
        result.Value.Total.ShouldBe(6);
        harness.Repository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Removing_an_items_quantity_returns_no_total()
    {
        Trip trip = GivenTripWithJackets(out TripItem jackets);

        Result<TripItemDto, Exception> result = await harness.Mediator.Send(
            new ChangeTripItemAmountHandler.Command(TripsHarness.Family, trip.Id, jackets.Id, null, null, null), CancellationToken);

        result.Value.Quantity.ShouldBeNull();
        result.Value.Basis.ShouldBe("Fixed");
        result.Value.Total.ShouldBeNull();
    }

    [Fact]
    public async Task Changing_the_quantity_of_a_progressed_item_is_rejected()
    {
        Trip trip = GivenTripWithJackets(out TripItem jackets);
        trip.AdvanceItem(jackets.Id);

        Result<TripItemDto, Exception> result = await harness.Mediator.Send(
            new ChangeTripItemAmountHandler.Command(TripsHarness.Family, trip.Id, jackets.Id, 2, null, "PerChild"), CancellationToken);

        result.Error.ShouldBeOfType<DomainValidationException>();
        harness.Repository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Changing_an_items_quantity_on_an_unknown_trip_is_not_found()
    {
        Result<TripItemDto, Exception> result = await harness.Mediator.Send(
            new ChangeTripItemAmountHandler.Command(TripsHarness.Family, Guid.CreateVersion7(), Guid.CreateVersion7(), 1, null, null), CancellationToken);

        result.Error.ShouldBeOfType<NotFoundException>();
    }

    [Fact]
    public async Task A_failing_repository_while_changing_a_quantity_becomes_a_failure()
    {
        Trip trip = GivenTripWithJackets(out TripItem jackets);
        harness.Repository
            .Setup(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("database down"));

        Result<TripItemDto, Exception> result = await harness.Mediator.Send(
            new ChangeTripItemAmountHandler.Command(TripsHarness.Family, trip.Id, jackets.Id, 2, null, "PerChild"), CancellationToken);

        result.Error.ShouldBeOfType<InvalidOperationException>();
    }
}
