using Families.Domain;

namespace Families.Application.Contracts;

public sealed record FamilyCompositionDto(int Adults, int Children)
{
    public static FamilyCompositionDto From(FamilyComposition composition)
    {
        ArgumentNullException.ThrowIfNull(composition);

        return new FamilyCompositionDto(composition.Adults, composition.Children);
    }
}
