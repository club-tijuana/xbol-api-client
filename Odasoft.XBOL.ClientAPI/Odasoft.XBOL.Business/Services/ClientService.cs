using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using Odasoft.XBOL.Commons.Enums;
using Odasoft.XBOL.Commons.Requests;
using Odasoft.XBOL.Commons.Responses;
using Odasoft.XBOL.Data.Repositories;
using Odasoft.XBOL.DTO;
using Odasoft.XBOL.Models;
using PhoneNumbers;

namespace Odasoft.XBOL.Business.Services
{
    public class ClientService
    {
        private readonly OrderRepository _orderRepository;
        private readonly ClientRepository _clientRepository;
        private readonly ClientLoginIdentifierRepository _clientLoginIdentifierRepository;

        private const int MIN_PAGE = 1;
        private const int MAX_PAGE = 50;
        private static readonly PhoneNumberUtil PhoneNumberParser = PhoneNumberUtil.GetInstance();

        public ClientService(
            OrderRepository orderRepository,
            ClientRepository clientRepository,
            ClientLoginIdentifierRepository clientLoginIdentifierRepository)
        {
            _orderRepository = orderRepository;
            _clientRepository = clientRepository;
            _clientLoginIdentifierRepository = clientLoginIdentifierRepository;
        }

        public async Task<ClientDTO?> GetClientByContactAsync(ClientContactRequest request)
        {
            var lookups = BuildLoginIdentifierLookups(request);
            if (lookups.Count == 0)
            {
                return null;
            }

            var matches = await _clientLoginIdentifierRepository.GetVerifiedMatchesAsync(lookups);
            var clientIds = matches
                .Select(x => x.ClientId)
                .Distinct()
                .ToList();

            if (clientIds.Count != 1)
            {
                return null;
            }

            return ToDto(matches.First(x => x.ClientId == clientIds[0]).Client);
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

        private static IReadOnlyList<ClientLoginIdentifierLookup> BuildLoginIdentifierLookups(ClientContactRequest request)
        {
            var lookups = new List<ClientLoginIdentifierLookup>();

            if (TryNormalizeEmailIdentifier(request.Email, out var email))
            {
                lookups.Add(new ClientLoginIdentifierLookup(
                    ClientLoginIdentifierType.Email,
                    email));
            }

            if (TryNormalizePhoneIdentifier(request, out var phone))
            {
                lookups.Add(new ClientLoginIdentifierLookup(
                    ClientLoginIdentifierType.Phone,
                    phone));
            }

            return lookups;
        }

        private static bool TryNormalizeEmailIdentifier(string? value, out string email)
        {
            email = string.Empty;
            if (string.IsNullOrWhiteSpace(value)
                || !value.Contains('@', StringComparison.Ordinal))
            {
                return false;
            }

            try
            {
                var trimmed = value.Trim();
                var address = new MailAddress(trimmed);
                if (!string.Equals(address.Address, trimmed, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                email = address.Address.ToLowerInvariant();
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static bool TryNormalizePhoneIdentifier(ClientContactRequest request, out string phone)
        {
            phone = string.Empty;
            if (string.IsNullOrWhiteSpace(request.Phone))
            {
                return false;
            }

            var raw = request.Phone.Trim();
            var region = raw.StartsWith("+", StringComparison.Ordinal)
                ? null
                : TryResolvePhoneRegion(request);
            if (region is null && !raw.StartsWith("+", StringComparison.Ordinal))
            {
                return false;
            }

            try
            {
                var parsed = PhoneNumberParser.Parse(raw, region);
                if (!PhoneNumberParser.IsValidNumber(parsed))
                {
                    return false;
                }

                phone = PhoneNumberParser.Format(parsed, PhoneNumberFormat.E164);
                return true;
            }
            catch (NumberParseException)
            {
                return false;
            }
        }

        private static string? TryResolvePhoneRegion(ClientContactRequest request)
        {
            return TryNormalizePhoneRegionCode(request.PhoneIsoCode)
                ?? TryNormalizePhoneRegionCode(request.PhoneCode);
        }

        private static string? TryNormalizePhoneRegionCode(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var region = value.Trim().ToUpperInvariant();
            return PhoneNumberParser.GetCountryCodeForRegion(region) > 0 ? region : null;
        }

        private static ClientDTO ToDto(Client client)
        {
            return new ClientDTO
            {
                Id = client.Id,
                FirebaseUid = client.FirebaseUid ?? string.Empty,
                FullName = client.FullName ?? string.Empty,
                BusinessName = client.BusinessName,
                Email = client.Email,
                PhoneNumber = client.PhoneNumber,
                PhoneCode = client.PhoneRegionCode?.DialCode ?? string.Empty
            };
        }
    }
}
