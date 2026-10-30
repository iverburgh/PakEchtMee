using CSharpFunctionalExtensions;
using Families.Application.Contracts;
using Families.Application.Members;
using Families.Domain;
using Moq;
using Shared.Validation;
using Shouldly;
using Xunit;

namespace Families.Application.Tests;

public sealed class MemberHandlerTests
{
    private readonly FamiliesHarness harness = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Adding_a_member_stores_the_lower_case_address()
    {
        Family family = harness.GivenFamily();

        Result<FamilyDto, Exception> result = await harness.Mediator.Send(
            new AddMemberHandler.Command(family.Id, "Oma.Jansen@Gmail.com"), CancellationToken);

        result.Value.Members.ShouldBe(["oma.jansen@gmail.com"]);
        harness.Families.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Adding_an_account_of_another_family_names_that_family()
    {
        harness.GivenFamily("Verburgh", "martine@gmail.com");
        Family jansen = harness.GivenFamily("Jansen");

        Result<FamilyDto, Exception> result = await harness.Mediator.Send(
            new AddMemberHandler.Command(jansen.Id, "martine@gmail.com"), CancellationToken);

        result.Error.ShouldBeOfType<DomainValidationException>();
        result.Error.Message.ShouldContain("Verburgh");
        harness.Families.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Adding_an_invalid_address_is_rejected()
    {
        Family family = harness.GivenFamily();

        Result<FamilyDto, Exception> result = await harness.Mediator.Send(
            new AddMemberHandler.Command(family.Id, "oma.jansen"), CancellationToken);

        result.Error.ShouldBeOfType<DomainValidationException>();
    }

    [Fact]
    public async Task Adding_to_an_unknown_family_is_not_found()
    {
        Result<FamilyDto, Exception> result = await harness.Mediator.Send(
            new AddMemberHandler.Command(Guid.CreateVersion7(), "oma.jansen@gmail.com"), CancellationToken);

        result.Error.ShouldBeOfType<NotFoundException>();
    }

    [Fact]
    public async Task Removing_a_member_takes_it_out_of_the_family()
    {
        Family family = harness.GivenFamily("Jansen", "oma.jansen@gmail.com");

        Result<FamilyDto, Exception> result = await harness.Mediator.Send(
            new RemoveMemberHandler.Command(family.Id, "oma.jansen@gmail.com"), CancellationToken);

        result.Value.Members.ShouldBeEmpty();
        harness.Families.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Removing_someone_who_is_not_a_member_is_not_found()
    {
        Family family = harness.GivenFamily();

        Result<FamilyDto, Exception> result = await harness.Mediator.Send(
            new RemoveMemberHandler.Command(family.Id, "vreemde@gmail.com"), CancellationToken);

        result.Error.ShouldBeOfType<NotFoundException>();
    }

    [Fact]
    public async Task Membership_of_a_member_names_the_family()
    {
        Family family = harness.GivenFamily("Verburgh", "ivoverburgh@gmail.com");

        Result<Maybe<MembershipDto>, Exception> result = await harness.Mediator.Send(
            new GetMembershipHandler.Query("IvoVerburgh@Gmail.com"), CancellationToken);

        result.Value.HasValue.ShouldBeTrue();
        result.Value.Value.ShouldBe(new MembershipDto(family.Id, "Verburgh"));
    }

    [Fact]
    public async Task Membership_of_a_stranger_is_empty()
    {
        Result<Maybe<MembershipDto>, Exception> result = await harness.Mediator.Send(
            new GetMembershipHandler.Query("vreemde@gmail.com"), CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.HasNoValue.ShouldBeTrue();
    }
}
