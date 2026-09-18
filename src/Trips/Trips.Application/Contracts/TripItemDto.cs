using Trips.Domain;

namespace Trips.Application.Contracts;

/// <summary>
/// A trip item as the client sees it. <c>Deleted</c> is projected here as a status of its own, while the
/// allowed transitions are supplied by the server so the client never computes the flow itself.
/// </summary>
public sealed record TripItemDto(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string Name,
    int? Quantity,
    string? Unit,
    string Status,
    string? NextStatus,
    string? PreviousStatus,
    bool IsDeleted)
{
    public const string DeletedStatus = "Deleted";

    public static TripItemDto From(TripItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        PackingStatusTransition transition = item.Transition;

        return new TripItemDto(
            item.Id,
            item.SourceCategoryId,
            item.CategoryName,
            item.Name,
            item.Amount?.Quantity,
            item.Amount?.Unit,
            item.IsDeleted ? DeletedStatus : item.Status.ToString(),
            item.IsDeleted ? null : transition.Next?.ToString(),
            item.IsDeleted ? null : transition.Previous?.ToString(),
            item.IsDeleted);
    }
}
