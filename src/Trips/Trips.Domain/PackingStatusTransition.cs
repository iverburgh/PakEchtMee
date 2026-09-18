namespace Trips.Domain;

/// <summary>The single source of the packing flow: everything else derives the allowed steps from here.</summary>
public readonly record struct PackingStatusTransition(PackingStatus Status)
{
    public PackingStatus? Next => Status switch
    {
        PackingStatus.Active => PackingStatus.Prepared,
        PackingStatus.Prepared => PackingStatus.Packed,
        PackingStatus.Packed => PackingStatus.Loaded,
        PackingStatus.Loaded => null,
        _ => throw new ArgumentOutOfRangeException(nameof(Status), Status, "Unknown packing status."),
    };

    public PackingStatus? Previous => Status switch
    {
        PackingStatus.Active => null,
        PackingStatus.Prepared => PackingStatus.Active,
        PackingStatus.Packed => PackingStatus.Prepared,
        PackingStatus.Loaded => PackingStatus.Packed,
        _ => throw new ArgumentOutOfRangeException(nameof(Status), Status, "Unknown packing status."),
    };

    public bool CanAdvance => Next is not null;

    public bool CanRevert => Previous is not null;

    /// <summary>True when <paramref name="target"/> is exactly one step forward or backward.</summary>
    public bool IsSingleStepTo(PackingStatus target) => Next == target || Previous == target;
}
