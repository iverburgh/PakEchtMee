using Shared.Validation;

namespace Families.Domain;

/// <summary>
/// How many adults and children a family consists of. Packing quantities per person, adult or child are derived from it,
/// and a new trip starts from it.
/// </summary>
public sealed record FamilyComposition
{
    public const int MaxCount = 20;

    /// <summary>
    /// What a family starts with until its members say otherwise. A new instance every time, because EF tracks an owned
    /// value per owner and cannot attach one shared instance to two families.
    /// </summary>
    public static FamilyComposition Default => new(2, 0);

    private FamilyComposition(int adults, int children)
    {
        Adults = adults;
        Children = children;
    }

    public int Adults { get; }

    public int Children { get; }

    public static FamilyComposition Create(int adults, int children)
    {
        Guard.AgainstOutOfRange(adults, 0, MaxCount, "Aantal volwassenen");
        Guard.AgainstOutOfRange(children, 0, MaxCount, "Aantal kinderen");

        if (adults + children is 0)
            throw new DomainValidationException("Een gezin bestaat uit minstens één persoon.");

        return new FamilyComposition(adults, children);
    }
}
