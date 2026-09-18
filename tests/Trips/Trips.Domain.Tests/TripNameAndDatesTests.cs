using Shared.Validation;
using Shouldly;
using Xunit;

namespace Trips.Domain.Tests;

public sealed class TripNameAndDatesTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_trip_name_cannot_be_blank(string? value)
        => Should.Throw<DomainValidationException>(() => TripName.Create(value));

    [Fact]
    public void A_trip_name_is_trimmed()
        => TripName.Create("  Summer France  ").Value.ShouldBe("Summer France");

    [Fact]
    public void A_trip_name_cannot_exceed_its_maximum_length()
        => Should.Throw<DomainValidationException>(() => TripName.Create(new string('a', TripName.MaxLength + 1)));

    [Fact]
    public void Dates_are_optional()
    {
        TripDates dates = TripDates.Create(null, null);

        dates.Start.ShouldBeNull();
        dates.End.ShouldBeNull();
    }

    [Fact]
    public void An_end_date_equal_to_the_start_date_is_allowed()
    {
        DateOnly day = new(2026, 7, 4);

        TripDates.Create(day, day).End.ShouldBe(day);
    }

    [Fact]
    public void An_end_date_before_the_start_date_is_rejected()
        => Should.Throw<DomainValidationException>(() => TripDates.Create(new DateOnly(2026, 7, 18), new DateOnly(2026, 7, 4)));
}
