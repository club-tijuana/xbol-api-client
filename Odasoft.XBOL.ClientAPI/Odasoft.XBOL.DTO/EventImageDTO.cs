using Odasoft.XBOL.Commons.Enums;

namespace Odasoft.XBOL.DTO
{
    public class EventImageDTO
    {
        public ImageType ImageType { get; set; }
        public byte[] Content { get; set; } = [];
        public string ContentType { get; set; } = "";
        public string FileName { get; set; } = "";
    }
}
