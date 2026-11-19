using MediatR;
using Shared.Validation;
using Trips.Application.Contracts;
using Trips.Domain;
using Result = CSharpFunctionalExtensions.Result<Trips.Application.Contracts.TripPlanChangedDto, System.Exception>;

namespace Trips.Application.Trips;

/// <summary>
/// Changes who comes along and when on one trip, in one step; returns the whole trip because many totals can change at once.
/// </summary>
public sealed class ChangeTripPlanHandler(ITripRepository repository)
    : IRequestHandler<ChangeTripPlanHandler.Command, Result>
{
    public sealed record Command(Guid FamilyId, Guid TripId, int Adults, int Children, DateOnly? StartDate, DateOnly? EndDate)
        : IRequest<Result>;

    public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Trip? trip = await repository.FindAsync(request.TripId, request.FamilyId, cancellationToken);

        if (trip is null)
            return NotFoundException.For("Uitje", request.TripId);

        int changedTotals = trip.ChangePlan(
            TripComposition.Create(request.Adults, request.Children),
            TripDates.Create(request.StartDate, request.EndDate));
        await repository.SaveChangesAsync(cancellationToken);

        return new TripPlanChangedDto(TripDetailsDto.From(trip), changedTotals);
    }
}
