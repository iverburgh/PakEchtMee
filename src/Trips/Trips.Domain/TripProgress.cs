namespace Trips.Domain;

/// <summary>How far a trip is, counting only items that are not deleted.</summary>
public sealed record TripProgress(int Total, int Active, int Prepared, int Packed, int Loaded)
{
    public double LoadedShare => Total is 0 ? 0d : (double)Loaded / Total;
}
