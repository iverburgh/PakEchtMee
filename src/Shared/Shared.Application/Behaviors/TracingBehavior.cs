using System.Diagnostics;
using MediatR;

namespace Shared.Application.Behaviors;

/// <summary>Wraps every handler in a span so a request shows up as one unit of work in the traces.</summary>
public sealed class TracingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        using Activity? activity = ApplicationTelemetry.ActivitySource.StartActivity(typeof(TRequest).Name, ActivityKind.Internal);
        activity?.SetTag("mediatr.request_type", typeof(TRequest).FullName);

        try
        {
            TResponse response = await next(cancellationToken).ConfigureAwait(false);
            activity?.SetStatus(ActivityStatusCode.Ok);

            return response;
        }
        catch (Exception exception)
        {
            activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            throw;
        }
    }
}
