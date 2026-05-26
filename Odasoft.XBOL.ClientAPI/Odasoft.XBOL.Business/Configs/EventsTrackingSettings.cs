namespace Odasoft.XBOL.Business.Configs
{
    public class EventsTrackingSettings
    {
        public long ViewDuplicateMinutes { get; set; }
        public long IpRateLimitMinutes { get; set; }
        public long MaxViewsPerIpPerMinute { get; set; }
    }
}
