namespace Odasoft.XBOL.DTO
{
    /// <summary>
    /// Map of image slot to resolved value. Each property returns a data URI
    /// (when an EventImage upload exists) or the legacy URL, or null.
    /// </summary>
    public class EventImagesDTO
    {
        public string? Horizontal { get; set; }
        public string? Vertical { get; set; }
    }
}
