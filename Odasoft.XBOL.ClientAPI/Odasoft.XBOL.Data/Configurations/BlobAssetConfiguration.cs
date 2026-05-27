using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Data.Configurations
{
    public class BlobAssetConfiguration : IEntityTypeConfiguration<BlobAsset>
    {
        public void Configure(EntityTypeBuilder<BlobAsset> builder)
        {
            builder.ToTable("BlobAssets");
            builder.Property(x => x.BucketName).HasMaxLength(255);
            builder.Property(x => x.ObjectName).HasMaxLength(1024);
            builder.Property(x => x.FileName).HasMaxLength(255);
            builder.Property(x => x.ContentType).HasMaxLength(255);
            builder.Property(x => x.Url).HasMaxLength(2048);
            builder.Property(x => x.Status).HasConversion<int>();
        }
    }
}
