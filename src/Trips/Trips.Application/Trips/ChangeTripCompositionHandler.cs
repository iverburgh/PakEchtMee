using MediatR;
using Shared.Validation;
using Trips.Application.Contracts;
using Trips.Domain;
using Result = CSharpFunctionalExtensions.Result<Trips.Application.Contracts.TripCompositionChangedDto, System.Exception>;

namespace Trips.Application.Trips;

/// <summary>Changes who comes along on one trip; returns the whole trip because many totals can change at once.</summary>
public sealed class ChangeTripCompositionHandler(ITripRepository repository)
    : IRequestHandler<ChangeTripCompositionHandler.Command, Result>
{
    public sealed record Command(Guid FamilyId, Guid TripId, int Adults, int Children) : IRequest<Result>;

    public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Trip? trip = await repository.FindAsync(request.TripId, request.FamilyId, cancellationToken);

        if (trip is null)
            return NotFoundException.For("Uitje", request.TripId);

        int changedTotals = trip.ChangeComposition(TripComposition.Create(request.Adults, request.Children));
        await repository.SaveChangesAsync(cancellationToken);

        return new TripCompositionChangedDto(TripDetailsDto.From(trip), changedTotals);
    }
}
