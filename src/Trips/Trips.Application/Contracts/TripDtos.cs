using Trips.Domain;

namespace Trips.Application.Contracts;

public sealed record TripCategoryDto(Guid CategoryId, string Name, bool HasProgressedItems);

public sealed record TripSummaryDto(Guid Id, string Name, DateOnly? StartDate, DateOnly? EndDate, TripProgressDto Progress)
{
    public static TripSummaryDto From(Trip trip)
    {
        ArgumentNullException.ThrowIfNull(trip);

        return new TripSummaryDto(trip.Id, trip.Name.Value, trip.Dates.Start, trip.Dates.End, TripProgressDto.From(trip.Progress()));
    }
}

public sealed record TripDetailsDto(
    Guid Id,
    string Name,
    DateOnly? StartDate,
    DateOnly? EndDate,
    IReadOnlyList<TripCategoryDto> Categories,
    IReadOnlyList<TripItemDto> Items,
    TripProgressDto Progress)
{
    public static TripDetailsDto From(Trip trip) => From(trip, TripItemFilter.None);

    /// <summary>The filter only narrows the item list; the categories and the progress always describe the whole trip.</summary>
    public static TripDetailsDto From(Trip trip, TripItemFilter filter)
    {
        ArgumentNullException.ThrowIfNull(trip);
        ArgumentNullException.ThrowIfNull(filter);

        return new TripDetailsDto(
            trip.Id,
            trip.Name.Value,
            trip.Dates.Start,
            trip.Dates.End,
            [.. trip.Categories.Select(category => new TripCategoryDto(category.CategoryId, category.Name, trip.HasProgressedItemsIn(category.CategoryId)))],
            [.. trip.Items.Select(TripItemDto.From).Where(filter.Matches)],
            TripProgressDto.From(trip.Progress()));
    }
}
