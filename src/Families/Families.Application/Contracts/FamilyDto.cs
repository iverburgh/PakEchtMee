using Families.Domain;

namespace Families.Application.Contracts;

public sealed record FamilyDto(Guid Id, string Name, IReadOnlyList<string> Members)
{
    public static FamilyDto From(Family family)
    {
        ArgumentNullException.ThrowIfNull(family);

        return new FamilyDto(family.Id, family.Name.Value, [.. family.Members.Select(member => member.Email.Value).Order(StringComparer.Ordinal)]);
    }
}
