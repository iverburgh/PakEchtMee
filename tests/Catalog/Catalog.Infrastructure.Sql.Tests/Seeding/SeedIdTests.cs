using Catalog.Infrastructure.Sql.Seeding;
using Shouldly;
using Xunit;

namespace Catalog.Infrastructure.Sql.Tests.Seeding;

public sealed class SeedIdTests
{
    [Fact]
    public void The_same_key_always_gives_the_same_id()
        => SeedId.For("category:Tent").ShouldBe(SeedId.For("category:Tent"));

    [Fact]
    public void Different_keys_give_different_ids()
        => SeedId.For("category:Tent").ShouldNotBe(SeedId.For("category:Was"));

    [Fact]
    public void An_id_is_never_empty()
        => SeedId.For("category:Tent").ShouldNotBe(Guid.Empty);

    [Fact]
    public void An_id_is_a_version_5_uuid()
        => SeedId.For("category:Tent").Version.ShouldBe(5);
}
