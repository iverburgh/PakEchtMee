using CSharpFunctionalExtensions;
using Families.Application.Contracts;
using Families.Application.Families;
using Families.Domain;
using Moq;
using Shared.Validation;
using Shouldly;
using Xunit;

namespace Families.Application.Tests;

public sealed class FamilyHandlerTests
{
    private readonly FamiliesHarness harness = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Creating_a_family_stores_and_returns_it()
    {
        Result<FamilyDto, Exception> result = await harness.Mediator.Send(new CreateFamilyHandler.Command("  Jansen "), CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Jansen");
        result.Value.Members.ShouldBeEmpty();
        harness.Families.Verify(repository => repository.Add(It.Is<Family>(family => family.Name.Value == "Jansen")), Times.Once);
        harness.Families.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Creating_a_family_with_an_existing_name_is_rejected()
    {
        harness.Families
            .Setup(repository => repository.ExistsWithNameAsync(It.IsAny<FamilyName>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Result<FamilyDto, Exception> result = await harness.Mediator.Send(new CreateFamilyHandler.Command("verburgh"), CancellationToken);

        result.Error.ShouldBeOfType<DomainValidationException>();
        harness.Families.Verify(repository => repository.Add(It.IsAny<Family>()), Times.Never);
    }

    [Fact]
    public async Task Renaming_a_family_keeps_its_members()
    {
        Family family = harness.GivenFamily("Jansen", "oma.jansen@gmail.com");

        Result<FamilyDto, Exception> result = await harness.Mediator.Send(
            new RenameFamilyHandler.Command(family.Id, "Familie Jansen"), CancellationToken);

        result.Value.Name.ShouldBe("Familie Jansen");
        result.Value.Members.ShouldBe(["oma.jansen@gmail.com"]);
    }

    [Fact]
    public async Task Renaming_to_an_existing_name_is_rejected()
    {
        Family family = harness.GivenFamily();
        harness.Families
            .Setup(repository => repository.ExistsWithNameAsync(It.IsAny<FamilyName>(), family.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Result<FamilyDto, Exception> result = await harness.Mediator.Send(
            new RenameFamilyHandler.Command(family.Id, "Verburgh"), CancellationToken);

        result.Error.ShouldBeOfType<DomainValidationException>();
    }

    [Fact]
    public async Task Renaming_an_unknown_family_is_not_found()
    {
        Result<FamilyDto, Exception> result = await harness.Mediator.Send(
            new RenameFamilyHandler.Command(Guid.CreateVersion7(), "Jansen"), CancellationToken);

        result.Error.ShouldBeOfType<NotFoundException>();
    }

    [Fact]
    public async Task Listing_returns_every_family_with_its_members()
    {
        Family jansen = Family.Create(FamilyName.Create("Jansen"));
        jansen.AddMember(EmailAddress.Create("oma.jansen@gmail.com"));
        harness.Families.Setup(repository => repository.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync([jansen]);

        Result<IReadOnlyList<FamilyDto>, Exception> result = await harness.Mediator.Send(new ListFamiliesHandler.Query(), CancellationToken);

        result.Value.Single().Members.ShouldBe(["oma.jansen@gmail.com"]);
    }

    [Fact]
    public async Task A_failing_repository_becomes_a_failed_result()
    {
        harness.Families
            .Setup(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("database unavailable"));

        Result<FamilyDto, Exception> result = await harness.Mediator.Send(new CreateFamilyHandler.Command("Jansen"), CancellationToken);

        result.Error.ShouldBeOfType<InvalidOperationException>();
    }
}
