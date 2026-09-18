using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Trips.Domain;

namespace Trips.Infrastructure.Sql.Configurations;

internal sealed class TripCategoryConfiguration : IEntityTypeConfiguration<TripCategory>
{
    public void Configure(EntityTypeBuilder<TripCategory> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("TripCategories");
        builder.HasKey(category => new { category.TripId, category.CategoryId });

        builder.Property(category => category.Name).HasMaxLength(60).IsRequired();
    }
}
