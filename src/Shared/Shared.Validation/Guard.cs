namespace Shared.Validation;

/// <summary>Input guards shared by the domain value objects. Every failure throws <see cref="DomainValidationException"/>.</summary>
public static class Guard
{
    /// <summary>Requires non-blank text and returns it trimmed, so value objects never store padding.</summary>
    public static string AgainstNullOrWhiteSpace(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainValidationException($"{name} is verplicht.");

        return value.Trim();
    }

    public static string AgainstLengthAbove(string value, int maxLength, string name)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value.Length > maxLength)
            throw new DomainValidationException($"{name} mag maximaal {maxLength} tekens bevatten, maar bevat er {value.Length}.");

        return value;
    }

    /// <summary>Requires non-blank text within <paramref name="maxLength"/> and returns it trimmed.</summary>
    public static string RequiredText(string? value, int maxLength, string name)
    {
        string trimmed = AgainstNullOrWhiteSpace(value, name);

        return AgainstLengthAbove(trimmed, maxLength, name);
    }

    /// <summary>Allows blank text as absent, and otherwise requires it to fit within <paramref name="maxLength"/>.</summary>
    public static string? OptionalText(string? value, int maxLength, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return AgainstLengthAbove(value.Trim(), maxLength, name);
    }

    public static int AgainstOutOfRange(int value, int min, int max, string name)
    {
        if (value < min || value > max)
            throw new DomainValidationException($"{name} moet tussen {min} en {max} liggen, maar is {value}.");

        return value;
    }
}
