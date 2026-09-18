using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Trips.Domain;

namespace Trips.Infrastructure.Sql.Configurations;

internal sealed class TripConfiguration : IEntityTypeConfiguration<Trip>
{
    public void Configure(EntityTypeBuilder<Trip> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Trips");
        builder.HasKey(trip => trip.Id);
        builder.Property(trip => trip.Id).ValueGeneratedNever();

        builder.Property(trip => trip.Name)
            .HasConversion(name => name.Value, value => TripName.Create(value))
            .HasMaxLength(TripName.MaxLength)
            .IsRequired();

        builder.OwnsOne(trip => trip.Dates, dates =>
        {
            dates.Property(value => value.Start).HasColumnName("StartDate");
            dates.Property(value => value.End).HasColumnName("EndDate");
        });
        builder.Navigation(trip => trip.Dates).IsRequired();

        builder.HasMany(trip => trip.Categories)
            .WithOne()
            .HasForeignKey(category => category.TripId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(trip => trip.Categories)
            .HasField("categories")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(trip => trip.Items)
            .WithOne()
            .HasForeignKey(item => item.TripId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(trip => trip.Items)
            .HasField("items")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
