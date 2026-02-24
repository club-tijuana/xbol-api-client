using System.Text.Json.Serialization;

namespace Odasoft.XBOL.DTO
{
    public class FaqDTO
    {
        [JsonPropertyName("question")]
        public string Question { get; set; } = string.Empty;
        [JsonPropertyName("answer")]
        public string Answer { get; set; } = string.Empty;
    }
}
