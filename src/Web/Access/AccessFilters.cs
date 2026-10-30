namespace Web.Access;

/// <summary>
/// Endpoint filters that state what a group of endpoints requires from the caller. Authentication itself is the
/// fallback policy; these add the PakEchtMee-specific rules on top.
/// </summary>
internal static class AccessFilters
{
    /// <summary>Administrators and family members only; any other signed-in Google account is refused.</summary>
    public static TBuilder RequireKnownUser<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
        => builder.AddEndpointFilter(async (context, next) =>
            Current(context).IsKnown
                ? await next(context)
                : Forbidden("Dit account heeft geen toegang tot PakEchtMee."));

    /// <summary>Requires a family, for everything that reads or changes a family's own data.</summary>
    public static TBuilder RequireMember<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
        => builder.AddEndpointFilter(async (context, next) =>
            Current(context) switch
            {
                { FamilyId: not null } => await next(context),
                { IsAdministrator: true } => Forbidden("Je bent geen lid van een familie; dit kan alleen een familielid."),
                _ => Forbidden("Dit account heeft geen toegang tot PakEchtMee."),
            });

    public static TBuilder RequireAdministrator<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
        => builder.AddEndpointFilter(async (context, next) =>
            Current(context).IsAdministrator
                ? await next(context)
                : Forbidden("Alleen de beheerder kan dit doen."));

    private static CurrentUser Current(EndpointFilterInvocationContext context)
        => context.HttpContext.RequestServices.GetRequiredService<CurrentUser>();

    private static IResult Forbidden(string detail)
        => Results.Problem(title: "Geen toegang", detail: detail, statusCode: StatusCodes.Status403Forbidden);
}
