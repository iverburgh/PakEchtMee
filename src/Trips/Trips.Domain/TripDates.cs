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

    public static TripDates Create(DateOnly? start, DateOnly? end)
    {
        if (start is not null && end is not null && end < start)
            throw new DomainValidationException("De einddatum kan niet voor de startdatum liggen.");

        return new TripDates(start, end);
    }
}
