using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace Web.Access;

internal static class AccessServiceCollectionExtensions
{
    /// <summary>
    /// Every endpoint requires an authenticated caller unless it opts out explicitly (health checks), so a new endpoint
    /// can never be public by accident.
    /// </summary>
    public static IServiceCollection AddAccessControl(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<AccessOptions>().BindConfiguration(AccessOptions.SectionName);
        services.AddScoped<CurrentUser>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // The key is read when the options are first used, so tests that swap the scheme need no key at all.
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IConfiguration>((options, configuration) =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = ApiToken.Issuer,
                    ValidAudience = ApiToken.Audience,
                    IssuerSigningKey = ApiToken.SigningKey(configuration[ApiToken.KeySetting]),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    RequireExpirationTime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = ApiToken.EmailClaim,
                };
            });

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        return services;
    }
}
