namespace Odasoft.XBOL.DTO
{
    public class BundleItemDTO
    {
        public long Id { get; set; }
        public string? BannerImageUrl { get; set; }
        public string? Name { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public string? Location { get; set; }
        public string? ExternalKey { get; set; }
        public EventMediaSetResponse? Media { get; set; }
        public bool IsRenewal { get; set; }
        public bool IsPreSale { get; set; }
        public bool IsGeneralSale { get; set; }
        public long? RelatedOrderId { get; set; }
    }
}
