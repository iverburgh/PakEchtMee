using Shared.Validation;

namespace Catalog.Domain.Items;

/// <summary>The name of a catalog item, trimmed and bounded; uniqueness within a category is enforced by the application layer.</summary>
public sealed record ItemName
{
    public const int MaxLength = 100;

    private ItemName(string value) => Value = value;

    public string Value { get; }

    /// <summary>Lower-cased form used to compare names case-insensitively.</summary>
    public string Normalized => Value.ToLowerInvariant();

    public static ItemName Create(string? value)
        => new(Guard.RequiredText(value, MaxLength, "Itemnaam"));

    public override string ToString() => Value;
}
