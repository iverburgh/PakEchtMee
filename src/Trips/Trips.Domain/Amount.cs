using System.Globalization;
using Shared.Validation;

namespace Trips.Domain;

/// <summary>The amount copied onto a trip item. Kept separate from the catalog's own type so the contexts stay independent.</summary>
public sealed record Amount
{
    public const int MinQuantity = 1;
    public const int MaxQuantity = 9999;
    public const int MaxUnitLength = 20;

    private Amount(int quantity, string? unit)
    {
        Quantity = quantity;
        Unit = unit;
    }

    public int Quantity { get; }

    public string? Unit { get; }

    public static Amount? Create(int? quantity, string? unit)
    {
        string? normalizedUnit = Guard.OptionalText(unit, MaxUnitLength, "Eenheid");

        if (quantity is null)
        {
            if (normalizedUnit is not null)
                throw new DomainValidationException("Een eenheid kan alleen samen met een aantal.");

            return null;
        }

        return new Amount(Guard.AgainstOutOfRange(quantity.Value, MinQuantity, MaxQuantity, "Aantal"), normalizedUnit);
    }

    public override string ToString()
        => Unit is null
            ? Quantity.ToString(CultureInfo.InvariantCulture)
            : $"{Quantity.ToString(CultureInfo.InvariantCulture)} {Unit}";
}
