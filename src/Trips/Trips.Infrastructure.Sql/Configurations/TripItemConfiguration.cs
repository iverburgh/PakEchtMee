using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Trips.Domain;

namespace Trips.Infrastructure.Sql.Configurations;

internal sealed class TripItemConfiguration : IEntityTypeConfiguration<TripItem>
{
    public void Configure(EntityTypeBuilder<TripItem> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("TripItems");
        builder.HasKey(item => item.Id);

        // The domain assigns the id, so EF must not read "key is set" as "row already exists" and issue an UPDATE.
        builder.Property(item => item.Id).ValueGeneratedNever();

        builder.Property(item => item.CategoryName).HasMaxLength(60).IsRequired();
        builder.Property(item => item.Name).HasMaxLength(100).IsRequired();
        builder.Property(item => item.Status).HasConversion<int>();

        builder.OwnsOne(item => item.Amount, amount =>
        {
            amount.Property(value => value.Quantity).HasColumnName("Quantity");
            amount.Property(value => value.Unit).HasColumnName("Unit").HasMaxLength(Amount.MaxUnitLength);
        });

        // Two overlapping taps in the walkthrough must fail loudly instead of silently losing one.
        builder.Property<byte[]>("RowVersion").IsRowVersion();

        builder.HasIndex(item => new { item.TripId, item.SourceItemId }).IsUnique();
        builder.HasIndex(item => new { item.TripId, item.SourceCategoryId });
    }
}
