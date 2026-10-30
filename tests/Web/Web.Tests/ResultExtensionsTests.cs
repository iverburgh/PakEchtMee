using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Shared.Validation;
using Shouldly;
using Xunit;

namespace Web.Tests;

public sealed class ResultExtensionsTests
{
    private static ProblemHttpResult ProblemFor(Exception error)
        => Result.Failure<string, Exception>(error).ToHttpResult().ShouldBeOfType<ProblemHttpResult>();

    [Fact]
    public void A_forbidden_failure_becomes_403_with_its_message()
    {
        ProblemHttpResult problem = ProblemFor(new ForbiddenException("Alleen de beheerder mag dit."));

        problem.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
        problem.ProblemDetails.Detail.ShouldBe("Alleen de beheerder mag dit.");
    }

    [Fact]
    public void A_not_found_failure_stays_404()
        => ProblemFor(new NotFoundException()).StatusCode.ShouldBe(StatusCodes.Status404NotFound);

    [Fact]
    public void A_validation_failure_stays_400()
        => ProblemFor(new DomainValidationException()).StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
}
