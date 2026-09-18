using System.Diagnostics;

namespace Shared.Application;

/// <summary>Single <see cref="ActivitySource"/> for the application layer; generic behaviours must not own one per closed type.</summary>
public static class ApplicationTelemetry
{
    public const string ActivitySourceName = "PakEchtMee.Application";

    public static ActivitySource ActivitySource { get; } = new(ActivitySourceName);
}
