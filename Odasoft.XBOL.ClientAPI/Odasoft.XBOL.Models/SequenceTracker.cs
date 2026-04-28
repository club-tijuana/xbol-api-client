namespace Odasoft.XBOL.Models
{
    public class SequenceTracker : BaseModel
    {
        public string SequenceKey { get; set; } = "";
        public long LastValue { get; set; }
    }
}
