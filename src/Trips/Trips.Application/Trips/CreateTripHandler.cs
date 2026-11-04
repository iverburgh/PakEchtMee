using MediatR;
using Trips.Application.Contracts;
using Trips.Domain;
using Result = CSharpFunctionalExtensions.Result<Trips.Application.Contracts.TripSummaryDto, System.Exception>;

namespace Trips.Application.Trips;

public sealed class CreateTripHandler(ITripRepository repository)
    : IRequestHandler<CreateTripHandler.Command, Result>
{
    /// <summary>The composition is the family's, passed in by the caller, so this context never reads the family itself.</summary>
    public sealed record Command(Guid FamilyId, string? Name, DateOnly? StartDate, DateOnly? EndDate, int Adults, int Children)
        : IRequest<Result>;

    public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Trip trip = Trip.Create(
            request.FamilyId,
            TripName.Create(request.Name),
            TripDates.Create(request.StartDate, request.EndDate),
            TripComposition.Create(request.Adults, request.Children));

        repository.Add(trip);
        await repository.SaveChangesAsync(cancellationToken);

        return TripSummaryDto.From(trip);
    }
}
