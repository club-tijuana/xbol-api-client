using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Data.Configurations
{
    public class EventConfiguration : IEntityTypeConfiguration<Event>
    {
        public void Configure(EntityTypeBuilder<Event> builder)
        {
            builder.HasMany(e => e.Categories)
               .WithMany(c => c.Events)
               .UsingEntity(j => j.ToTable("EventEventCategory"));

            builder.Property(e => e.ViewCount)
                .HasDefaultValue(0);
        }
    }
}
