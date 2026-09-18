using Shouldly;
using Xunit;

namespace Shared.Validation.Tests;

public sealed class GuardTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AgainstNullOrWhiteSpace_rejects_blank_input(string? value)
    {
        Should.Throw<DomainValidationException>(() => Guard.AgainstNullOrWhiteSpace(value, "Name"))
            .Message.ShouldContain("Name");
    }

    [Fact]
    public void AgainstNullOrWhiteSpace_trims_the_value()
        => Guard.AgainstNullOrWhiteSpace("  Toiletries  ", "Name").ShouldBe("Toiletries");

    [Fact]
    public void AgainstLengthAbove_accepts_the_maximum_length()
        => Guard.AgainstLengthAbove(new string('a', 60), 60, "Name").Length.ShouldBe(60);

    [Fact]
    public void AgainstLengthAbove_rejects_one_character_too_many()
        => Should.Throw<DomainValidationException>(() => Guard.AgainstLengthAbove(new string('a', 61), 60, "Name"));

    [Fact]
    public void RequiredText_trims_before_measuring_the_length()
        => Guard.RequiredText($"  {new string('a', 60)}  ", 60, "Name").Length.ShouldBe(60);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void OptionalText_treats_blank_input_as_absent(string? value)
        => Guard.OptionalText(value, 20, "Unit").ShouldBeNull();

    [Fact]
    public void OptionalText_trims_a_present_value()
        => Guard.OptionalText(" pair ", 20, "Unit").ShouldBe("pair");

    [Fact]
    public void OptionalText_rejects_an_over_long_value()
        => Should.Throw<DomainValidationException>(() => Guard.OptionalText(new string('a', 21), 20, "Unit"));

    [Theory]
    [InlineData(1)]
    [InlineData(9999)]
    public void AgainstOutOfRange_accepts_the_boundaries(int value)
        => Guard.AgainstOutOfRange(value, 1, 9999, "Quantity").ShouldBe(value);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10000)]
    public void AgainstOutOfRange_rejects_values_outside_the_range(int value)
        => Should.Throw<DomainValidationException>(() => Guard.AgainstOutOfRange(value, 1, 9999, "Quantity"));
}
