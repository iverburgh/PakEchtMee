using CSharpFunctionalExtensions;
using Families.Application.Contracts;
using Families.Application.Members;
using MediatR;
using Microsoft.Extensions.Options;

namespace Web.Access;

/// <summary>
/// Resolves the caller's administrator status and family on every request. Looking the membership up each time
/// is what makes removing a member effective on their very next request, instead of when a session expires.
/// </summary>
internal sealed class CurrentUserMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, CurrentUser currentUser, ISender sender, IOptions<AccessOptions> access)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(currentUser);
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(access);

        string? email = context.User.Identity?.IsAuthenticated == true
            ? context.User.FindFirst(ApiToken.EmailClaim)?.Value?.Trim().ToLowerInvariant()
            : null;

        if (!string.IsNullOrEmpty(email))
        {
            Result<Maybe<MembershipDto>, Exception> membership =
                await sender.Send(new GetMembershipHandler.Query(email), context.RequestAborted);

            // A failed lookup must not silently downgrade to "no family": surface it as a server error.
            if (membership.IsFailure)
                throw new InvalidOperationException("Looking up the caller's family failed.", membership.Error);

            MembershipDto? family = membership.Value.GetValueOrDefault();
            currentUser.Set(email, access.Value.IsAdministrator(email), family?.FamilyId, family?.FamilyName);
        }

        await next(context);
    }
}
