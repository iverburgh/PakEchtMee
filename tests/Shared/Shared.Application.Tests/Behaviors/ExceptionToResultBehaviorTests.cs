using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Application.Behaviors;
using Shouldly;
using Xunit;

namespace Shared.Application.Tests.Behaviors;

public sealed class ExceptionToResultBehaviorTests
{
    private sealed record ResultRequest : IRequest<Result<string, Exception>>;

    private sealed record PlainRequest : IRequest<string>;

    private static ExceptionToResultBehavior<ResultRequest, Result<string, Exception>> ResultBehavior()
        => new(NullLogger<ExceptionToResultBehavior<ResultRequest, Result<string, Exception>>>.Instance);

    private static ExceptionToResultBehavior<PlainRequest, string> PlainBehavior()
        => new(NullLogger<ExceptionToResultBehavior<PlainRequest, string>>.Instance);

    [Fact]
    public async Task Converts_an_exception_into_a_failed_result()
    {
        InvalidOperationException thrown = new("boom");

        Result<string, Exception> result = await ResultBehavior()
            .Handle(new ResultRequest(), _ => throw thrown, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeSameAs(thrown);
    }

    [Fact]
    public async Task Returns_the_handler_result_untouched_on_success()
    {
        Result<string, Exception> result = await ResultBehavior()
            .Handle(new ResultRequest(), _ => Task.FromResult<Result<string, Exception>>("packed"), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("packed");
    }

    [Fact]
    public async Task Rethrows_a_cancellation()
    {
        await Should.ThrowAsync<OperationCanceledException>(
            () => ResultBehavior().Handle(new ResultRequest(), _ => throw new OperationCanceledException(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Passes_through_a_response_that_is_not_a_result()
    {
        string response = await PlainBehavior()
            .Handle(new PlainRequest(), _ => Task.FromResult("plain"), TestContext.Current.CancellationToken);

        response.ShouldBe("plain");
    }

    [Fact]
    public async Task Lets_an_exception_escape_when_the_response_is_not_a_result()
    {
        await Should.ThrowAsync<InvalidOperationException>(
            () => PlainBehavior().Handle(new PlainRequest(), _ => throw new InvalidOperationException("boom"), TestContext.Current.CancellationToken));
    }
}
