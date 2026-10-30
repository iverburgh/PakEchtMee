using Shared.Validation;
using Shouldly;
using Xunit;

namespace Families.Domain.Tests;

public sealed class EmailAddressTests
{
    [Fact]
    public void An_address_is_stored_lower_case_and_trimmed()
        => EmailAddress.Create("  IvoVerburgh@Gmail.com ").Value.ShouldBe("ivoverburgh@gmail.com");

    [Fact]
    public void Addresses_differing_only_in_case_are_equal()
        => EmailAddress.Create("IvoVerburgh@Gmail.com").ShouldBe(EmailAddress.Create("ivoverburgh@gmail.com"));

    [Theory]
    [InlineData("oma.jansen")]
    [InlineData("oma.jansen@")]
    [InlineData("oma@localhost")]
    [InlineData("Oma <oma@gmail.com>")]
    [InlineData("oma jansen@gmail.com")]
    public void An_invalid_address_is_rejected(string value)
        => Should.Throw<DomainValidationException>(() => EmailAddress.Create(value));

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void A_missing_address_is_rejected(string? value)
        => Should.Throw<DomainValidationException>(() => EmailAddress.Create(value));
}
