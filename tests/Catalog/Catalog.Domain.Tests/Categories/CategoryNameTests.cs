using Catalog.Domain.Categories;
using Shared.Validation;
using Shouldly;
using Xunit;

namespace Catalog.Domain.Tests.Categories;

public sealed class CategoryNameTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejects_blank_names(string? value)
        => Should.Throw<DomainValidationException>(() => CategoryName.Create(value));

    [Fact]
    public void Trims_the_name()
        => CategoryName.Create("  Toiletries  ").Value.ShouldBe("Toiletries");

    [Fact]
    public void Accepts_the_maximum_length()
        => CategoryName.Create(new string('a', CategoryName.MaxLength)).Value.Length.ShouldBe(CategoryName.MaxLength);

    [Fact]
    public void Rejects_one_character_too_many()
        => Should.Throw<DomainValidationException>(() => CategoryName.Create(new string('a', CategoryName.MaxLength + 1)));

    [Fact]
    public void Normalizes_to_lower_case_for_comparison()
        => CategoryName.Create("Toiletries").Normalized.ShouldBe(CategoryName.Create("toiletries").Normalized);
}
