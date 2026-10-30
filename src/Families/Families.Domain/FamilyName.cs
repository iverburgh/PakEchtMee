using Shared.Validation;

namespace Families.Domain;

/// <summary>The name of a family, trimmed and bounded; uniqueness among families is enforced by the application layer.</summary>
public sealed record FamilyName
{
    public const int MaxLength = 60;

    private FamilyName(string value) => Value = value;

    public string Value { get; }

    public static FamilyName Create(string? value) => new(Guard.RequiredText(value, MaxLength, "Familienaam"));

    public override string ToString() => Value;
}
