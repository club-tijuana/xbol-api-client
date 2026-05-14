using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasIndex(x => x.FirebaseUid)
                   .IsUnique()
                   .HasFilter("\"FirebaseUid\" IS NOT NULL");

            builder.HasOne(x => x.OrganizerMember)
                   .WithOne(x => x.User)
                   .HasForeignKey<OrganizerMember>(x => x.UserId);
        }
    }
}
