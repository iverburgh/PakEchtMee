namespace Trips.Domain;

/// <summary>A catalog item as it looked when the trip list was generated.</summary>
public sealed record CatalogItemSnapshot(Guid SourceItemId, string Name, int? Quantity, string? Unit);

/// <summary>A category the user picked for a trip, together with the items to copy from it.</summary>
public sealed record CategorySelection(Guid CategoryId, string Name, IReadOnlyList<CatalogItemSnapshot> Items);
