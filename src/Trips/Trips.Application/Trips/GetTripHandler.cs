using MediatR;
using Shared.Validation;
using Trips.Application.Contracts;
using Trips.Domain;
using Result = CSharpFunctionalExtensions.Result<Trips.Application.Contracts.TripDetailsDto, System.Exception>;

namespace Trips.Application.Trips;

public sealed class GetTripHandler(ITripRepository repository)
    : IRequestHandler<GetTripHandler.Query, Result>
{
    public sealed record Query(Guid Id, TripItemFilter? Filter = null) : IRequest<Result>;

    public async Task<Result> Handle(Query request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Trip? trip = await repository.FindAsync(request.Id, cancellationToken);

        return trip is null
            ? NotFoundException.For("Uitje", request.Id)
            : TripDetailsDto.From(trip, request.Filter ?? TripItemFilter.None);
    }
}
