using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Data.Configurations
{
    public class BundleEventScheduleConfiguration : IEntityTypeConfiguration<BundleEventSchedule>
    {
        public void Configure(EntityTypeBuilder<BundleEventSchedule> builder)
        {
            builder.HasKey(bes => new { bes.BundleId, bes.EventScheduleId });

            builder.HasOne(bes => bes.EventSchedule)
                .WithMany()
                .HasForeignKey(bes => bes.EventScheduleId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
