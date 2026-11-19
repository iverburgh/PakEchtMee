using Shared.Validation;

namespace Trips.Domain;

/// <summary>The optional start and end date of a trip; an end date never precedes a start date.</summary>
public sealed record TripDates
{
    public static readonly TripDates None = new(null, null);

    private TripDates(DateOnly? start, DateOnly? end)
    {
        Start = start;
        End = end;
    }

    public DateOnly? Start { get; }

    public DateOnly? End { get; }

    /// <summary>Every calendar day from start to end, both included; <c>null</c> until both dates are known.</summary>
    public int? Days => Start is { } start && End is { } end ? end.DayNumber - start.DayNumber + 1 : null;

    /// <summary>One night fewer than days, so a day trip has none; <c>null</c> until both dates are known.</summary>
    public int? Nights => Days - 1;

    /// <summary>
    /// How many times a quantity with this period is needed. Without dates a per-day or per-night rule counts once, so the
    /// total is still a sensible minimum; the client shows that the dates are missing.
    /// </summary>
    public int Multiplier(QuantityPeriod period) => period switch
    {
        QuantityPeriod.PerTrip => 1,
        QuantityPeriod.PerDay => Days ?? 1,
        QuantityPeriod.PerNight => Nights ?? 1,
        _ => throw new ArgumentOutOfRangeException(nameof(period), period, null),
    };

    public static TripDates Create(DateOnly? start, DateOnly? end)
    {
        if (start is not null && end is not null && end < start)
            throw new DomainValidationException("De einddatum kan niet voor de startdatum liggen.");

        return new TripDates(start, end);
    }
}
