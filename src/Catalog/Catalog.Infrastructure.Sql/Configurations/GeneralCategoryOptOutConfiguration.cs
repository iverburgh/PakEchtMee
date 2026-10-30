using Catalog.Domain.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Sql.Configurations;

internal sealed class GeneralCategoryOptOutConfiguration : IEntityTypeConfiguration<GeneralCategoryOptOut>
{
    public void Configure(EntityTypeBuilder<GeneralCategoryOptOut> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("FamilyCategoryOptOuts");
        builder.HasKey(optOut => new { optOut.FamilyId, optOut.CategoryId });

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(optOut => optOut.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
