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
            var normalizedRequestPhone = NormalizePhoneNumber(request.Phone);
            if (normalizedRequestPhone.Length == 0)
            {
                return null;
            }

            var normalizedRequestPhoneWithCode = AddPhoneCode(normalizedRequestPhone, request.PhoneCode);

            var candidates = await _clientRepository.Get(
                    filter: client => client.PhoneNumber != null
                    && client.FirebaseUid != null
                    && client.Email.ToUpper().Equals(upperEmail)
                )
                .OrderByDescending(client => client.Id)
                .Select(client => new ClientDTO
                {
                    Id = client.Id,
                    UserId = client.FirebaseUid ?? string.Empty,
                    FullName = client.FullName ?? "",
                    BusinessName = client.BusinessName,
                    Email = client.Email,
                    PhoneNumber = client.PhoneNumber,
                    PhoneCode = client.PhoneRegionCode != null ? client.PhoneRegionCode.DialCode : string.Empty
                })
                .ToListAsync();

            var client = candidates.FirstOrDefault(client =>
                PhoneNumbersMatch(
                    client.PhoneNumber,
                    client.PhoneCode,
                    normalizedRequestPhone,
                    normalizedRequestPhoneWithCode));

            return client;
        }

        private static bool PhoneNumbersMatch(
            string phoneNumber,
            string phoneCode,
            string normalizedRequestPhone,
            string normalizedRequestPhoneWithCode)
        {
            var normalizedClientPhone = NormalizePhoneNumber(phoneNumber);
            if (normalizedClientPhone.Length == 0)
            {
                return false;
            }

            var normalizedClientPhoneWithCode = AddPhoneCode(normalizedClientPhone, phoneCode);

            return normalizedClientPhone == normalizedRequestPhone
                || normalizedClientPhone == normalizedRequestPhoneWithCode
                || normalizedClientPhoneWithCode == normalizedRequestPhone
                || normalizedClientPhoneWithCode == normalizedRequestPhoneWithCode;
        }

        private static string AddPhoneCode(string normalizedPhoneNumber, string? phoneCode)
        {
            var normalizedPhoneCode = NormalizePhoneNumber(phoneCode);
            if (normalizedPhoneNumber.Length == 0
                || normalizedPhoneCode.Length == 0
                || normalizedPhoneNumber.StartsWith(normalizedPhoneCode))
            {
                return normalizedPhoneNumber;
            }

            return normalizedPhoneCode + normalizedPhoneNumber;
        }

        private static string NormalizePhoneNumber(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return new string(value.Where(char.IsDigit).ToArray());
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

        public async Task<MyEventDetailDTO?> GetMyEventDetailAsync(long clientId, long eventId, long orderId)
        {
            return await _orderRepository.GetMyEventDetailAsync(clientId, eventId, orderId);
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
