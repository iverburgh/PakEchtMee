using Families.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Families.Infrastructure.Sql.Configurations;

internal sealed class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Members");

        // The address is the key: it makes "one family per account" a database guarantee, not only an application check.
        builder.HasKey(member => member.Email);
        builder.Property(member => member.Email)
            .HasConversion(email => email.Value, value => EmailAddress.Create(value))
            .HasMaxLength(EmailAddress.MaxLength)
            .ValueGeneratedNever();

        builder.HasIndex(member => member.FamilyId);
    }
}
