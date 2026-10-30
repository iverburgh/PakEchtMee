using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Trips.Application.Contracts;
using Trips.Application.Items;
using Trips.Application.Trips;
using Trips.Domain;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Web.Endpoints;

internal static class TripEndpoints
{
    public static RouteGroupBuilder MapTripEndpoints(this IEndpointRouteBuilder routes)
    {
        RouteGroupBuilder group = routes.MapGroup("/api/trips").WithTags("Trips");

        group.MapGet("/", async (ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new ListTripsHandler.Query(), cancellationToken)).ToHttpResult())
            .WithName("ListTrips");

        group.MapGet("/{tripId:guid}", async (
            Guid tripId,
            [FromQuery(Name = "categoryId")] Guid[]? categoryIds,
            [FromQuery(Name = "status")] string[]? statuses,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(
                new GetTripHandler.Query(tripId, new TripItemFilter(categoryIds ?? [], statuses ?? [])),
                cancellationToken)).ToHttpResult())
            .WithName("GetTrip");

        group.MapPost("/", async (CreateTripRequest request, ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new CreateTripHandler.Command(request.Name, request.StartDate, request.EndDate), cancellationToken))
                .ToHttpResult(trip => Results.Created($"/api/trips/{trip.Id}", trip)))
            .WithName("CreateTrip");

        group.MapPost("/{tripId:guid}/categories", SelectCategoriesAsync).WithName("SelectTripCategories");

        group.MapDelete("/{tripId:guid}/categories/{categoryId:guid}", async (Guid tripId, Guid categoryId, ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new RemoveTripCategoryHandler.Command(tripId, categoryId), cancellationToken)).ToHttpResult())
            .WithName("RemoveTripCategory");

        MapItemTransitions(group);

        return group;
    }

    private static void MapItemTransitions(RouteGroupBuilder group)
    {
        // Intent-based endpoints: the client never names a target status, so it cannot skip a step.
        group.MapPost("/{tripId:guid}/items/{itemId:guid}/advance", async (Guid tripId, Guid itemId, ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new AdvanceTripItemHandler.Command(tripId, itemId), cancellationToken)).ToHttpResult())
            .WithName("AdvanceTripItem");

        group.MapPost("/{tripId:guid}/items/{itemId:guid}/revert", async (Guid tripId, Guid itemId, ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new RevertTripItemHandler.Command(tripId, itemId), cancellationToken)).ToHttpResult())
            .WithName("RevertTripItem");

        group.MapPost("/{tripId:guid}/items/{itemId:guid}/delete", async (Guid tripId, Guid itemId, ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new DeleteTripItemHandler.Command(tripId, itemId), cancellationToken)).ToHttpResult())
            .WithName("DeleteTripItem");

        group.MapPost("/{tripId:guid}/items/{itemId:guid}/restore", async (Guid tripId, Guid itemId, ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new RestoreTripItemHandler.Command(tripId, itemId), cancellationToken)).ToHttpResult())
            .WithName("RestoreTripItem");
    }

    /// <summary>
    /// Reads the chosen categories and their items from the catalog and hands the snapshots to the trip,
    /// so the trips context never queries catalog tables itself.
    /// </summary>
    private static async Task<IResult> SelectCategoriesAsync(
        Guid tripId,
        SelectCategoriesRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<CategorySelection>, Exception> selections =
            await CategorySelections.ReadAsync(sender, request.CategoryIds, cancellationToken);

        if (selections.IsFailure)
            return selections.ToHttpResult();

        return (await sender.Send(new SelectTripCategoriesHandler.Command(tripId, selections.Value), cancellationToken)).ToHttpResult();
    }

    internal sealed record CreateTripRequest(string? Name, DateOnly? StartDate, DateOnly? EndDate);

    internal sealed record SelectCategoriesRequest(IReadOnlyList<Guid> CategoryIds);
}
