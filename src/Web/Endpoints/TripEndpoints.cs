using CSharpFunctionalExtensions;
using Families.Application.Composition;
using Families.Application.Contracts;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Trips.Application.Contracts;
using Trips.Application.Items;
using Trips.Application.Trips;
using Trips.Domain;
using Web.Access;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Web.Endpoints;

internal static class TripEndpoints
{
    public static RouteGroupBuilder MapTripEndpoints(this IEndpointRouteBuilder routes)
    {
        RouteGroupBuilder group = routes.MapGroup("/api/trips").WithTags("Trips");

        group.MapGet("/", async (CurrentUser user, ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new ListTripsHandler.Query(user.FamilyId!.Value), cancellationToken)).ToHttpResult())
            .WithName("ListTrips");

        group.MapGet("/{tripId:guid}", async (
            Guid tripId,
            [FromQuery(Name = "categoryId")] Guid[]? categoryIds,
            [FromQuery(Name = "status")] string[]? statuses,
            CurrentUser user,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(
                new GetTripHandler.Query(user.FamilyId!.Value, tripId, new TripItemFilter(categoryIds ?? [], statuses ?? [])),
                cancellationToken)).ToHttpResult())
            .WithName("GetTrip");

        group.MapPost("/", CreateTripAsync).WithName("CreateTrip");

        group.MapPut("/{tripId:guid}/composition", async (
            Guid tripId,
            FamilyEndpoints.CompositionRequest request,
            CurrentUser user,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(
                new ChangeTripCompositionHandler.Command(user.FamilyId!.Value, tripId, request.Adults, request.Children),
                cancellationToken)).ToHttpResult())
            .WithName("ChangeTripComposition");

        group.MapPost("/{tripId:guid}/categories", SelectCategoriesAsync).WithName("SelectTripCategories");

        group.MapDelete("/{tripId:guid}/categories/{categoryId:guid}", async (Guid tripId, Guid categoryId, CurrentUser user, ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new RemoveTripCategoryHandler.Command(user.FamilyId!.Value, tripId, categoryId), cancellationToken)).ToHttpResult())
            .WithName("RemoveTripCategory");

        MapItemTransitions(group);

        return group;
    }

    private static void MapItemTransitions(RouteGroupBuilder group)
    {
        // Intent-based endpoints: the client never names a target status, so it cannot skip a step.
        group.MapPost("/{tripId:guid}/items/{itemId:guid}/advance", async (Guid tripId, Guid itemId, CurrentUser user, ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new AdvanceTripItemHandler.Command(user.FamilyId!.Value, tripId, itemId), cancellationToken)).ToHttpResult())
            .WithName("AdvanceTripItem");

        group.MapPost("/{tripId:guid}/items/{itemId:guid}/revert", async (Guid tripId, Guid itemId, CurrentUser user, ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new RevertTripItemHandler.Command(user.FamilyId!.Value, tripId, itemId), cancellationToken)).ToHttpResult())
            .WithName("RevertTripItem");

        group.MapPost("/{tripId:guid}/items/{itemId:guid}/delete", async (Guid tripId, Guid itemId, CurrentUser user, ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new DeleteTripItemHandler.Command(user.FamilyId!.Value, tripId, itemId), cancellationToken)).ToHttpResult())
            .WithName("DeleteTripItem");

        group.MapPost("/{tripId:guid}/items/{itemId:guid}/restore", async (Guid tripId, Guid itemId, CurrentUser user, ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new RestoreTripItemHandler.Command(user.FamilyId!.Value, tripId, itemId), cancellationToken)).ToHttpResult())
            .WithName("RestoreTripItem");

        group.MapPut("/{tripId:guid}/items/{itemId:guid}/quantity", async (
            Guid tripId,
            Guid itemId,
            QuantityRequest request,
            CurrentUser user,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(
                new ChangeTripItemAmountHandler.Command(user.FamilyId!.Value, tripId, itemId, request.Quantity, request.Unit, request.Basis),
                cancellationToken)).ToHttpResult())
            .WithName("ChangeTripItemQuantity");
    }

    /// <summary>
    /// A new trip takes over the family's composition. The server reads it rather than trusting the client, and passes it
    /// in, so the trips context never reads the family itself.
    /// </summary>
    private static async Task<IResult> CreateTripAsync(
        CreateTripRequest request,
        CurrentUser user,
        ISender sender,
        CancellationToken cancellationToken)
    {
        Guid familyId = user.FamilyId!.Value;
        Result<FamilyCompositionDto, Exception> composition =
            await sender.Send(new GetFamilyCompositionHandler.Query(familyId), cancellationToken);

        if (composition.IsFailure)
            return composition.ToHttpResult();

        return (await sender.Send(
                new CreateTripHandler.Command(
                    familyId,
                    request.Name,
                    request.StartDate,
                    request.EndDate,
                    composition.Value.Adults,
                    composition.Value.Children),
                cancellationToken))
            .ToHttpResult(trip => Results.Created($"/api/trips/{trip.Id}", trip));
    }

    /// <summary>
    /// Reads the chosen categories and their items from the catalog and hands the snapshots to the trip,
    /// so the trips context never queries catalog tables itself.
    /// </summary>
    private static async Task<IResult> SelectCategoriesAsync(
        Guid tripId,
        SelectCategoriesRequest request,
        CurrentUser user,
        ISender sender,
        CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<CategorySelection>, Exception> selections =
            await CategorySelections.ReadAsync(sender, user.CatalogActor, request.CategoryIds, cancellationToken);

        if (selections.IsFailure)
            return selections.ToHttpResult();

        return (await sender.Send(new SelectTripCategoriesHandler.Command(user.FamilyId!.Value, tripId, selections.Value), cancellationToken)).ToHttpResult();
    }

    internal sealed record CreateTripRequest(string? Name, DateOnly? StartDate, DateOnly? EndDate);

    internal sealed record SelectCategoriesRequest(IReadOnlyList<Guid> CategoryIds);

    internal sealed record QuantityRequest(int? Quantity, string? Unit, string? Basis);
}
