using Catalog.Domain.Items;
using Shared.Validation;
using Shouldly;
using Xunit;

namespace Catalog.Domain.Tests.Items;

public sealed class AmountTests
{
    [Fact]
    public void Without_a_quantity_there_is_no_amount()
        => Amount.Create(null, null).ShouldBeNull();

    [Fact]
    public void A_unit_without_a_quantity_is_rejected()
        => Should.Throw<DomainValidationException>(() => Amount.Create(null, "pair"));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(Amount.MaxQuantity + 1)]
    public void Rejects_a_quantity_outside_the_allowed_range(int quantity)
        => Should.Throw<DomainValidationException>(() => Amount.Create(quantity, null));

    [Fact]
    public void Rejects_an_over_long_unit()
        => Should.Throw<DomainValidationException>(() => Amount.Create(1, new string('a', Amount.MaxUnitLength + 1)));

    [Fact]
    public void Renders_a_quantity_with_a_unit()
        => Amount.Create(7, "pair")!.ToString().ShouldBe("7 pair");

    [Fact]
    public void Renders_a_quantity_without_a_unit()
        => Amount.Create(2, null)!.ToString().ShouldBe("2");

    [Fact]
    public void Treats_a_blank_unit_as_absent()
        => Amount.Create(2, "   ")!.Unit.ShouldBeNull();

    [Theory]
    [InlineData(Amount.MinQuantity)]
    [InlineData(Amount.MaxQuantity)]
    public void Accepts_the_quantity_boundaries(int quantity)
        => Amount.Create(quantity, null)!.Quantity.ShouldBe(quantity);

    [Fact]
    public void Compares_by_value()
        => Amount.Create(7, "pair").ShouldBe(Amount.Create(7, "pair"));
}
