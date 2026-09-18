using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Behaviors;
using Shouldly;
using Xunit;

namespace Catalog.Application.Tests;

public sealed class PipelineRegistrationTests
{
    [Fact]
    public void The_catalog_registration_wires_both_shared_behaviours_in_order()
    {
        CatalogHarness harness = new();

        IPipelineBehavior<CreateSample, string>[] behaviors =
            [.. harness.Services.GetServices<IPipelineBehavior<CreateSample, string>>()];

        behaviors.Select(behavior => behavior.GetType().GetGenericTypeDefinition())
            .ShouldBe([typeof(TracingBehavior<,>), typeof(ExceptionToResultBehavior<,>)]);
    }

    public sealed record CreateSample : IRequest<string>;
}
