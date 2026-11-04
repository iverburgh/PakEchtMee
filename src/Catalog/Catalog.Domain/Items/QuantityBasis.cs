using Shared.Validation;

namespace Catalog.Domain.Items;

/// <summary>What a quantity is counted against: a fixed number, or a number for every person, adult or child on a trip.</summary>
public enum QuantityBasis
{
    Fixed,
    PerPerson,
    PerAdult,
    PerChild,
}

public static class QuantityBases
{
    /// <summary>Parses the API value; a missing basis means fixed, so clients that do not know about bases keep working.</summary>
    public static QuantityBasis Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return QuantityBasis.Fixed;

        if (Enum.TryParse(value.Trim(), ignoreCase: true, out QuantityBasis basis) && Enum.IsDefined(basis))
            return basis;

        throw new DomainValidationException($"'{value}' is geen geldige basis voor een aantal.");
    }

    /// <summary>The Dutch suffix shown after a quantity, such as "per kind"; empty for a fixed quantity.</summary>
    public static string Suffix(this QuantityBasis basis) => basis switch
    {
        QuantityBasis.Fixed => string.Empty,
        QuantityBasis.PerPerson => "per persoon",
        QuantityBasis.PerAdult => "per volwassene",
        QuantityBasis.PerChild => "per kind",
        _ => throw new ArgumentOutOfRangeException(nameof(basis), basis, null),
    };
}
