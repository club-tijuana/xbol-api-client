using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Odasoft.XBOL.Commons.Enums;
using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Data.Configurations
{
    public class MediaConfiguration : IEntityTypeConfiguration<Media>
    {
        public void Configure(EntityTypeBuilder<Media> builder)
        {
            builder.HasIndex(x => new { x.ReferenceType, x.ReferenceId, x.MediaType, x.Order })
                .IsUnique()
                .HasFilter("\"DeletedAt\" IS NULL")
                .HasDatabaseName("UX_Media_ActiveReferenceTypeReferenceIdMediaTypeOrder");

            builder.HasOne(x => x.BlobAsset)
                .WithMany()
                .HasForeignKey(x => x.BlobAssetId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Navigation(x => x.BlobAsset).AutoInclude();

            builder.HasQueryFilter(x =>
                x.DeletedAt == null
                && x.BlobAsset.Status == BlobAssetStatus.Available);
        }
    }
}
