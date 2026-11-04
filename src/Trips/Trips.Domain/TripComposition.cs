using Shared.Validation;

namespace Trips.Domain;

/// <summary>
/// How many adults and children go on a trip. It starts as a copy of the family's composition and may differ per trip;
/// quantities per person, adult or child are counted against it.
/// </summary>
public sealed record TripComposition
{
    public const int MaxCount = 20;

    private TripComposition(int adults, int children)
    {
        Adults = adults;
        Children = children;
    }

    public int Adults { get; }

    public int Children { get; }

    /// <summary>
    /// The composition of a trip whose family's size is unknown. A new instance every time, because EF tracks an owned
    /// value per owner and cannot attach one shared instance to two trips.
    /// </summary>
    public static TripComposition Default => new(2, 0);

    public static TripComposition Create(int adults, int children)
    {
        Guard.AgainstOutOfRange(adults, 0, MaxCount, "Aantal volwassenen");
        Guard.AgainstOutOfRange(children, 0, MaxCount, "Aantal kinderen");

        if (adults + children is 0)
            throw new DomainValidationException("Een uitje heeft minstens één persoon.");

        return new TripComposition(adults, children);
    }

    /// <summary>The number of heads a quantity with this basis is counted for.</summary>
    public int Multiplier(QuantityBasis basis) => basis switch
    {
        QuantityBasis.Fixed => 1,
        QuantityBasis.PerPerson => Adults + Children,
        QuantityBasis.PerAdult => Adults,
        QuantityBasis.PerChild => Children,
        _ => throw new ArgumentOutOfRangeException(nameof(basis), basis, null),
    };
}
