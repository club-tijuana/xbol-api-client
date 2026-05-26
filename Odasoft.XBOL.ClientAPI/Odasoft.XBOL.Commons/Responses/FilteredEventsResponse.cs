namespace Odasoft.XBOL.Commons.Responses
{
    public class FilteredEventsResponse<P, E>
    {
        public List<P> Performers { get; set; } = new List<P>();
        public required PagedResponse<E> PagedEvents { get; set; }
    }
}
