using Catalog.Domain.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Sql.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    /// <summary>Case- and accent-insensitive collation, so "Kaarsen" and "Käarsen" collide in the database as well.</summary>
    internal const string NameCollation = "Latin1_General_CI_AI";

    public void Configure(EntityTypeBuilder<Category> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Categories");
        builder.HasKey(category => category.Id);
        builder.Property(category => category.Id).ValueGeneratedNever();

        builder.Property(category => category.Name)
            .HasConversion(name => name.Value, value => CategoryName.Create(value))
            .HasMaxLength(CategoryName.MaxLength)
            .UseCollation(NameCollation)
            .IsRequired();

        builder.Property(category => category.DeletedAt);
        builder.Property(category => category.FamilyId);

        // Unique per scope. SQL Server treats NULLs as equal in a unique index, so general names (FamilyId NULL)
        // are unique among themselves exactly like each family's own names.
        builder.HasIndex(category => new { category.FamilyId, category.Name })
            .IsUnique()
            .HasFilter("[DeletedAt] IS NULL");
    }
}
