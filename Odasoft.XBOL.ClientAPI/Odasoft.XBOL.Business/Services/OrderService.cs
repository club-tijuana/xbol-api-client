using Odasoft.XBOL.Data.Repositories;
using Odasoft.XBOL.DTO;

namespace Odasoft.XBOL.Business.Services
{
    public class OrderService
    {
        private readonly OrderRepository _orderRepository;

        public OrderService(OrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public async Task<OrderDTO?> GetOrderAsync(long clientId, long orderId)
        {
            return await _orderRepository.GetOrderAsync(clientId, orderId);
        }
    }
}
