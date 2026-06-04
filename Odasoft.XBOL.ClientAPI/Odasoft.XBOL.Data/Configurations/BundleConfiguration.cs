using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Data.Configurations
{
    public class BundleConfiguration : IEntityTypeConfiguration<Bundle>
    {
        public void Configure(EntityTypeBuilder<Bundle> builder)
        {
            builder.ToTable("Bundle");

            builder.HasMany(b => b.BundleEventSchedules)
                .WithOne(bes => bes.Bundle)
                .HasForeignKey(bes => bes.BundleId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(b => b.BundleSections)
                .WithOne(bs => bs.Bundle)
                .HasForeignKey(bs => bs.BundleId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(b => b.PreviousBundle)
                .WithMany()
                .HasForeignKey(b => b.PreviousBundleId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
