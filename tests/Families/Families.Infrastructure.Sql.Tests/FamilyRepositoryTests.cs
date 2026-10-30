using Families.Domain;
using Families.Infrastructure.Sql.Repositories;
using Microsoft.EntityFrameworkCore;
using Shared.Tests;
using Shouldly;
using Xunit;

namespace Families.Infrastructure.Sql.Tests;

[IntegrationTest]
public sealed class FamilyRepositoryTests(FamiliesDatabaseFixture fixture) : IClassFixture<FamiliesDatabaseFixture>
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static EmailAddress An(string address) => EmailAddress.Create(address);

    private static string Unique(string prefix) => $"{prefix} {Guid.NewGuid():N}";

    private async Task<Family> GivenFamilyAsync(string name, params string[] members)
    {
        await using FamiliesDbContext context = fixture.CreateContext();
        Family family = Family.Create(FamilyName.Create(name));

        foreach (string member in members)
            family.AddMember(An(member));

        FamilyRepository repository = new(context);
        repository.Add(family);
        await repository.SaveChangesAsync(CancellationToken);

        return family;
    }

    [Fact]
    public async Task The_migration_creates_verburgh_with_ivo_as_member()
    {
        await using FamiliesDbContext context = fixture.CreateContext();

        Family? verburgh = await new FamilyRepository(context).FindAsync(KnownFamilies.Verburgh, CancellationToken);

        verburgh.ShouldNotBeNull();
        verburgh.Name.Value.ShouldBe("Verburgh");
        verburgh.Members.Select(member => member.Email.Value).ShouldBe(["ivoverburgh@gmail.com"]);
    }

    [Fact]
    public async Task A_family_with_members_round_trips()
    {
        string email = $"{Guid.NewGuid():N}@gmail.com";
        Family created = await GivenFamilyAsync(Unique("Jansen"), email);

        await using FamiliesDbContext context = fixture.CreateContext();
        Family? found = await new FamilyRepository(context).FindAsync(created.Id, CancellationToken);

        found.ShouldNotBeNull();
        found.Name.ShouldBe(created.Name);
        found.Members.Single().Email.Value.ShouldBe(email);
    }

    [Fact]
    public async Task A_family_is_found_by_any_casing_of_a_member_address()
    {
        string email = $"{Guid.NewGuid():N}@gmail.com";
        Family created = await GivenFamilyAsync(Unique("Jansen"), email);

        await using FamiliesDbContext context = fixture.CreateContext();
        Family? found = await new FamilyRepository(context).FindByMemberAsync(An(email.ToUpperInvariant()), CancellationToken);

        found.ShouldNotBeNull();
        found.Id.ShouldBe(created.Id);
    }

    [Fact]
    public async Task An_address_outside_every_family_finds_nothing()
    {
        await using FamiliesDbContext context = fixture.CreateContext();

        Family? found = await new FamilyRepository(context).FindByMemberAsync(An($"{Guid.NewGuid():N}@gmail.com"), CancellationToken);

        found.ShouldBeNull();
    }

    [Fact]
    public async Task The_database_rejects_an_address_in_a_second_family()
    {
        string email = $"{Guid.NewGuid():N}@gmail.com";
        await GivenFamilyAsync(Unique("Jansen"), email);

        await Should.ThrowAsync<DbUpdateException>(() => GivenFamilyAsync(Unique("De Vries"), email));
    }

    [Fact]
    public async Task Names_are_compared_case_insensitively()
    {
        string name = Unique("Jansen");
        Family created = await GivenFamilyAsync(name);

        await using FamiliesDbContext context = fixture.CreateContext();
        FamilyRepository repository = new(context);

        (await repository.ExistsWithNameAsync(FamilyName.Create(name.ToUpperInvariant()), null, CancellationToken)).ShouldBeTrue();
        (await repository.ExistsWithNameAsync(FamilyName.Create(name), created.Id, CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    public async Task Removing_a_member_is_persisted()
    {
        string email = $"{Guid.NewGuid():N}@gmail.com";
        Family created = await GivenFamilyAsync(Unique("Jansen"), email);

        await using (FamiliesDbContext context = fixture.CreateContext())
        {
            FamilyRepository repository = new(context);
            Family family = (await repository.FindAsync(created.Id, CancellationToken))!;
            family.RemoveMember(An(email));
            await repository.SaveChangesAsync(CancellationToken);
        }

        await using FamiliesDbContext check = fixture.CreateContext();
        (await new FamilyRepository(check).FindByMemberAsync(An(email), CancellationToken)).ShouldBeNull();
    }
}
