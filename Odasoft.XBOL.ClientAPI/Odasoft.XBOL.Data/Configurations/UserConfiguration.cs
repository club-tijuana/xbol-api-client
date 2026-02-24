using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasOne(x => x.Client)
                   .WithOne(x => x.User)
                   .HasForeignKey<Client>(x => x.UserId);

            builder.HasOne(x => x.OrganizerMember)
                   .WithOne(x => x.User)
                   .HasForeignKey<OrganizerMember>(x => x.UserId);
        }
    }
}
