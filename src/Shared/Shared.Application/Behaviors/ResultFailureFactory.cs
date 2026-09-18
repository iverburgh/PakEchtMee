using System.Linq.Expressions;
using System.Reflection;
using CSharpFunctionalExtensions;

namespace Shared.Application.Behaviors;

/// <summary>Builds a delegate that turns an <see cref="Exception"/> into a failed <c>Result&lt;T, E&gt;</c>, or nothing when the response is not such a result.</summary>
internal static class ResultFailureFactory
{
    public static Func<Exception, TResponse>? TryCreate<TResponse>()
    {
        Type responseType = typeof(TResponse);

        if (!responseType.IsGenericType || responseType.GetGenericTypeDefinition() != typeof(Result<,>))
            return null;

        Type[] arguments = responseType.GetGenericArguments();
        Type errorType = arguments[1];

        if (!errorType.IsAssignableFrom(typeof(Exception)))
            return null;

        MethodInfo failure = typeof(Result)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method => method is { Name: nameof(Result.Failure), IsGenericMethodDefinition: true }
                && method.GetGenericArguments().Length == 2
                && method.GetParameters().Length == 1)
            .MakeGenericMethod(arguments);

        ParameterExpression error = Expression.Parameter(typeof(Exception), "error");
        MethodCallExpression call = Expression.Call(failure, Expression.Convert(error, errorType));

        return Expression.Lambda<Func<Exception, TResponse>>(call, error).Compile();
    }
}
