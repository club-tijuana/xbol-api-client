namespace Odasoft.XBOL.Commons.Responses
{
    public class SyncFavoritesResponse
    {
        public long TotalReceived { get; set; }
        public long Inserted { get; set; }
        public long AlreadyExists { get; set; }
    }
}
