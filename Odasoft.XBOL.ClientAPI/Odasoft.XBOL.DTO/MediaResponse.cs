using Odasoft.XBOL.Commons.Enums;

namespace Odasoft.XBOL.DTO
{
    public class MediaResponse
    {
        public long Id { get; set; }
        public string? Url { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public ClientMediaType MediaType { get; set; }
        public int Order { get; set; }
    }
}
