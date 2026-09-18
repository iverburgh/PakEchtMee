using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Shared.Validation;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Web;

/// <summary>Maps a handler result onto an HTTP response, keeping the error vocabulary in one place.</summary>
internal static class ResultExtensions
{
    public static IResult ToHttpResult<T>(this Result<T, Exception> result)
        => result.IsSuccess ? Results.Ok(result.Value) : ToProblem(result.Error);

    public static IResult ToHttpResult<T>(this Result<T, Exception> result, Func<T, IResult> onSuccess)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);

        return result.IsSuccess ? onSuccess(result.Value) : ToProblem(result.Error);
    }

    private static IResult ToProblem(Exception error) => error switch
    {
        DomainValidationException => Results.Problem(
            title: "Validatie mislukt",
            detail: error.Message,
            statusCode: StatusCodes.Status400BadRequest),
        NotFoundException => Results.Problem(
            title: "Niet gevonden",
            detail: error.Message,
            statusCode: StatusCodes.Status404NotFound),
        DbUpdateConcurrencyException => Results.Problem(
            title: "Conflict",
            detail: "Dit item is ondertussen door een andere actie gewijzigd. Ververs en probeer het opnieuw.",
            statusCode: StatusCodes.Status409Conflict),
        // Anything else is unexpected; the pipeline already logged it, so nothing internal is exposed here.
        _ => Results.Problem(
            title: "Er ging iets mis",
            detail: "De actie kon niet worden uitgevoerd. Probeer het opnieuw.",
            statusCode: StatusCodes.Status500InternalServerError),
    };
}
