using System.Globalization;
using Shared.Validation;

namespace Catalog.Domain.Items;

/// <summary>
/// An optional quantity rule on an item: a quantity, a unit that only exists alongside a quantity, and the basis the
/// quantity is counted against (fixed, or per person, adult or child).
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

    /// <summary>Returns <c>null</c> when no quantity is given, which is how an item without an amount is represented.</summary>
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

    /// <summary>Renders the rule in Dutch, such as "7 paar", "1 per kind" or "2 liter per persoon".</summary>
    public override string ToString()
    {
        string amount = Unit is null
            ? Quantity.ToString(CultureInfo.InvariantCulture)
            : $"{Quantity.ToString(CultureInfo.InvariantCulture)} {Unit}";

        return Basis is QuantityBasis.Fixed ? amount : $"{amount} {Basis.Suffix()}";
    }
}
