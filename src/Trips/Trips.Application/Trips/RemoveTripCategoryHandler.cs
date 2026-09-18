using MediatR;
using Shared.Validation;
using Trips.Application.Contracts;
using Trips.Domain;
using Result = CSharpFunctionalExtensions.Result<Trips.Application.Contracts.TripDetailsDto, System.Exception>;

namespace Trips.Application.Trips;

public sealed class RemoveTripCategoryHandler(ITripRepository repository, TimeProvider timeProvider)
    : IRequestHandler<RemoveTripCategoryHandler.Command, Result>
{
    public sealed record Command(Guid TripId, Guid CategoryId) : IRequest<Result>;

    public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Trip? trip = await repository.FindAsync(request.TripId, cancellationToken);

        if (trip is null)
            return NotFoundException.For("Uitje", request.TripId);

        trip.RemoveCategory(request.CategoryId, timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken);

        return TripDetailsDto.From(trip);
    }
}
