using Web.Access;

namespace Web.Endpoints;

internal static class MeEndpoints
{
    /// <summary>
    /// Who the caller is. The frontend asks this during sign-in to refuse unknown accounts, and on every page to decide
    /// which navigation to show.
    /// </summary>
    public static RouteHandlerBuilder MapMeEndpoints(this IEndpointRouteBuilder routes)
        => routes.MapGet("/api/me", (CurrentUser user) =>
                Results.Ok(new MeResponse(
                    user.Email!,
                    user.IsAdministrator,
                    user.FamilyId is { } familyId ? new FamilyResponse(familyId, user.FamilyName!) : null)))
            .RequireKnownUser()
            .WithTags("Access")
            .WithName("GetMe");

    internal sealed record MeResponse(string Email, bool IsAdministrator, FamilyResponse? Family);

    internal sealed record FamilyResponse(Guid Id, string Name);
}
