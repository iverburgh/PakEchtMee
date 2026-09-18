using Shared.Validation;

namespace Trips.Domain;

public sealed record TripName
{
    public const int MaxLength = 100;

    private TripName(string value) => Value = value;

    public string Value { get; }

    public static TripName Create(string? value) => new(Guard.RequiredText(value, MaxLength, "Naam van het uitje"));

    public override string ToString() => Value;
}
