namespace Odasoft.XBOL.DTO
{
    public class SeasonItemDTO
    {
        public long Id { get; set; }
        public string BannerImageUrl { get; set; } = string.Empty;
        public DateTimeOffset StartDate { get; set; }
        public string ExternalSeasonKey { get; set; } = string.Empty;
        public EventMediaSetResponse? Media { get; set; }
        public bool IsRenewal { get; set; }
        public bool IsPreSale { get; set; }
        public bool IsGeneralSale { get; set; }
    }
}
