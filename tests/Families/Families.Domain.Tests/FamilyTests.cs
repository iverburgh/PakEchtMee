using Shared.Validation;
using Shouldly;
using Xunit;

namespace Families.Domain.Tests;

public sealed class FamilyTests
{
    private static Family AFamily(string name = "Jansen") => Family.Create(FamilyName.Create(name));

    private static EmailAddress An(string address) => EmailAddress.Create(address);

    [Fact]
    public void A_new_family_has_no_members()
    {
        Family family = AFamily();

        family.Id.ShouldNotBe(Guid.Empty);
        family.Name.Value.ShouldBe("Jansen");
        family.Members.ShouldBeEmpty();
    }

    [Fact]
    public void A_family_created_with_an_id_keeps_that_id()
        => Family.Create(KnownFamilies.Verburgh, FamilyName.Create("Verburgh")).Id.ShouldBe(KnownFamilies.Verburgh);

    [Fact]
    public void A_family_cannot_be_created_with_an_empty_id()
        => Should.Throw<ArgumentException>(() => Family.Create(Guid.Empty, FamilyName.Create("Verburgh")));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_family_needs_a_name(string name)
        => Should.Throw<DomainValidationException>(() => FamilyName.Create(name));

    [Fact]
    public void A_family_name_is_at_most_60_characters()
    {
        FamilyName.Create(new string('a', 60)).Value.Length.ShouldBe(60);
        Should.Throw<DomainValidationException>(() => FamilyName.Create(new string('a', 61)));
    }

    [Fact]
    public void Renaming_keeps_the_members()
    {
        Family family = AFamily();
        family.AddMember(An("oma.jansen@gmail.com"));

        family.Rename(FamilyName.Create("Familie Jansen"));

        family.Name.Value.ShouldBe("Familie Jansen");
        family.Members.Count.ShouldBe(1);
    }

    [Fact]
    public void Adding_a_member_links_it_to_the_family()
    {
        Family family = AFamily();

        Member member = family.AddMember(An("Oma.Jansen@Gmail.com"));

        member.FamilyId.ShouldBe(family.Id);
        member.Email.Value.ShouldBe("oma.jansen@gmail.com");
        family.HasMember(An("oma.jansen@gmail.com")).ShouldBeTrue();
    }

    [Fact]
    public void The_same_address_cannot_be_added_twice()
    {
        Family family = AFamily();
        family.AddMember(An("oma.jansen@gmail.com"));

        Should.Throw<DomainValidationException>(() => family.AddMember(An("OMA.JANSEN@gmail.com")));
    }

    [Fact]
    public void Removing_a_member_takes_it_out()
    {
        Family family = AFamily();
        family.AddMember(An("oma.jansen@gmail.com"));

        family.RemoveMember(An("oma.jansen@gmail.com"));

        family.Members.ShouldBeEmpty();
    }

    [Fact]
    public void Removing_someone_who_is_not_a_member_is_not_found()
        => Should.Throw<NotFoundException>(() => AFamily().RemoveMember(An("vreemde@gmail.com")));
}
