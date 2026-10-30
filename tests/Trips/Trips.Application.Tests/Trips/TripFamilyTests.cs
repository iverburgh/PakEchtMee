using CSharpFunctionalExtensions;
using Moq;
using Shared.Validation;
using Shouldly;
using Trips.Application.Contracts;
using Trips.Application.Items;
using Trips.Application.Trips;
using Trips.Domain;
using Xunit;

namespace Trips.Application.Tests.Trips;

/// <summary>Every trip handler works inside the caller's family; a trip of another family behaves as if it did not exist.</summary>
public sealed class TripFamilyTests
{
    private readonly TripsHarness harness = new();

    private static readonly Guid OtherFamily = Guid.CreateVersion7();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_new_trip_belongs_to_the_callers_family()
    {
        await harness.Mediator.Send(new CreateTripHandler.Command(TripsHarness.Family, "Kust", null, null), CancellationToken);

        harness.Repository.Verify(repository => repository.Add(It.Is<Trip>(trip => trip.FamilyId == TripsHarness.Family)), Times.Once);
    }

    [Fact]
    public async Task Listing_asks_only_for_the_callers_family()
    {
        harness.Repository.Setup(repository => repository.ListAsync(TripsHarness.Family, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        Result<IReadOnlyList<TripSummaryDto>, Exception> result =
            await harness.Mediator.Send(new ListTripsHandler.Query(TripsHarness.Family), CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        harness.Repository.Verify(repository => repository.ListAsync(TripsHarness.Family, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Another_familys_trip_is_not_found()
    {
        Trip trip = harness.GivenTrip();

        Result<TripDetailsDto, Exception> result =
            await harness.Mediator.Send(new GetTripHandler.Query(OtherFamily, trip.Id), CancellationToken);

        result.Error.ShouldBeOfType<NotFoundException>();
    }

    [Fact]
    public async Task Another_family_cannot_advance_an_item()
    {
        Trip trip = harness.GivenTrip();
        trip.SelectCategories([TripsHarness.ACategory(Guid.CreateVersion7(), "Toiletries", 1)]);

        Result<TripItemDto, Exception> result =
            await harness.Mediator.Send(new AdvanceTripItemHandler.Command(OtherFamily, trip.Id, trip.Items[0].Id), CancellationToken);

        result.Error.ShouldBeOfType<NotFoundException>();
        trip.Items[0].Status.ShouldBe(PackingStatus.Active);
        harness.Repository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
