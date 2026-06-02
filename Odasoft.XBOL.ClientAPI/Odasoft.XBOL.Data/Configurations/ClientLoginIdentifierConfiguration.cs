using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Data.Configurations
{
    public sealed class ClientLoginIdentifierConfiguration : IEntityTypeConfiguration<ClientLoginIdentifier>
    {
        public void Configure(EntityTypeBuilder<ClientLoginIdentifier> builder)
        {
            builder.ToTable("ClientLoginIdentifier");

            builder.Property(x => x.NormalizedValue)
                .HasMaxLength(512)
                .IsRequired();

            builder.HasIndex(x => new { x.Type, x.NormalizedValue })
                .HasDatabaseName("IX_ClientLoginIdentifier_Type_NormalizedValue")
                .IsUnique();

            builder.HasOne(x => x.Client)
                .WithMany(x => x.LoginIdentifiers)
                .HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
