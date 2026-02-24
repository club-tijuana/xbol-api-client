using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Data.Configurations
{
    public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
    {
        public void Configure(EntityTypeBuilder<Ticket> builder)
        {
            builder.HasOne(a => a.OriginalClient)
                   .WithMany(b => b.Tickets)
                   .HasForeignKey(b => b.OriginalClientId);

            builder.HasOne(a => a.CurrentClient)
                   .WithMany()
                   .HasForeignKey(b => b.CurrentClientId);

            builder.HasOne(x => x.SeasonPassEventTicket)
                   .WithOne(x => x.Ticket)
                   .HasForeignKey<SeasonPassEventTicket>(x => x.TicketId);
        }
    }
}
