using System.Globalization;
using Shared.Validation;

namespace Catalog.Domain.Items;

/// <summary>
/// An optional quantity rule on an item: a quantity, a unit that only exists alongside a quantity, the basis the
/// quantity is counted against (fixed, or per person, adult or child) and how often it is needed (once, per day or per night).
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

    /// <summary>Returns <c>null</c> when no quantity is given, which is how an item without an amount is represented.</summary>
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

    /// <summary>Renders the rule in Dutch, such as "7 paar", "1 per kind", "2 per nacht" or "2 liter per persoon per dag".</summary>
    public override string ToString()
    {
        string amount = Unit is null
            ? Quantity.ToString(CultureInfo.InvariantCulture)
            : $"{Quantity.ToString(CultureInfo.InvariantCulture)} {Unit}";

        return string.Join(' ', new[] { amount, Basis.Suffix(), Period.Suffix() }.Where(part => part.Length > 0));
    }
}
