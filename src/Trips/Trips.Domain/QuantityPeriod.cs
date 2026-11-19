using Shared.Validation;

namespace Trips.Domain;

/// <summary>How often a quantity is needed during a trip. Mirrors the catalog's period so the contexts stay independent.</summary>
public enum QuantityPeriod
{
    PerTrip,
    PerDay,
    PerNight,
}

public static class QuantityPeriods
{
    /// <summary>Parses the API value; a missing period means once per trip.</summary>
    public static QuantityPeriod Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return QuantityPeriod.PerTrip;

        if (Enum.TryParse(value.Trim(), ignoreCase: true, out QuantityPeriod period) && Enum.IsDefined(period))
            return period;

        throw new DomainValidationException($"'{value}' is geen geldige periode voor een aantal.");
    }
}
