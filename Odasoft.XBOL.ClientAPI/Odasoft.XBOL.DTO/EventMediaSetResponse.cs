namespace Odasoft.XBOL.DTO
{
    public sealed class EventMediaSetResponse
    {
        public MediaResponse? Banner { get; set; }
        public MediaResponse? Logo { get; set; }
        public List<MediaResponse> Sponsors { get; set; } = [];
        public List<MediaResponse> Gallery { get; set; } = [];
    }
}
