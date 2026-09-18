using Trips.Domain;

namespace Trips.Application.Contracts;

public sealed record TripProgressDto(int Total, int Active, int Prepared, int Packed, int Loaded, double LoadedShare)
{
    public static TripProgressDto From(TripProgress progress)
    {
        ArgumentNullException.ThrowIfNull(progress);

        return new TripProgressDto(progress.Total, progress.Active, progress.Prepared, progress.Packed, progress.Loaded, progress.LoadedShare);
    }
}
