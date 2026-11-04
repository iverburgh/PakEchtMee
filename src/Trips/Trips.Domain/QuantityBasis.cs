using Shared.Validation;

namespace Trips.Domain;

/// <summary>What a quantity is counted against. Mirrors the catalog's basis so the contexts stay independent.</summary>
public enum QuantityBasis
{
    Fixed,
    PerPerson,
    PerAdult,
    PerChild,
}

public static class QuantityBases
{
    /// <summary>Parses the API value; a missing basis means fixed.</summary>
    public static QuantityBasis Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return QuantityBasis.Fixed;

        if (Enum.TryParse(value.Trim(), ignoreCase: true, out QuantityBasis basis) && Enum.IsDefined(basis))
            return basis;

        throw new DomainValidationException($"'{value}' is geen geldige basis voor een aantal.");
    }
}
