using System.Globalization;
using Shared.Validation;

namespace Trips.Domain;

/// <summary>
/// The quantity rule copied onto a trip item: a quantity, an optional unit and the basis it is counted against. Kept
/// separate from the catalog's own type so the contexts stay independent.
/// </summary>
public sealed record Amount
{
    public const int MinQuantity = 1;
    public const int MaxQuantity = 9999;
    public const int MaxUnitLength = 20;

    private Amount(int quantity, string? unit, QuantityBasis basis)
    {
        Quantity = quantity;
        Unit = unit;
        Basis = basis;
    }

    public int Quantity { get; }

    public string? Unit { get; }

    public QuantityBasis Basis { get; }

    public static Amount? Create(int? quantity, string? unit, QuantityBasis basis = QuantityBasis.Fixed)
    {
        string? normalizedUnit = Guard.OptionalText(unit, MaxUnitLength, "Eenheid");

        if (!Enum.IsDefined(basis))
            throw new DomainValidationException($"'{basis}' is geen geldige basis voor een aantal.");

        if (quantity is null)
        {
            if (normalizedUnit is not null)
                throw new DomainValidationException("Een eenheid kan alleen samen met een aantal.");

            if (basis is not QuantityBasis.Fixed)
                throw new DomainValidationException("Een aantal per persoon, volwassene of kind heeft een aantal nodig.");

            return null;
        }

        return new Amount(Guard.AgainstOutOfRange(quantity.Value, MinQuantity, MaxQuantity, "Aantal"), normalizedUnit, basis);
    }

    /// <summary>How many to pack on a trip of this composition: the quantity times the number of heads it counts.</summary>
    public int TotalFor(TripComposition composition)
    {
        ArgumentNullException.ThrowIfNull(composition);

        return Quantity * composition.Multiplier(Basis);
    }

    public override string ToString()
        => Unit is null
            ? Quantity.ToString(CultureInfo.InvariantCulture)
            : $"{Quantity.ToString(CultureInfo.InvariantCulture)} {Unit}";
}
