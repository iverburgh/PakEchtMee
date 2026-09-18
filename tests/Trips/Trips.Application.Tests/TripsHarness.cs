using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Trips.Domain;

namespace Trips.Application.Tests;

internal sealed class TripsHarness
{
    public TripsHarness()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton(Repository.Object);
        services.AddTripsApplication();

        Services = services.BuildServiceProvider();
        Mediator = Services.GetRequiredService<IMediator>();
    }

    public Mock<ITripRepository> Repository { get; } = new();

    public ServiceProvider Services { get; }

    public IMediator Mediator { get; }

    public Trip GivenTrip(string name = "Summer France", DateOnly? start = null, DateOnly? end = null)
    {
        Trip trip = Trip.Create(TripName.Create(name), TripDates.Create(start, end));
        Repository.Setup(repository => repository.FindAsync(trip.Id, It.IsAny<CancellationToken>())).ReturnsAsync(trip);

        return trip;
    }

    public static CategorySelection ACategory(Guid categoryId, string name, int itemCount)
        => new(categoryId, name, [.. Enumerable.Range(1, itemCount).Select(index => new CatalogItemSnapshot(Guid.CreateVersion7(), $"{name} {index}", null, null))]);
}
