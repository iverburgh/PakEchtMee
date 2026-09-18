using System.Diagnostics;
using MediatR;
using Shared.Application.Behaviors;
using Shouldly;
using Xunit;

namespace Shared.Application.Tests.Behaviors;

public sealed class TracingBehaviorTests : IDisposable
{
    private readonly List<Activity> activities = [];
    private readonly ActivityListener listener;

    public TracingBehaviorTests()
    {
        listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ApplicationTelemetry.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activities.Add,
        };

        ActivitySource.AddActivityListener(listener);
    }

    private sealed record SampleRequest : IRequest<string>;

    [Fact]
    public async Task Records_a_span_for_a_successful_handler()
    {
        string response = await new TracingBehavior<SampleRequest, string>()
            .Handle(new SampleRequest(), _ => Task.FromResult("done"), TestContext.Current.CancellationToken);

        response.ShouldBe("done");
        activities.ShouldContain(activity => activity.DisplayName == nameof(SampleRequest) && activity.Status == ActivityStatusCode.Ok);
    }

    [Fact]
    public async Task Marks_the_span_as_failed_and_rethrows()
    {
        await Should.ThrowAsync<InvalidOperationException>(
            () => new TracingBehavior<SampleRequest, string>()
                .Handle(new SampleRequest(), _ => throw new InvalidOperationException("boom"), TestContext.Current.CancellationToken));

        activities.ShouldContain(activity => activity.DisplayName == nameof(SampleRequest) && activity.Status == ActivityStatusCode.Error);
    }

    public void Dispose() => listener.Dispose();
}
