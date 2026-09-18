using Shouldly;
using Xunit;

namespace Trips.Domain.Tests;

public sealed class PackingStatusTransitionTests
{
    [Theory]
    [InlineData(PackingStatus.Active, PackingStatus.Prepared)]
    [InlineData(PackingStatus.Prepared, PackingStatus.Packed)]
    [InlineData(PackingStatus.Packed, PackingStatus.Loaded)]
    public void Each_status_has_exactly_one_next_step(PackingStatus from, PackingStatus expected)
        => new PackingStatusTransition(from).Next.ShouldBe(expected);

    [Theory]
    [InlineData(PackingStatus.Prepared, PackingStatus.Active)]
    [InlineData(PackingStatus.Packed, PackingStatus.Prepared)]
    [InlineData(PackingStatus.Loaded, PackingStatus.Packed)]
    public void Each_status_has_exactly_one_previous_step(PackingStatus from, PackingStatus expected)
        => new PackingStatusTransition(from).Previous.ShouldBe(expected);

    [Fact]
    public void Loaded_has_no_next_status()
    {
        PackingStatusTransition transition = new(PackingStatus.Loaded);

        transition.Next.ShouldBeNull();
        transition.CanAdvance.ShouldBeFalse();
    }

    [Fact]
    public void Active_has_no_previous_status()
    {
        PackingStatusTransition transition = new(PackingStatus.Active);

        transition.Previous.ShouldBeNull();
        transition.CanRevert.ShouldBeFalse();
    }

    [Fact]
    public void Skipping_a_step_is_not_a_single_step()
        => new PackingStatusTransition(PackingStatus.Active).IsSingleStepTo(PackingStatus.Loaded).ShouldBeFalse();

    [Fact]
    public void One_step_forward_and_backward_are_single_steps()
    {
        PackingStatusTransition transition = new(PackingStatus.Packed);

        transition.IsSingleStepTo(PackingStatus.Loaded).ShouldBeTrue();
        transition.IsSingleStepTo(PackingStatus.Prepared).ShouldBeTrue();
    }
}
