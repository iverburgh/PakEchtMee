using Families.Application.Composition;
using MediatR;
using Web.Access;

namespace Web.Endpoints;

/// <summary>What a member may change about their own family; the group is restricted to family members in <c>Program.cs</c>.</summary>
internal static class FamilyEndpoints
{
    public static RouteGroupBuilder MapFamilyEndpoints(this IEndpointRouteBuilder routes)
    {
        RouteGroupBuilder group = routes.MapGroup("/api/family").WithTags("Family");

        group.MapGet("/composition", async (CurrentUser user, ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new GetFamilyCompositionHandler.Query(user.FamilyId!.Value), cancellationToken)).ToHttpResult())
            .WithName("GetFamilyComposition");

        group.MapPut("/composition", async (CompositionRequest request, CurrentUser user, ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(
                new ChangeFamilyCompositionHandler.Command(user.FamilyId!.Value, request.Adults, request.Children),
                cancellationToken)).ToHttpResult())
            .WithName("ChangeFamilyComposition");

        return group;
    }

    /// <summary>Shared by the family and the trip composition endpoints.</summary>
    internal sealed record CompositionRequest(int Adults, int Children);
}
