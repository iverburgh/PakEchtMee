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

    [Fact]
    public void The_basis_defaults_to_fixed()
        => Amount.Create(2, null)!.Basis.ShouldBe(QuantityBasis.Fixed);

    [Fact]
    public void Renders_a_quantity_per_child()
        => Amount.Create(1, null, QuantityBasis.PerChild)!.ToString().ShouldBe("1 per kind");

    [Fact]
    public void Renders_a_quantity_with_a_unit_per_person()
        => Amount.Create(2, "liter", QuantityBasis.PerPerson)!.ToString().ShouldBe("2 liter per persoon");

    [Fact]
    public void Renders_a_quantity_per_adult()
        => Amount.Create(1, null, QuantityBasis.PerAdult)!.ToString().ShouldBe("1 per volwassene");

    [Fact]
    public void A_basis_without_a_quantity_is_rejected()
        => Should.Throw<DomainValidationException>(() => Amount.Create(null, null, QuantityBasis.PerAdult));

    [Fact]
    public void An_undefined_basis_is_rejected()
        => Should.Throw<DomainValidationException>(() => Amount.Create(1, null, (QuantityBasis)42));

    [Fact]
    public void Amounts_with_another_basis_differ()
        => Amount.Create(1, null, QuantityBasis.PerChild).ShouldNotBe(Amount.Create(1, null));

    [Theory]
    [InlineData(null, QuantityBasis.Fixed)]
    [InlineData("", QuantityBasis.Fixed)]
    [InlineData("PerChild", QuantityBasis.PerChild)]
    [InlineData("perperson", QuantityBasis.PerPerson)]
    [InlineData(" PerAdult ", QuantityBasis.PerAdult)]
    public void Parses_the_api_value(string? value, QuantityBasis expected)
        => QuantityBases.Parse(value).ShouldBe(expected);

    [Theory]
    [InlineData("PerDog")]
    [InlineData("7")]
    public void Rejects_an_unknown_api_value(string value)
        => Should.Throw<DomainValidationException>(() => QuantityBases.Parse(value));

    [Fact]
    public void The_period_defaults_to_once_per_trip()
        => Amount.Create(2, null)!.Period.ShouldBe(QuantityPeriod.PerTrip);

    [Fact]
    public void Renders_a_quantity_per_child_per_day()
        => Amount.Create(1, null, QuantityBasis.PerChild, QuantityPeriod.PerDay)!.ToString().ShouldBe("1 per kind per dag");

    [Fact]
    public void Renders_a_fixed_quantity_per_night()
        => Amount.Create(2, null, QuantityBasis.Fixed, QuantityPeriod.PerNight)!.ToString().ShouldBe("2 per nacht");

    [Fact]
    public void Renders_a_quantity_with_a_unit_per_person_per_day()
        => Amount.Create(2, "liter", QuantityBasis.PerPerson, QuantityPeriod.PerDay)!.ToString().ShouldBe("2 liter per persoon per dag");

    [Fact]
    public void A_period_without_a_quantity_is_rejected()
        => Should.Throw<DomainValidationException>(() => Amount.Create(null, null, QuantityBasis.Fixed, QuantityPeriod.PerDay));

    [Fact]
    public void An_undefined_period_is_rejected()
        => Should.Throw<DomainValidationException>(() => Amount.Create(1, null, QuantityBasis.Fixed, (QuantityPeriod)42));

    [Fact]
    public void Amounts_with_another_period_differ()
        => Amount.Create(1, null, QuantityBasis.PerChild, QuantityPeriod.PerDay).ShouldNotBe(Amount.Create(1, null, QuantityBasis.PerChild));

    [Theory]
    [InlineData(null, QuantityPeriod.PerTrip)]
    [InlineData("", QuantityPeriod.PerTrip)]
    [InlineData("PerDay", QuantityPeriod.PerDay)]
    [InlineData(" pernight ", QuantityPeriod.PerNight)]
    public void Parses_the_api_period(string? value, QuantityPeriod expected)
        => QuantityPeriods.Parse(value).ShouldBe(expected);

    [Theory]
    [InlineData("PerWeek")]
    [InlineData("5")]
    public void Rejects_an_unknown_api_period(string value)
        => Should.Throw<DomainValidationException>(() => QuantityPeriods.Parse(value));
}
