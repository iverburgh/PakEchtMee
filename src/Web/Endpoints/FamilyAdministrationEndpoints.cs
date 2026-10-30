using Families.Application.Families;
using Families.Application.Members;
using MediatR;

namespace Web.Endpoints;

/// <summary>Families and their members; the whole group is restricted to administrators in <c>Program.cs</c>.</summary>
internal static class FamilyAdministrationEndpoints
{
    public static RouteGroupBuilder MapFamilyAdministrationEndpoints(this IEndpointRouteBuilder routes)
    {
        RouteGroupBuilder group = routes.MapGroup("/api/admin/families").WithTags("Administration");

        group.MapGet("/", async (ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new ListFamiliesHandler.Query(), cancellationToken)).ToHttpResult())
            .WithName("ListFamilies");

        group.MapPost("/", async (FamilyNameRequest request, ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new CreateFamilyHandler.Command(request.Name), cancellationToken))
                .ToHttpResult(family => Results.Created($"/api/admin/families/{family.Id}", family)))
            .WithName("CreateFamily");

        group.MapPut("/{id:guid}", async (Guid id, FamilyNameRequest request, ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new RenameFamilyHandler.Command(id, request.Name), cancellationToken)).ToHttpResult())
            .WithName("RenameFamily");

        group.MapPost("/{id:guid}/members", async (Guid id, MemberRequest request, ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new AddMemberHandler.Command(id, request.Email), cancellationToken)).ToHttpResult())
            .WithName("AddMember");

        group.MapDelete("/{id:guid}/members/{email}", async (Guid id, string email, ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new RemoveMemberHandler.Command(id, email), cancellationToken)).ToHttpResult())
            .WithName("RemoveMember");

        return group;
    }

    internal sealed record FamilyNameRequest(string? Name);

    internal sealed record MemberRequest(string? Email);
}
