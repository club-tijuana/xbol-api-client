using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Odasoft.XBOL.Data.Extensions;
using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Data.Configurations
{
    public class ClientConfiguration : IEntityTypeConfiguration<Client>
    {
        public void Configure(EntityTypeBuilder<Client> builder)
        {
            builder.Property(x => x.PhoneNumber)
                    .IsRequired()
                    .HasMaxLength(15)
                    .HasConversion<PhoneSanitizerConverter>();

            builder.Property(x => x.PhoneRegionCodeId)
                    .IsRequired();

            builder.HasIndex(x => new { x.PhoneRegionCodeId, x.PhoneNumber })
                    .IsUnique();

            builder.HasIndex(x => x.FirebaseUid)
                   .IsUnique()
                   .HasFilter("\"FirebaseUid\" IS NOT NULL");
        }
    }
}