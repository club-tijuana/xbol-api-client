using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Data.Configurations
{
    public class BundleSeatConfiguration : IEntityTypeConfiguration<BundleSeat>
    {
        public void Configure(EntityTypeBuilder<BundleSeat> builder)
        {
            builder.Property(bs => bs.ForSale)
                .HasDefaultValue(true);

            builder.HasOne(bs => bs.BundleSection)
                .WithMany(bsec => bsec.BundleSeats)
                .HasForeignKey(bs => bs.BundleSectionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(bs => bs.BaseSeat)
                .WithMany()
                .HasForeignKey(bs => bs.BaseSeatId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
