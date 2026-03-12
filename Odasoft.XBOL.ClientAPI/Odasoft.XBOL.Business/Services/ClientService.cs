using Odasoft.XBOL.Commons.Enums;
using Odasoft.XBOL.Commons.Responses;
using Odasoft.XBOL.Data.Repositories;
using Odasoft.XBOL.DTO;

namespace Odasoft.XBOL.Business.Services
{
    public class ClientService
    {
        private readonly OrderRepository _orderRepository;

        private const int MIN_PAGE = 1;
        private const int MAX_PAGE = 50;

        public ClientService(OrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public async Task<PagedResponse<MyEventDTO>> GetMyEventsAsync(
            int? page,
            int? pageSize,
            OrderType orderType,
            long idClient)
        {
            return await _orderRepository.GetMyEventsAsync(
                page ?? MIN_PAGE,
                pageSize ?? MAX_PAGE,
                orderType,
                idClient);
        }

        public async Task<MyEventDetailDTO?> GetMyEventDetailAsync(long clientId, long eventId)
        {
            return await _orderRepository.GetMyEventDetailAsync(clientId, eventId);
        }

        public async Task<PagedResponse<MyTicketDTO>> GetMyTicketsByOrderAsync(
            int? page,
            int? pageSize,
            long eventId,
            long orderId)
        {
            return await _orderRepository.GetMyTicketsByOrderAsync(
                page ?? MIN_PAGE,
                pageSize ?? MAX_PAGE,
                eventId,
                orderId);
        }
    }
}
