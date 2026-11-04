using CSharpFunctionalExtensions;
using Families.Application.Composition;
using Families.Application.Contracts;
using Families.Domain;
using Moq;
using Shared.Validation;
using Shouldly;
using Xunit;

namespace Families.Application.Tests;

public sealed class CompositionHandlerTests
{
    private readonly FamiliesHarness harness = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Reading_the_composition_returns_the_family_default()
    {
        Family family = harness.GivenFamily();

        Result<FamilyCompositionDto, Exception> result =
            await harness.Mediator.Send(new GetFamilyCompositionHandler.Query(family.Id), CancellationToken);

        result.Value.ShouldBe(new FamilyCompositionDto(2, 0));
    }

    [Fact]
    public async Task Reading_the_composition_of_an_unknown_family_is_not_found()
    {
        Result<FamilyCompositionDto, Exception> result =
            await harness.Mediator.Send(new GetFamilyCompositionHandler.Query(Guid.CreateVersion7()), CancellationToken);

        result.Error.ShouldBeOfType<NotFoundException>();
    }

    [Fact]
    public async Task Changing_the_composition_stores_it()
    {
        Family family = harness.GivenFamily("Verburgh");

        Result<FamilyCompositionDto, Exception> result =
            await harness.Mediator.Send(new ChangeFamilyCompositionHandler.Command(family.Id, 2, 3), CancellationToken);

        result.Value.ShouldBe(new FamilyCompositionDto(2, 3));
        family.Composition.ShouldBe(FamilyComposition.Create(2, 3));
        harness.Families.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task An_empty_composition_is_rejected_and_not_saved()
    {
        Family family = harness.GivenFamily();

        Result<FamilyCompositionDto, Exception> result =
            await harness.Mediator.Send(new ChangeFamilyCompositionHandler.Command(family.Id, 0, 0), CancellationToken);

        result.Error.ShouldBeOfType<DomainValidationException>();
        family.Composition.ShouldBe(FamilyComposition.Default);
        harness.Families.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Changing_the_composition_of_an_unknown_family_is_not_found()
    {
        Result<FamilyCompositionDto, Exception> result =
            await harness.Mediator.Send(new ChangeFamilyCompositionHandler.Command(Guid.CreateVersion7(), 2, 1), CancellationToken);

        result.Error.ShouldBeOfType<NotFoundException>();
    }

    [Fact]
    public async Task A_failing_repository_becomes_a_failure()
    {
        Family family = harness.GivenFamily();
        harness.Families
            .Setup(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("database down"));

        Result<FamilyCompositionDto, Exception> result =
            await harness.Mediator.Send(new ChangeFamilyCompositionHandler.Command(family.Id, 2, 1), CancellationToken);

        result.Error.ShouldBeOfType<InvalidOperationException>();
    }
}
