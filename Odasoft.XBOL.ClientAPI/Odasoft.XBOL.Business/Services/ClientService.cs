using Odasoft.XBOL.Commons.Requests.Filters;
using Odasoft.XBOL.Commons.Responses;
using Odasoft.XBOL.Data.Repositories;
using Odasoft.XBOL.DTO;

namespace Odasoft.XBOL.Business.Services
{
    public class ClientService
    {
        private readonly ClientRepository _clientRepository;

        public ClientService(ClientRepository clientRepository)
        {
            _clientRepository = clientRepository;
        }

        public async Task<PagedResponse<MyEventDTO>> GetMyEventsAsync(TicketsFilters filters, long idClient)
        {
            filters.Page = Math.Max(filters.Page, 1);
            filters.PageSize = Math.Clamp(filters.PageSize, 1, 50);

            (List<MyEventDTO> result, int totalCount) = await _clientRepository.GetMyEventsAsync(filters, idClient);

            return new PagedResponse<MyEventDTO>
            {
                Items = result,
                CurrentPage = filters.Page,
                PageSize = filters.PageSize,
                TotalItems = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)filters.PageSize)
            };
        }

        public async Task<MyEventDetailDTO?> GetMyEventDetailAsync(long clientId, long eventId)
        {
            return await _clientRepository.GetMyEventDetailAsync(clientId, eventId);
        }

        public async Task<PagedResponse<MyTicketDTO>> GetMyTicketsByOrderAsync(TicketsFilters filters)
        {
            filters.Page = Math.Max(filters.Page, 1);
            filters.PageSize = Math.Clamp(filters.PageSize, 1, 50);

            (List<MyTicketDTO> result, int totalCount) = await _clientRepository.GetMyTicketsByOrderAsync(filters);

            return new PagedResponse<MyTicketDTO>
            {
                Items = result,
                CurrentPage = filters.Page,
                PageSize = filters.PageSize,
                TotalItems = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)filters.PageSize)
            };
        }
    }
}
