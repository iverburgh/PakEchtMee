using Catalog.Application.Categories;
using MediatR;
using Web.Access;

namespace Web.Endpoints;

internal static class CategoryEndpoints
{
    public static RouteGroupBuilder MapCategoryEndpoints(this IEndpointRouteBuilder routes)
    {
        RouteGroupBuilder group = routes.MapGroup("/api/categories").WithTags("Categories");

        group.MapGet("/", async (bool? includeDeleted, bool? includeSwitchedOff, CurrentUser user, ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(
                new ListCategoriesHandler.Query(user.CatalogActor, includeDeleted ?? false, includeSwitchedOff ?? false),
                cancellationToken)).ToHttpResult())
            .WithName("ListCategories");

        group.MapGet("/general", async (bool? includeDeleted, CurrentUser user, ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new ListGeneralCategoriesHandler.Query(user.CatalogActor, includeDeleted ?? false), cancellationToken)).ToHttpResult())
            .WithName("ListGeneralCategories");

        group.MapPost("/", async (CreateCategoryRequest request, CurrentUser user, ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new CreateCategoryHandler.Command(user.CatalogActor, request.Name, request.General), cancellationToken))
                .ToHttpResult(category => Results.Created($"/api/categories/{category.Id}", category)))
            .WithName("CreateCategory");

        group.MapPut("/{id:guid}", async (Guid id, RenameCategoryRequest request, CurrentUser user, ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new RenameCategoryHandler.Command(user.CatalogActor, id, request.Name), cancellationToken)).ToHttpResult())
            .WithName("RenameCategory");

        group.MapPut("/{id:guid}/switch", async (Guid id, SwitchCategoryRequest request, CurrentUser user, ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new SwitchGeneralCategoryHandler.Command(user.CatalogActor, id, request.On), cancellationToken)).ToHttpResult())
            .WithName("SwitchGeneralCategory");

        group.MapDelete("/{id:guid}", async (Guid id, CurrentUser user, ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new RemoveCategoryHandler.Command(user.CatalogActor, id), cancellationToken)).ToHttpResult())
            .WithName("RemoveCategory");

        group.MapPost("/{id:guid}/restore", async (Guid id, CurrentUser user, ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new RestoreCategoryHandler.Command(user.CatalogActor, id), cancellationToken)).ToHttpResult())
            .WithName("RestoreCategory");

        return group;
    }

    internal sealed record CreateCategoryRequest(string? Name, bool General = false);

    internal sealed record RenameCategoryRequest(string? Name);

    internal sealed record SwitchCategoryRequest(bool On);
}
