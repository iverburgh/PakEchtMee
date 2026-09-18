using Catalog.Application.Categories;
using MediatR;

namespace Web.Endpoints;

internal static class CategoryEndpoints
{
    public static RouteGroupBuilder MapCategoryEndpoints(this IEndpointRouteBuilder routes)
    {
        RouteGroupBuilder group = routes.MapGroup("/api/categories").WithTags("Categories");

        group.MapGet("/", async (bool? includeDeleted, ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new ListCategoriesHandler.Query(includeDeleted ?? false), cancellationToken)).ToHttpResult())
            .WithName("ListCategories");

        group.MapPost("/", async (CreateCategoryRequest request, ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new CreateCategoryHandler.Command(request.Name), cancellationToken))
                .ToHttpResult(category => Results.Created($"/api/categories/{category.Id}", category)))
            .WithName("CreateCategory");

        group.MapPut("/{id:guid}", async (Guid id, RenameCategoryRequest request, ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new RenameCategoryHandler.Command(id, request.Name), cancellationToken)).ToHttpResult())
            .WithName("RenameCategory");

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new RemoveCategoryHandler.Command(id), cancellationToken)).ToHttpResult())
            .WithName("RemoveCategory");

        group.MapPost("/{id:guid}/restore", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new RestoreCategoryHandler.Command(id), cancellationToken)).ToHttpResult())
            .WithName("RestoreCategory");

        return group;
    }

    internal sealed record CreateCategoryRequest(string? Name);

    internal sealed record RenameCategoryRequest(string? Name);
}
