using Xunit.v3;

namespace Shared.Tests;

/// <summary>Marks a test that needs a container, so unit tests remain runnable without a container runtime.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class IntegrationTestAttribute : Attribute, ITraitAttribute
{
    public const string TraitName = "Category";
    public const string TraitValue = "Integration";

    public IReadOnlyCollection<KeyValuePair<string, string>> GetTraits() => [new(TraitName, TraitValue)];
}
