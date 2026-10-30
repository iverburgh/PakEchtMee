using Catalog.Application.Items;
using MediatR;
using Web.Access;

namespace Web.Endpoints;

internal static class CatalogItemEndpoints
{
    public static RouteGroupBuilder MapCatalogItemEndpoints(this IEndpointRouteBuilder routes)
    {
        RouteGroupBuilder group = routes.MapGroup("/api/items").WithTags("Items");

        group.MapGet("/", async (Guid categoryId, bool? includeDeleted, CurrentUser user, ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new ListCatalogItemsHandler.Query(user.CatalogActor, categoryId, includeDeleted ?? false), cancellationToken)).ToHttpResult())
            .WithName("ListCatalogItems");

        group.MapPost("/", async (CreateItemRequest request, CurrentUser user, ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(
                new CreateCatalogItemHandler.Command(user.CatalogActor, request.CategoryId, request.Name, request.Quantity, request.Unit),
                cancellationToken))
                .ToHttpResult(item => Results.Created($"/api/items/{item.Id}", item)))
            .WithName("CreateCatalogItem");

        group.MapPut("/{id:guid}", async (Guid id, UpdateItemRequest request, CurrentUser user, ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(
                new UpdateCatalogItemHandler.Command(user.CatalogActor, id, request.Name, request.Quantity, request.Unit),
                cancellationToken)).ToHttpResult())
            .WithName("UpdateCatalogItem");

        group.MapPost("/{id:guid}/move", async (Guid id, MoveItemRequest request, CurrentUser user, ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new MoveCatalogItemHandler.Command(user.CatalogActor, id, request.TargetCategoryId), cancellationToken)).ToHttpResult())
            .WithName("MoveCatalogItem");

        group.MapDelete("/{id:guid}", async (Guid id, CurrentUser user, ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new RemoveCatalogItemHandler.Command(user.CatalogActor, id), cancellationToken)).ToHttpResult())
            .WithName("RemoveCatalogItem");

        group.MapPost("/{id:guid}/restore", async (Guid id, CurrentUser user, ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new RestoreCatalogItemHandler.Command(user.CatalogActor, id), cancellationToken)).ToHttpResult())
            .WithName("RestoreCatalogItem");

        return group;
    }

    internal sealed record CreateItemRequest(Guid CategoryId, string? Name, int? Quantity, string? Unit);

    internal sealed record UpdateItemRequest(string? Name, int? Quantity, string? Unit);

    internal sealed record MoveItemRequest(Guid TargetCategoryId);
}
