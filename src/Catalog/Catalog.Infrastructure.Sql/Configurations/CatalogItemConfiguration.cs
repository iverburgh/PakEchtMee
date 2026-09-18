using Catalog.Domain.Categories;
using Catalog.Domain.Items;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Sql.Configurations;

internal sealed class CatalogItemConfiguration : IEntityTypeConfiguration<CatalogItem>
{
    public void Configure(EntityTypeBuilder<CatalogItem> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Items");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();

        builder.Property(item => item.Name)
            .HasConversion(name => name.Value, value => ItemName.Create(value))
            .HasMaxLength(ItemName.MaxLength)
            .UseCollation(CategoryConfiguration.NameCollation)
            .IsRequired();

        builder.OwnsOne(item => item.Amount, amount =>
        {
            amount.Property(value => value.Quantity).HasColumnName("Quantity");
            amount.Property(value => value.Unit).HasColumnName("Unit").HasMaxLength(Amount.MaxUnitLength);
        });

        builder.Property(item => item.DeletedAt);

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(item => item.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(item => new { item.CategoryId, item.Name })
            .IsUnique()
            .HasFilter("[DeletedAt] IS NULL");

        builder.HasIndex(item => item.DeletedAt);
    }
}
