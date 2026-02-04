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

        public async Task<PagedResponse<MyEventTicketDTO>> GetMyEventTicketsAsync(TicketsFilters filters, long idClient)
        {
            filters.Page = Math.Max(filters.Page, 1);
            filters.PageSize = Math.Clamp(filters.PageSize, 1, 50);

            (List<MyEventTicketDTO> result, int totalCount) = await _clientRepository.GetMyEventTicketsAsync(filters, idClient);

            return new PagedResponse<MyEventTicketDTO>
            {
                Items = result,
                CurrentPage = filters.Page,
                PageSize = filters.PageSize,
                TotalItems = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)filters.PageSize)
            };
        }

        public async Task<PagedResponse<MyTicketDTO>> GetMyTicketsByEventAsync(TicketsFilters filters, long idClient)
        {
            filters.Page = Math.Max(filters.Page, 1);
            filters.PageSize = Math.Clamp(filters.PageSize, 1, 50);

            (List<MyTicketDTO> result, int totalCount) = await _clientRepository.GetMyTicketsByEventAsync(filters, idClient);

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
