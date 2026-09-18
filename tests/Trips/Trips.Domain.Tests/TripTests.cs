using Shared.Validation;
using Shouldly;
using Xunit;

namespace Trips.Domain.Tests;

public sealed class TripTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 4, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid ToiletriesId = Guid.CreateVersion7();
    private static readonly Guid ElectronicsId = Guid.CreateVersion7();

    private static Trip ATrip(string name = "Summer France")
        => Trip.Create(TripName.Create(name), TripDates.None);

    private static CategorySelection Toiletries(int itemCount = 4)
        => new(ToiletriesId, "Toiletries", [.. Enumerable.Range(1, itemCount).Select(index => new CatalogItemSnapshot(Guid.CreateVersion7(), $"Toiletry {index}", null, null))]);

    private static CategorySelection Electronics(int itemCount = 3)
        => new(ElectronicsId, "Electronics", [.. Enumerable.Range(1, itemCount).Select(index => new CatalogItemSnapshot(Guid.CreateVersion7(), $"Device {index}", null, null))]);

    [Fact]
    public void Selecting_categories_generates_one_active_item_per_catalog_item()
    {
        Trip trip = ATrip();

        trip.SelectCategories([Toiletries(), Electronics()]);

        trip.Items.Count.ShouldBe(7);
        trip.Items.ShouldAllBe(item => item.Status == PackingStatus.Active);
        trip.Categories.Count.ShouldBe(2);
    }

    [Fact]
    public void Generated_items_copy_the_name_category_and_amount()
    {
        Trip trip = ATrip();
        CategorySelection selection = new(ToiletriesId, "Toiletries", [new CatalogItemSnapshot(Guid.CreateVersion7(), "Socks", 7, "pair")]);

        trip.SelectCategories([selection]);

        TripItem item = trip.Items[0];
        item.Name.ShouldBe("Socks");
        item.CategoryName.ShouldBe("Toiletries");
        item.Amount!.ToString().ShouldBe("7 pair");
    }

    [Fact]
    public void Adding_a_category_later_leaves_existing_statuses_untouched()
    {
        Trip trip = ATrip();
        trip.SelectCategories([Toiletries(2)]);
        trip.AdvanceItem(trip.Items[0].Id);

        trip.SelectCategories([Electronics(1)]);

        trip.Items.Count.ShouldBe(3);
        trip.Items[0].Status.ShouldBe(PackingStatus.Prepared);
        trip.Items[2].Status.ShouldBe(PackingStatus.Active);
    }

    [Fact]
    public void Re_adding_a_category_restores_its_items_instead_of_duplicating_them()
    {
        Trip trip = ATrip();
        CategorySelection selection = Toiletries(3);
        trip.SelectCategories([selection]);

        trip.RemoveCategory(ToiletriesId, Now);
        trip.SelectCategories([selection]);

        trip.Items.Count.ShouldBe(3);
        trip.Items.ShouldAllBe(item => !item.IsDeleted);
        trip.Categories.Count.ShouldBe(1);
    }

    [Fact]
    public void Removing_a_category_deletes_its_items()
    {
        Trip trip = ATrip();
        trip.SelectCategories([Toiletries(2), Electronics(1)]);

        trip.RemoveCategory(ToiletriesId, Now);

        trip.Items.Count(item => item.IsDeleted).ShouldBe(2);
        trip.Categories.Count.ShouldBe(1);
    }

    [Fact]
    public void Confirmation_is_needed_only_when_a_category_has_progressed_items()
    {
        Trip trip = ATrip();
        trip.SelectCategories([Toiletries(2)]);

        trip.HasProgressedItemsIn(ToiletriesId).ShouldBeFalse();

        trip.AdvanceItem(trip.Items[0].Id);

        trip.HasProgressedItemsIn(ToiletriesId).ShouldBeTrue();
    }

    [Fact]
    public void Advancing_three_times_walks_the_whole_flow()
    {
        Trip trip = ATrip();
        trip.SelectCategories([Toiletries(1)]);
        Guid itemId = trip.Items[0].Id;

        trip.AdvanceItem(itemId).Status.ShouldBe(PackingStatus.Prepared);
        trip.AdvanceItem(itemId).Status.ShouldBe(PackingStatus.Packed);
        trip.AdvanceItem(itemId).Status.ShouldBe(PackingStatus.Loaded);
    }

    [Fact]
    public void A_loaded_item_cannot_advance_further()
    {
        Trip trip = ATrip();
        trip.SelectCategories([Toiletries(1)]);
        Guid itemId = trip.Items[0].Id;
        trip.AdvanceItem(itemId);
        trip.AdvanceItem(itemId);
        trip.AdvanceItem(itemId);

        Should.Throw<DomainValidationException>(() => trip.AdvanceItem(itemId));
        trip.Items[0].Status.ShouldBe(PackingStatus.Loaded);
    }

    [Fact]
    public void Reverting_corrects_a_mistake()
    {
        Trip trip = ATrip();
        trip.SelectCategories([Toiletries(1)]);
        Guid itemId = trip.Items[0].Id;
        trip.AdvanceItem(itemId);
        trip.AdvanceItem(itemId);

        trip.RevertItem(itemId).Status.ShouldBe(PackingStatus.Prepared);
    }

    [Fact]
    public void An_active_item_cannot_move_further_back()
    {
        Trip trip = ATrip();
        trip.SelectCategories([Toiletries(1)]);

        Should.Throw<DomainValidationException>(() => trip.RevertItem(trip.Items[0].Id));
        trip.Items[0].Status.ShouldBe(PackingStatus.Active);
    }

    [Fact]
    public void Skipping_a_status_is_rejected()
    {
        Trip trip = ATrip();
        trip.SelectCategories([Toiletries(1)]);

        Should.Throw<DomainValidationException>(() => trip.ChangeItemStatus(trip.Items[0].Id, PackingStatus.Loaded));
        trip.Items[0].Status.ShouldBe(PackingStatus.Active);
    }

    [Fact]
    public void Changing_an_item_to_its_current_status_is_a_no_op()
    {
        Trip trip = ATrip();
        trip.SelectCategories([Toiletries(1)]);

        trip.ChangeItemStatus(trip.Items[0].Id, PackingStatus.Active).Status.ShouldBe(PackingStatus.Active);
    }

    [Fact]
    public void An_unknown_item_reports_not_found()
    {
        Trip trip = ATrip();

        Should.Throw<NotFoundException>(() => trip.AdvanceItem(Guid.CreateVersion7()));
    }

    [Fact]
    public void Deleting_an_item_keeps_its_status_for_the_restore()
    {
        Trip trip = ATrip();
        trip.SelectCategories([Toiletries(1)]);
        Guid itemId = trip.Items[0].Id;
        trip.AdvanceItem(itemId);
        trip.AdvanceItem(itemId);

        trip.DeleteItem(itemId, Now).IsDeleted.ShouldBeTrue();
        TripItem restored = trip.RestoreItem(itemId);

        restored.IsDeleted.ShouldBeFalse();
        restored.Status.ShouldBe(PackingStatus.Packed);
    }

    [Fact]
    public void A_deleted_item_cannot_change_status()
    {
        Trip trip = ATrip();
        trip.SelectCategories([Toiletries(1)]);
        Guid itemId = trip.Items[0].Id;
        trip.DeleteItem(itemId, Now);

        Should.Throw<DomainValidationException>(() => trip.AdvanceItem(itemId));
    }

    [Fact]
    public void Progress_counts_every_status_of_the_live_items()
    {
        Trip trip = ATrip();
        trip.SelectCategories([Toiletries(10)]);

        for (int index = 0; index < 4; index++)
        {
            Guid itemId = trip.Items[index].Id;
            trip.AdvanceItem(itemId);
            trip.AdvanceItem(itemId);
            trip.AdvanceItem(itemId);
        }

        TripProgress progress = trip.Progress();

        progress.Total.ShouldBe(10);
        progress.Loaded.ShouldBe(4);
        progress.Active.ShouldBe(6);
        progress.LoadedShare.ShouldBe(0.4);
    }

    [Fact]
    public void Deleted_items_drop_out_of_the_progress_total()
    {
        Trip trip = ATrip();
        trip.SelectCategories([Toiletries(10)]);

        trip.DeleteItem(trip.Items[0].Id, Now);

        trip.Progress().Total.ShouldBe(9);
    }

    [Fact]
    public void An_empty_trip_has_no_loaded_share()
        => ATrip().Progress().LoadedShare.ShouldBe(0d);
}
