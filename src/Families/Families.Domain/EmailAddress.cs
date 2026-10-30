using System.Net.Mail;
using Shared.Validation;

namespace Families.Domain;

/// <summary>
/// The e-mail address of a Google account, stored lower-case so "IvoVerburgh@Gmail.com" and "ivoverburgh@gmail.com"
/// are the same member everywhere: in the database index, in membership lookups and in the administrator list.
/// </summary>
public sealed record EmailAddress
{
    public const int MaxLength = 254;

    private EmailAddress(string value) => Value = value;

    public string Value { get; }

    public static EmailAddress Create(string? value)
    {
        string trimmed = Guard.RequiredText(value, MaxLength, "E-mailadres");

        // MailAddress also accepts display names ("Ivo <ivo@x.nl>"); only a bare address with a dotted domain is allowed.
        if (!MailAddress.TryCreate(trimmed, out MailAddress? parsed)
            || !string.Equals(parsed.Address, trimmed, StringComparison.Ordinal)
            || !parsed.Host.Contains('.', StringComparison.Ordinal))
        {
            throw new DomainValidationException($"'{trimmed}' is geen geldig e-mailadres.");
        }

        return new EmailAddress(trimmed.ToLowerInvariant());
    }

    public override string ToString() => Value;
}
