using Odasoft.XBOL.Commons.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace Odasoft.XBOL.Models
{
    public class Media : BaseModel
    {
        public ClientSaleType ReferenceType { get; set; }
        public long ReferenceId { get; set; } = 0;
        public ClientMediaType MediaType { get; set; }
        public long BlobAssetId { get; set; }
        public BlobAsset BlobAsset { get; set; } = null!;
        [NotMapped]
        public string ContentType => BlobAsset?.ContentType ?? "";
        [NotMapped]
        public string FileName => BlobAsset?.FileName ?? "";
        [NotMapped]
        public string? Url => BlobAsset?.Url;
        public int Order { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public Guid UpdatedBy { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
    }
}
