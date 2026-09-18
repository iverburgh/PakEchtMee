using MediatR;
using Trips.Application.Contracts;
using Trips.Domain;
using Result = CSharpFunctionalExtensions.Result<System.Collections.Generic.IReadOnlyList<Trips.Application.Contracts.TripSummaryDto>, System.Exception>;

namespace Trips.Application.Trips;

/// <summary>Lists trips with the ones you are about to pack for first: ongoing, then upcoming, then undated, then past.</summary>
public sealed class ListTripsHandler(ITripRepository repository, TimeProvider timeProvider)
    : IRequestHandler<ListTripsHandler.Query, Result>
{
    public sealed record Query : IRequest<Result>;

    public async Task<Result> Handle(Query request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        IReadOnlyList<Trip> trips = await repository.ListAsync(cancellationToken);
        DateOnly today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        return trips
            .OrderBy(trip => Rank(trip, today))
            .ThenBy(trip => SortKey(trip, today))
            .ThenBy(trip => trip.Name.Value, StringComparer.OrdinalIgnoreCase)
            .Select(TripSummaryDto.From)
            .ToList();
    }

    private static int Rank(Trip trip, DateOnly today)
    {
        DateOnly? start = trip.Dates.Start;
        DateOnly? end = trip.Dates.End;

        if (end is not null && end < today)
            return 3;

        if (start is null && end is null)
            return 2;

        return start is not null && start > today ? 1 : 0;
    }

    /// <summary>Upcoming and ongoing trips sort by their nearest date; past trips sort most recent first.</summary>
    private static int SortKey(Trip trip, DateOnly today) => Rank(trip, today) switch
    {
        3 => -trip.Dates.End!.Value.DayNumber,
        2 => 0,
        _ => (trip.Dates.Start ?? trip.Dates.End ?? today).DayNumber,
    };
}
