using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Web.Access;

/// <summary>
/// The contract for the short-lived token the Next.js server signs for every API call (HS256, subject = e-mail).
/// The frontend's <c>lib/api-token.ts</c> must use the same issuer, audience and key.
/// </summary>
internal static class ApiToken
{
    public const string Issuer = "pakechtmee-web";
    public const string Audience = "pakechtmee-api";
    public const string KeySetting = "Auth:TokenKey";
    public const string EmailClaim = "sub";

    /// <summary>HS256 needs at least 256 bits of key material.</summary>
    private const int MinimumKeyLength = 32;

    public static SymmetricSecurityKey SigningKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length < MinimumKeyLength)
            throw new InvalidOperationException($"'{KeySetting}' must be configured with at least {MinimumKeyLength} characters.");

        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
    }
}
