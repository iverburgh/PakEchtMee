using Shared.Validation;

namespace Catalog.Domain.Categories;

/// <summary>The name of a category, trimmed and bounded; uniqueness among non-removed categories is enforced by the application layer.</summary>
public sealed record CategoryName
{
    public const int MaxLength = 60;

    private CategoryName(string value) => Value = value;

    public string Value { get; }

    /// <summary>Lower-cased form used to compare names case-insensitively.</summary>
    public string Normalized => Value.ToLowerInvariant();

    public static CategoryName Create(string? value)
        => new(Guard.RequiredText(value, MaxLength, "Categorienaam"));

    public override string ToString() => Value;
}
