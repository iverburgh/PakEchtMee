using System.Globalization;
using Shared.Validation;

namespace Trips.Domain;

/// <summary>
/// The quantity rule copied onto a trip item: a quantity, an optional unit, the basis it is counted against and how
/// often it is needed. Kept separate from the catalog's own type so the contexts stay independent.
/// </summary>
public sealed record Amount
{
    public const int MinQuantity = 1;
    public const int MaxQuantity = 9999;
    public const int MaxUnitLength = 20;

    private Amount(int quantity, string? unit, QuantityBasis basis, QuantityPeriod period)
    {
        Quantity = quantity;
        Unit = unit;
        Basis = basis;
        Period = period;
    }

    public int Quantity { get; }

    public string? Unit { get; }

    public QuantityBasis Basis { get; }

    public QuantityPeriod Period { get; }

    /// <summary>True when the total changes with who comes along or how long the trip lasts.</summary>
    public bool DependsOnPlan => Basis is not QuantityBasis.Fixed || Period is not QuantityPeriod.PerTrip;

    public static Amount? Create(
        int? quantity,
        string? unit,
        QuantityBasis basis = QuantityBasis.Fixed,
        QuantityPeriod period = QuantityPeriod.PerTrip)
    {
        string? normalizedUnit = Guard.OptionalText(unit, MaxUnitLength, "Eenheid");

        if (!Enum.IsDefined(basis))
            throw new DomainValidationException($"'{basis}' is geen geldige basis voor een aantal.");

        if (!Enum.IsDefined(period))
            throw new DomainValidationException($"'{period}' is geen geldige periode voor een aantal.");

        if (quantity is null)
        {
            if (normalizedUnit is not null)
                throw new DomainValidationException("Een eenheid kan alleen samen met een aantal.");

            if (basis is not QuantityBasis.Fixed)
                throw new DomainValidationException("Een aantal per persoon, volwassene of kind heeft een aantal nodig.");

            if (period is not QuantityPeriod.PerTrip)
                throw new DomainValidationException("Een aantal per dag of per nacht heeft een aantal nodig.");

            return null;
        }

        return new Amount(Guard.AgainstOutOfRange(quantity.Value, MinQuantity, MaxQuantity, "Aantal"), normalizedUnit, basis, period);
    }

    /// <summary>
    /// How many to pack on a trip: the quantity times the number of heads it counts, times the days or nights it is
    /// needed for.
    /// </summary>
    public int TotalFor(TripComposition composition, TripDates dates)
    {
        ArgumentNullException.ThrowIfNull(composition);
        ArgumentNullException.ThrowIfNull(dates);

        return Quantity * composition.Multiplier(Basis) * dates.Multiplier(Period);
    }

    public override string ToString()
        => Unit is null
            ? Quantity.ToString(CultureInfo.InvariantCulture)
            : $"{Quantity.ToString(CultureInfo.InvariantCulture)} {Unit}";
}
