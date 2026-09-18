using MediatR;
using Shared.Validation;
using Trips.Application.Contracts;
using Trips.Domain;
using Result = CSharpFunctionalExtensions.Result<Trips.Application.Contracts.TripItemDto, System.Exception>;

namespace Trips.Application.Items;

public sealed class DeleteTripItemHandler(ITripRepository repository, TimeProvider timeProvider)
    : IRequestHandler<DeleteTripItemHandler.Command, Result>
{
    public sealed record Command(Guid TripId, Guid ItemId) : IRequest<Result>;

    public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Trip? trip = await repository.FindAsync(request.TripId, cancellationToken);

        if (trip is null)
            return NotFoundException.For("Uitje", request.TripId);

        TripItem item = trip.DeleteItem(request.ItemId, timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken);

        return TripItemDto.From(item);
    }
}
