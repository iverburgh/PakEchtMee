using MediatR;
using Trips.Application.Contracts;
using Trips.Domain;
using Result = CSharpFunctionalExtensions.Result<Trips.Application.Contracts.TripSummaryDto, System.Exception>;

namespace Trips.Application.Trips;

public sealed class CreateTripHandler(ITripRepository repository)
    : IRequestHandler<CreateTripHandler.Command, Result>
{
    public sealed record Command(string? Name, DateOnly? StartDate, DateOnly? EndDate) : IRequest<Result>;

    public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Trip trip = Trip.Create(TripName.Create(request.Name), TripDates.Create(request.StartDate, request.EndDate));

        repository.Add(trip);
        await repository.SaveChangesAsync(cancellationToken);

        return TripSummaryDto.From(trip);
    }
}
