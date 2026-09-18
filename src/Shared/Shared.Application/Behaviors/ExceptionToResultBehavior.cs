using MediatR;
using Microsoft.Extensions.Logging;

namespace Shared.Application.Behaviors;

/// <summary>
/// Converts an unhandled exception into a failed <c>Result&lt;T, Exception&gt;</c> so handlers and repositories
/// need no try/catch. Responses that are not such a result are passed through untouched.
/// </summary>
public sealed class ExceptionToResultBehavior<TRequest, TResponse>(ILogger<ExceptionToResultBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly Func<Exception, TResponse>? Failure = ResultFailureFactory.TryCreate<TResponse>();

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        if (Failure is null)
            return await next(cancellationToken).ConfigureAwait(false);

        try
        {
            return await next(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Handling {RequestType} failed.", typeof(TRequest).Name);

            return Failure(exception);
        }
    }
}
