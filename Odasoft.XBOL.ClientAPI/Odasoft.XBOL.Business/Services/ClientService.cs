using Microsoft.EntityFrameworkCore;
using Odasoft.XBOL.Commons.Enums;
using Odasoft.XBOL.Commons.Requests;
using Odasoft.XBOL.Commons.Responses;
using Odasoft.XBOL.Data.Repositories;
using Odasoft.XBOL.DTO;

namespace Odasoft.XBOL.Business.Services
{
    public class ClientService
    {
        private readonly OrderRepository _orderRepository;
        private readonly ClientRepository _clientRepository;

        private const int MIN_PAGE = 1;
        private const int MAX_PAGE = 50;

        public ClientService(
            OrderRepository orderRepository,
            ClientRepository clientRepository)
        {
            _orderRepository = orderRepository;
            _clientRepository = clientRepository;
        }

        public async Task<ClientDTO?> GetClientByContactAsync(ClientContactRequest request)
        {
            string upperEmail = request.Email.ToUpper().Trim();

            var client = await _clientRepository.Get(
                    filter: client => client.User != null
                    && client.User.PhoneNumber != null
                    && client.Email.ToUpper().Equals(upperEmail)
                    && client.User.PhoneNumber.Equals(request.Phone),
                    includedProperties: [
                        "User"
                    ]
                )
                .Select(client => new ClientDTO
                {
                    Id = client.Id,
                    UserId = client.UserId != null ? client.UserId.Value.ToString() : string.Empty,
                    FullName = client.FullName ?? "",
                    BusinessName = client.BusinessName,
                    Email = client.Email,
                    PhoneNumber = client.PhoneNumber,
                    PhoneCode = client.PhoneRegionCode != null ? client.PhoneRegionCode.DialCode : string.Empty
                })
                .FirstOrDefaultAsync();

            return client;
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
            long orderId,
            long clientId)
        {
            return await _orderRepository.GetMyTicketsByOrderAsync(
                page ?? MIN_PAGE,
                pageSize ?? MAX_PAGE,
                eventId,
                orderId,
                clientId);
        }
    }
}
