using Shared.Validation;

namespace Catalog.Domain.Items;

/// <summary>How often a quantity is needed during a trip: once, or for every day or every night of it.</summary>
public enum QuantityPeriod
{
    PerTrip,
    PerDay,
    PerNight,
}

public static class QuantityPeriods
{
    /// <summary>Parses the API value; a missing period means once per trip, so clients that do not know about periods keep working.</summary>
    public static QuantityPeriod Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return QuantityPeriod.PerTrip;

        if (Enum.TryParse(value.Trim(), ignoreCase: true, out QuantityPeriod period) && Enum.IsDefined(period))
            return period;

        throw new DomainValidationException($"'{value}' is geen geldige periode voor een aantal.");
    }

    /// <summary>The Dutch suffix shown after a quantity, such as "per dag"; empty for once per trip.</summary>
    public static string Suffix(this QuantityPeriod period) => period switch
    {
        QuantityPeriod.PerTrip => string.Empty,
        QuantityPeriod.PerDay => "per dag",
        QuantityPeriod.PerNight => "per nacht",
        _ => throw new ArgumentOutOfRangeException(nameof(period), period, null),
    };
}
