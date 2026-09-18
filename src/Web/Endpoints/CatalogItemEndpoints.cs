using Catalog.Application.Items;
using MediatR;

namespace Web.Endpoints;

internal static class CatalogItemEndpoints
{
    public static RouteGroupBuilder MapCatalogItemEndpoints(this IEndpointRouteBuilder routes)
    {
        RouteGroupBuilder group = routes.MapGroup("/api/items").WithTags("Items");

        group.MapGet("/", async (Guid categoryId, bool? includeDeleted, ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new ListCatalogItemsHandler.Query(categoryId, includeDeleted ?? false), cancellationToken)).ToHttpResult())
            .WithName("ListCatalogItems");

        group.MapPost("/", async (CreateItemRequest request, ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new CreateCatalogItemHandler.Command(request.CategoryId, request.Name, request.Quantity, request.Unit), cancellationToken))
                .ToHttpResult(item => Results.Created($"/api/items/{item.Id}", item)))
            .WithName("CreateCatalogItem");

        group.MapPut("/{id:guid}", async (Guid id, UpdateItemRequest request, ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new UpdateCatalogItemHandler.Command(id, request.Name, request.Quantity, request.Unit), cancellationToken)).ToHttpResult())
            .WithName("UpdateCatalogItem");

        group.MapPost("/{id:guid}/move", async (Guid id, MoveItemRequest request, ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new MoveCatalogItemHandler.Command(id, request.TargetCategoryId), cancellationToken)).ToHttpResult())
            .WithName("MoveCatalogItem");

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new RemoveCatalogItemHandler.Command(id), cancellationToken)).ToHttpResult())
            .WithName("RemoveCatalogItem");

        group.MapPost("/{id:guid}/restore", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new RestoreCatalogItemHandler.Command(id), cancellationToken)).ToHttpResult())
            .WithName("RestoreCatalogItem");

        return group;
    }

    internal sealed record CreateItemRequest(Guid CategoryId, string? Name, int? Quantity, string? Unit);

    internal sealed record UpdateItemRequest(string? Name, int? Quantity, string? Unit);

    internal sealed record MoveItemRequest(Guid TargetCategoryId);
}
