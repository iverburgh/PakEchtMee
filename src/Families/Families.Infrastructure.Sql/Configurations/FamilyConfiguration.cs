using Families.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Families.Infrastructure.Sql.Configurations;

internal sealed class FamilyConfiguration : IEntityTypeConfiguration<Family>
{
    /// <summary>Case- and accent-insensitive, like the catalog names, so "Verburgh" and "verburgh" collide in the database too.</summary>
    internal const string NameCollation = "Latin1_General_CI_AI";

    public void Configure(EntityTypeBuilder<Family> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Families");
        builder.HasKey(family => family.Id);
        builder.Property(family => family.Id).ValueGeneratedNever();

        builder.Property(family => family.Name)
            .HasConversion(name => name.Value, value => FamilyName.Create(value))
            .HasMaxLength(FamilyName.MaxLength)
            .UseCollation(NameCollation)
            .IsRequired();

        builder.HasIndex(family => family.Name).IsUnique();

        builder.OwnsOne(family => family.Composition, composition =>
        {
            composition.Property(value => value.Adults).HasColumnName("Adults");
            composition.Property(value => value.Children).HasColumnName("Children");
        });
        builder.Navigation(family => family.Composition).IsRequired();

        builder.HasMany(family => family.Members)
            .WithOne()
            .HasForeignKey(member => member.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(family => family.Members)
            .HasField("members")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
