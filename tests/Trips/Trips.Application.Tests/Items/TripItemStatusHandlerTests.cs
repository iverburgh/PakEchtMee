using CSharpFunctionalExtensions;
using Moq;
using Shared.Validation;
using Shouldly;
using Trips.Application.Contracts;
using Trips.Application.Items;
using Trips.Domain;
using Xunit;

namespace Trips.Application.Tests.Items;

public sealed class TripItemStatusHandlerTests
{
    private readonly TripsHarness harness = new();

    private Trip GivenTripWithOneItem()
    {
        Trip trip = harness.GivenTrip();
        trip.SelectCategories([TripsHarness.ACategory(Guid.CreateVersion7(), "Toiletries", 1)]);

        return trip;
    }

    [Fact]
    public async Task Advancing_walks_the_flow_and_reports_the_allowed_next_steps()
    {
        Trip trip = GivenTripWithOneItem();
        Guid itemId = trip.Items[0].Id;

        Result<TripItemDto, Exception> prepared = await harness.Mediator.Send(
            new AdvanceTripItemHandler.Command(trip.Id, itemId), TestContext.Current.CancellationToken);
        prepared.Value.Status.ShouldBe("Prepared");
        prepared.Value.NextStatus.ShouldBe("Packed");
        prepared.Value.PreviousStatus.ShouldBe("Active");

        await harness.Mediator.Send(new AdvanceTripItemHandler.Command(trip.Id, itemId), TestContext.Current.CancellationToken);
        Result<TripItemDto, Exception> loaded = await harness.Mediator.Send(
            new AdvanceTripItemHandler.Command(trip.Id, itemId), TestContext.Current.CancellationToken);

        loaded.Value.Status.ShouldBe("Loaded");
        loaded.Value.NextStatus.ShouldBeNull();
    }

    [Fact]
    public async Task Advancing_a_loaded_item_is_rejected()
    {
        Trip trip = GivenTripWithOneItem();
        Guid itemId = trip.Items[0].Id;
        trip.AdvanceItem(itemId);
        trip.AdvanceItem(itemId);
        trip.AdvanceItem(itemId);

        Result<TripItemDto, Exception> result = await harness.Mediator.Send(
            new AdvanceTripItemHandler.Command(trip.Id, itemId), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<DomainValidationException>();
        trip.Items[0].Status.ShouldBe(PackingStatus.Loaded);
    }

    [Fact]
    public async Task Reverting_moves_one_step_back()
    {
        Trip trip = GivenTripWithOneItem();
        Guid itemId = trip.Items[0].Id;
        trip.AdvanceItem(itemId);
        trip.AdvanceItem(itemId);

        Result<TripItemDto, Exception> result = await harness.Mediator.Send(
            new RevertTripItemHandler.Command(trip.Id, itemId), TestContext.Current.CancellationToken);

        result.Value.Status.ShouldBe("Prepared");
    }

    [Fact]
    public async Task Reverting_an_active_item_is_rejected()
    {
        Trip trip = GivenTripWithOneItem();

        Result<TripItemDto, Exception> result = await harness.Mediator.Send(
            new RevertTripItemHandler.Command(trip.Id, trip.Items[0].Id), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<DomainValidationException>();
    }

    [Fact]
    public async Task Deleting_and_restoring_keeps_the_status()
    {
        Trip trip = GivenTripWithOneItem();
        Guid itemId = trip.Items[0].Id;
        trip.AdvanceItem(itemId);
        trip.AdvanceItem(itemId);

        Result<TripItemDto, Exception> deleted = await harness.Mediator.Send(
            new DeleteTripItemHandler.Command(trip.Id, itemId), TestContext.Current.CancellationToken);
        deleted.Value.Status.ShouldBe(TripItemDto.DeletedStatus);
        deleted.Value.NextStatus.ShouldBeNull();

        Result<TripItemDto, Exception> restored = await harness.Mediator.Send(
            new RestoreTripItemHandler.Command(trip.Id, itemId), TestContext.Current.CancellationToken);
        restored.Value.Status.ShouldBe("Packed");
    }

    [Fact]
    public async Task An_unknown_item_reports_not_found()
    {
        Trip trip = GivenTripWithOneItem();

        Result<TripItemDto, Exception> result = await harness.Mediator.Send(
            new AdvanceTripItemHandler.Command(trip.Id, Guid.CreateVersion7()), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<NotFoundException>();
    }

    [Fact]
    public async Task An_unknown_trip_reports_not_found()
    {
        Result<TripItemDto, Exception> result = await harness.Mediator.Send(
            new AdvanceTripItemHandler.Command(Guid.CreateVersion7(), Guid.CreateVersion7()), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<NotFoundException>();
    }

    [Fact]
    public async Task A_failing_repository_becomes_a_failed_result()
    {
        Trip trip = GivenTripWithOneItem();
        harness.Repository
            .Setup(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("database unavailable"));

        Result<TripItemDto, Exception> result = await harness.Mediator.Send(
            new AdvanceTripItemHandler.Command(trip.Id, trip.Items[0].Id), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<InvalidOperationException>();
    }
}
