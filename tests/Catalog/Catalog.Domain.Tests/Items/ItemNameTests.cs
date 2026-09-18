using Catalog.Domain.Items;
using Shared.Validation;
using Shouldly;
using Xunit;

namespace Catalog.Domain.Tests.Items;

public sealed class ItemNameTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejects_blank_names(string? value)
        => Should.Throw<DomainValidationException>(() => ItemName.Create(value));

    [Fact]
    public void Trims_the_name()
        => ItemName.Create("  Toothbrush  ").Value.ShouldBe("Toothbrush");

    [Fact]
    public void Accepts_the_maximum_length()
        => ItemName.Create(new string('a', ItemName.MaxLength)).Value.Length.ShouldBe(ItemName.MaxLength);

    [Fact]
    public void Rejects_one_character_too_many()
        => Should.Throw<DomainValidationException>(() => ItemName.Create(new string('a', ItemName.MaxLength + 1)));

    [Fact]
    public void Normalizes_to_lower_case_for_comparison()
        => ItemName.Create("Toothbrush").Normalized.ShouldBe(ItemName.Create("toothbrush").Normalized);
}
