using Odasoft.XBOL.Commons.Enums;

namespace Odasoft.XBOL.Models
{
    public class BlobAsset : BaseModel
    {
        public string BucketName { get; set; } = "";
        public string ObjectName { get; set; } = "";
        public string FileName { get; set; } = "";
        public string ContentType { get; set; } = "";
        public long SizeBytes { get; set; }
        public string? Url { get; set; }
        public BlobAssetStatus Status { get; set; }
        public int CleanupAttempts { get; set; }
        public string? LastError { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
    }
}
