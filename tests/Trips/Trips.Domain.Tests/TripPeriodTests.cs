using Shared.Validation;
using Shouldly;
using Xunit;

namespace Trips.Domain.Tests;

/// <summary>The period scenarios of trip-packing: days and nights from the dates, totals per day or night, and changing who and when.</summary>
public sealed class TripPeriodTests
{
    private static readonly Guid Family = Guid.CreateVersion7();
    private static readonly DateOnly July4 = new(2026, 7, 4);
    private static readonly DateOnly July18 = new(2026, 7, 18);

    private static Trip ATrip(DateOnly? start = null, DateOnly? end = null, int adults = 2, int children = 3)
        => Trip.Create(Family, TripName.Create("Summer France"), TripDates.Create(start, end), TripComposition.Create(adults, children));

    private static TripItem Add(Trip trip, string name, int? quantity, QuantityBasis basis, QuantityPeriod period)
    {
        CatalogItemSnapshot snapshot = new(Guid.CreateVersion7(), name, quantity, null, basis, period);
        trip.SelectCategories([new CategorySelection(Guid.CreateVersion7(), "Kleding", [snapshot])]);

        return trip.Items.Single(item => item.SourceItemId == snapshot.SourceItemId);
    }

    [Fact]
    public void Two_weeks_in_france_has_15_days_and_14_nights()
    {
        TripDates dates = TripDates.Create(July4, July18);

        dates.Days.ShouldBe(15);
        dates.Nights.ShouldBe(14);
    }

    [Fact]
    public void A_day_trip_has_one_day_and_no_nights()
    {
        TripDates dates = TripDates.Create(July4, July4);

        dates.Days.ShouldBe(1);
        dates.Nights.ShouldBe(0);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void Without_both_dates_there_is_no_duration_and_a_period_counts_once(bool hasStart, bool hasEnd)
    {
        TripDates dates = TripDates.Create(hasStart ? July4 : null, hasEnd ? July18 : null);

        dates.Days.ShouldBeNull();
        dates.Nights.ShouldBeNull();
        dates.Multiplier(QuantityPeriod.PerDay).ShouldBe(1);
        dates.Multiplier(QuantityPeriod.PerNight).ShouldBe(1);
    }

    [Theory]
    [InlineData(1, QuantityBasis.PerChild, QuantityPeriod.PerDay, 45)]
    [InlineData(2, QuantityBasis.Fixed, QuantityPeriod.PerNight, 28)]
    [InlineData(1, QuantityBasis.PerPerson, QuantityPeriod.PerNight, 70)]
    [InlineData(1, QuantityBasis.PerChild, QuantityPeriod.PerTrip, 3)]
    public void The_total_counts_heads_and_periods(int quantity, QuantityBasis basis, QuantityPeriod period, int expected)
        => Add(ATrip(July4, July18), "Sokken", quantity, basis, period).Total.ShouldBe(expected);

    [Fact]
    public void A_per_day_item_on_a_trip_without_dates_counts_once()
        => Add(ATrip(), "Onderbroeken", 1, QuantityBasis.PerAdult, QuantityPeriod.PerDay).Total.ShouldBe(2);

    [Fact]
    public void A_per_night_item_on_a_day_trip_totals_zero()
        => Add(ATrip(July4, July4), "Theezakjes", 2, QuantityBasis.Fixed, QuantityPeriod.PerNight).Total.ShouldBe(0);

    [Fact]
    public void A_longer_trip_recalculates_per_day_items_still_to_do()
    {
        Trip trip = ATrip(July4, July18);
        TripItem socks = Add(trip, "Sokken", 1, QuantityBasis.PerChild, QuantityPeriod.PerDay);

        int changed = trip.ChangePlan(TripComposition.Create(2, 3), TripDates.Create(July4, July18.AddDays(7)));

        changed.ShouldBe(1);
        socks.Total.ShouldBe(66);
        trip.Dates.Days.ShouldBe(22);
    }

    [Fact]
    public void Dates_added_to_a_trip_without_dates_give_per_day_items_their_total()
    {
        Trip trip = ATrip();
        TripItem socks = Add(trip, "Sokken", 1, QuantityBasis.PerChild, QuantityPeriod.PerDay);
        TripItem teaBags = Add(trip, "Theezakjes", 2, QuantityBasis.Fixed, QuantityPeriod.PerNight);

        trip.ChangePlan(TripComposition.Create(2, 3), TripDates.Create(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 3)));

        socks.Total.ShouldBe(9);
        teaBags.Total.ShouldBe(4);
    }

    [Fact]
    public void A_fixed_item_per_trip_and_a_packed_item_keep_their_total()
    {
        Trip trip = ATrip(July4, July18);
        TripItem pegs = Add(trip, "Haringen", 20, QuantityBasis.Fixed, QuantityPeriod.PerTrip);
        TripItem underwear = Add(trip, "Onderbroeken", 1, QuantityBasis.PerAdult, QuantityPeriod.PerDay);
        trip.AdvanceItem(underwear.Id);

        int changed = trip.ChangePlan(TripComposition.Create(1, 1), TripDates.Create(July4, July4.AddDays(1)));

        changed.ShouldBe(0);
        pegs.Total.ShouldBe(20);
        underwear.Total.ShouldBe(30);
    }

    [Fact]
    public void One_child_staying_home_changes_only_per_child_items()
    {
        Trip trip = ATrip(July4, July18);
        TripItem socks = Add(trip, "Sokken", 1, QuantityBasis.PerChild, QuantityPeriod.PerDay);
        TripItem underwear = Add(trip, "Onderbroeken", 1, QuantityBasis.PerAdult, QuantityPeriod.PerDay);

        trip.ChangePlan(TripComposition.Create(2, 2), trip.Dates).ShouldBe(1);

        socks.Total.ShouldBe(30);
        underwear.Total.ShouldBe(30);
    }

    [Fact]
    public void An_end_date_before_the_start_date_is_rejected()
        => Should.Throw<DomainValidationException>(() => TripDates.Create(July18, July4));

    [Fact]
    public void Changing_an_items_rule_to_per_day_counts_the_days()
    {
        Trip trip = ATrip(July4, July18);
        TripItem underwear = Add(trip, "Onderbroeken", null, QuantityBasis.Fixed, QuantityPeriod.PerTrip);

        trip.ChangeItemAmount(underwear.Id, Amount.Create(1, null, QuantityBasis.PerAdult, QuantityPeriod.PerDay));

        underwear.Total.ShouldBe(30);
    }

    [Fact]
    public void A_period_without_a_quantity_is_rejected()
        => Should.Throw<DomainValidationException>(() => Amount.Create(null, null, QuantityBasis.Fixed, QuantityPeriod.PerNight));

    [Theory]
    [InlineData(null, QuantityPeriod.PerTrip)]
    [InlineData("PerNight", QuantityPeriod.PerNight)]
    [InlineData("perday", QuantityPeriod.PerDay)]
    public void Parses_the_api_period(string? value, QuantityPeriod expected)
        => QuantityPeriods.Parse(value).ShouldBe(expected);

    [Fact]
    public void Rejects_an_unknown_api_period()
        => Should.Throw<DomainValidationException>(() => QuantityPeriods.Parse("PerWeek"));
}
