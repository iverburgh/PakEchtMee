using Shared.Validation;
using Shouldly;
using Xunit;

namespace Families.Domain.Tests;

public sealed class FamilyCompositionTests
{
    [Fact]
    public void A_new_family_has_two_adults_and_no_children()
    {
        Family family = Family.Create(FamilyName.Create("Jansen"));

        family.Composition.Adults.ShouldBe(2);
        family.Composition.Children.ShouldBe(0);
    }

    [Fact]
    public void Changing_the_composition_replaces_it()
    {
        Family family = Family.Create(FamilyName.Create("Verburgh"));

        family.ChangeComposition(FamilyComposition.Create(2, 3));

        family.Composition.ShouldBe(FamilyComposition.Create(2, 3));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(20, 20)]
    public void A_composition_within_the_limits_is_accepted(int adults, int children)
    {
        FamilyComposition composition = FamilyComposition.Create(adults, children);

        composition.Adults.ShouldBe(adults);
        composition.Children.ShouldBe(children);
    }

    [Fact]
    public void A_family_of_nobody_is_rejected()
        => Should.Throw<DomainValidationException>(() => FamilyComposition.Create(0, 0));

    [Theory]
    [InlineData(-1, 2)]
    [InlineData(21, 2)]
    [InlineData(2, -1)]
    [InlineData(2, 21)]
    public void A_count_out_of_range_is_rejected(int adults, int children)
        => Should.Throw<DomainValidationException>(() => FamilyComposition.Create(adults, children));
}
