using Shared.Validation;
using Shouldly;
using Xunit;

namespace Trips.Domain.Tests;

/// <summary>The quantity scenarios of trip-packing: totals from the trip composition, recalculation and per-item changes.</summary>
public sealed class TripQuantityTests
{
    private static readonly Guid Family = Guid.CreateVersion7();
    private static readonly Guid ClothingId = Guid.CreateVersion7();

    private static Trip ATrip(int adults = 2, int children = 3)
        => Trip.Create(Family, TripName.Create("Summer France"), TripDates.None, TripComposition.Create(adults, children));

    private static CatalogItemSnapshot Snapshot(string name, int? quantity, QuantityBasis basis = QuantityBasis.Fixed, string? unit = null)
        => new(Guid.CreateVersion7(), name, quantity, unit, basis);

    private static TripItem Add(Trip trip, CatalogItemSnapshot snapshot)
    {
        trip.SelectCategories([new CategorySelection(ClothingId, "Kinderen – Buitenkleding", [snapshot])]);

        return trip.Items.Single(item => item.SourceItemId == snapshot.SourceItemId);
    }

    [Theory]
    [InlineData(QuantityBasis.PerChild, 1, 3)]
    [InlineData(QuantityBasis.PerAdult, 1, 2)]
    [InlineData(QuantityBasis.PerPerson, 1, 5)]
    [InlineData(QuantityBasis.PerPerson, 2, 10)]
    [InlineData(QuantityBasis.Fixed, 20, 20)]
    public void The_total_follows_the_trip_composition(QuantityBasis basis, int quantity, int expected)
    {
        TripItem item = Add(ATrip(adults: 2, children: 3), Snapshot("Jassen", quantity, basis));

        item.Total.ShouldBe(expected);
        item.Amount!.Basis.ShouldBe(basis);
    }

    [Fact]
    public void A_per_child_item_on_a_trip_without_children_totals_zero()
        => Add(ATrip(adults: 2, children: 0), Snapshot("Jassen", 1, QuantityBasis.PerChild)).Total.ShouldBe(0);

    [Fact]
    public void An_item_without_a_quantity_has_no_total()
        => Add(ATrip(), Snapshot("Paraplu", null)).Total.ShouldBeNull();

    [Fact]
    public void A_trip_without_a_composition_gets_two_adults_and_no_children()
    {
        Trip trip = Trip.Create(Family, TripName.Create("Day at the beach"), TripDates.None);

        trip.Composition.ShouldBe(TripComposition.Create(2, 0));
    }

    [Fact]
    public void Changing_the_composition_recalculates_only_items_that_are_still_to_do()
    {
        Trip trip = ATrip(children: 3);
        TripItem jackets = Add(trip, Snapshot("Jassen", 1, QuantityBasis.PerChild));
        TripItem shoes = Add(trip, Snapshot("Schoenen", 1, QuantityBasis.PerChild));
        TripItem pegs = Add(trip, Snapshot("Haringen", 20));
        trip.AdvanceItem(shoes.Id);
        trip.AdvanceItem(shoes.Id);

        int changed = trip.ChangeComposition(TripComposition.Create(2, 2));

        changed.ShouldBe(1);
        jackets.Total.ShouldBe(2);
        shoes.Total.ShouldBe(3);
        pegs.Total.ShouldBe(20);
        trip.Composition.ShouldBe(TripComposition.Create(2, 2));
    }

    [Fact]
    public void A_deleted_item_keeps_its_total_when_the_composition_changes()
    {
        Trip trip = ATrip(children: 3);
        TripItem jackets = Add(trip, Snapshot("Jassen", 1, QuantityBasis.PerChild));
        trip.DeleteItem(jackets.Id, DateTimeOffset.UtcNow);

        trip.ChangeComposition(TripComposition.Create(2, 1)).ShouldBe(0);
        jackets.Total.ShouldBe(3);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(-1, 2)]
    [InlineData(2, 21)]
    public void An_invalid_composition_is_rejected(int adults, int children)
        => Should.Throw<DomainValidationException>(() => TripComposition.Create(adults, children));

    [Fact]
    public void Changing_an_items_rule_recalculates_its_total()
    {
        Trip trip = ATrip(children: 3);
        TripItem jackets = Add(trip, Snapshot("Jassen", 1, QuantityBasis.PerChild));

        trip.ChangeItemAmount(jackets.Id, Amount.Create(2, null, QuantityBasis.PerChild));

        jackets.Total.ShouldBe(6);
        jackets.Amount!.Quantity.ShouldBe(2);
    }

    [Fact]
    public void A_fixed_number_is_kept_when_the_composition_changes()
    {
        Trip trip = ATrip(children: 3);
        TripItem towels = Add(trip, Snapshot("Handdoeken", 1, QuantityBasis.PerPerson));

        trip.ChangeItemAmount(towels.Id, Amount.Create(4, null));
        trip.ChangeComposition(TripComposition.Create(2, 1));

        towels.Total.ShouldBe(4);
    }

    [Fact]
    public void A_later_composition_change_uses_the_new_rule()
    {
        Trip trip = ATrip(children: 3);
        TripItem jackets = Add(trip, Snapshot("Jassen", 1, QuantityBasis.PerChild));
        trip.ChangeItemAmount(jackets.Id, Amount.Create(2, null, QuantityBasis.PerChild));

        trip.ChangeComposition(TripComposition.Create(2, 1));

        jackets.Total.ShouldBe(2);
    }

    [Fact]
    public void Removing_the_quantity_removes_the_total()
    {
        Trip trip = ATrip();
        TripItem umbrella = Add(trip, Snapshot("Paraplu", 1, QuantityBasis.PerPerson));

        trip.ChangeItemAmount(umbrella.Id, null);

        umbrella.Amount.ShouldBeNull();
        umbrella.Total.ShouldBeNull();
    }

    [Fact]
    public void The_quantity_of_a_progressed_item_cannot_be_changed()
    {
        Trip trip = ATrip();
        TripItem jackets = Add(trip, Snapshot("Jassen", 1, QuantityBasis.PerChild));
        trip.AdvanceItem(jackets.Id);

        Should.Throw<DomainValidationException>(() => trip.ChangeItemAmount(jackets.Id, Amount.Create(2, null)));
        jackets.Total.ShouldBe(3);
    }

    [Fact]
    public void The_quantity_of_a_deleted_item_cannot_be_changed()
    {
        Trip trip = ATrip();
        TripItem jackets = Add(trip, Snapshot("Jassen", 1, QuantityBasis.PerChild));
        trip.DeleteItem(jackets.Id, DateTimeOffset.UtcNow);

        Should.Throw<DomainValidationException>(() => trip.ChangeItemAmount(jackets.Id, Amount.Create(2, null)));
    }

    [Fact]
    public void Changing_the_quantity_of_an_unknown_item_is_not_found()
        => Should.Throw<NotFoundException>(() => ATrip().ChangeItemAmount(Guid.CreateVersion7(), Amount.Create(1, null)));

    [Theory]
    [InlineData(null, QuantityBasis.Fixed)]
    [InlineData("PerChild", QuantityBasis.PerChild)]
    [InlineData("perperson", QuantityBasis.PerPerson)]
    public void Parses_the_api_basis(string? value, QuantityBasis expected)
        => QuantityBases.Parse(value).ShouldBe(expected);

    [Fact]
    public void Rejects_an_unknown_api_basis()
        => Should.Throw<DomainValidationException>(() => QuantityBases.Parse("PerDog"));

    [Fact]
    public void A_basis_without_a_quantity_is_rejected()
        => Should.Throw<DomainValidationException>(() => Amount.Create(null, null, QuantityBasis.PerChild));
}
