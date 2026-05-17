using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Odasoft.XBOL.Commons.Enums;
using Odasoft.XBOL.Data.Repositories;
using Odasoft.XBOL.DTO;
using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Business.Services
{
    public class OrderService
    {
        private readonly OrderRepository _orderRepository;
        private readonly ClientRepository _clientRepository;
        private readonly EventScheduleRepository _eventScheduleRepository;
        private readonly EventSeatRepository _eventSeatRepository;
        private readonly TicketRepository _ticketRepository;
        private readonly SeasonPassRepository _seasonPassRepository;
        private readonly SeasonPassEventTicketRepository _seasonPassEventTicketRepository;
        private readonly SeasonRepository _seasonRepository;
        private readonly SeasonSeatRepository _seasonSeatRepository;
        private readonly EventRepository _eventRepository;
        private readonly SeasonService _seasonService;
        private readonly EventScheduleService _eventScheduleService;
        private readonly ILogger<OrderService> _logger;

        public OrderService(
            OrderRepository orderRepository,
            ClientRepository clientRepository,
            EventScheduleRepository eventScheduleRepository,
            EventSeatRepository eventSeatRepository,
            TicketRepository ticketRepository,
            SeasonPassRepository seasonPassRepository,
            SeasonPassEventTicketRepository seasonPassEventTicketRepository,
            SeasonRepository seasonRepository,
            SeasonSeatRepository seasonSeatRepository,
            EventRepository eventRepository,
            SeasonService seasonService,
            EventScheduleService eventScheduleService,
            ILogger<OrderService> logger
        )
        {
            _orderRepository = orderRepository;
            _clientRepository = clientRepository;
            _eventScheduleRepository = eventScheduleRepository;
            _eventSeatRepository = eventSeatRepository;
            _ticketRepository = ticketRepository;
            _seasonPassRepository = seasonPassRepository;
            _seasonPassEventTicketRepository = seasonPassEventTicketRepository;
            _seasonRepository = seasonRepository;
            _seasonSeatRepository = seasonSeatRepository;
            _eventRepository = eventRepository;
            _seasonService = seasonService;
            _eventScheduleService = eventScheduleService;
            _logger = logger;
        }

        public async Task<OrderDTO?> GetOrderAsync(long clientId, long orderId)
        {
            return await _orderRepository.GetOrderAsync(clientId, orderId);
        }

        public async Task<long> CreateEventOrderAsync(EventBookingRequest request)
        {
            IDbContextTransaction transaction = await _orderRepository.BeginTransactionAsync();

            try
            {
                EventSchedule schedule = await _eventScheduleRepository.Get(x => x.ExternalEventKey == request.EventKey).FirstAsync();

                var client = await UpsertClientFromOrderContactAsync(request.ClientContact);

                request.ClientContact.Id = client.Id;

                List<Ticket> tickets = await CreateTicketsAsync(request.Seats, schedule.EventId, client);

                var subtotal = (request.PaymentInfoRequest.IsCourtesy ?? false)
                                ? 0
                                : request.Seats.Sum(x => x.Value);
                var newOrder = new Order
                {
                    ClientId = client.Id,
                    UserId = null,
                    Reference = request.Localizer,
                    Status = OrderStatus.Paid,
                    SubTotal = subtotal,
                    TotalFees = 0,
                    TotalTaxes = 0,
                    Total = subtotal,
                    OrderType = OrderType.Ticket,
                    PayformType = PayformType.BoxOffice,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = Guid.Empty,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = Guid.Empty,
                    Items = [.. tickets.Select(x => new OrderItem
                    {
                        ItemType = Commons.Enums.ItemType.Ticket,
                        ItemReferenceId = x.Id,
                        Price = x.PricePaid,
                        IsCourtesy = request.PaymentInfoRequest.IsCourtesy ?? false
                    })]
                };

                await _orderRepository.InsertAsync(newOrder);
                await _orderRepository.CommitAsync();

                foreach (var ticket in tickets)
                {
                    ticket.OriginalOrderId = newOrder.Id;
                }

                await _ticketRepository.UpdateRangeAsync(tickets);

                await transaction.CommitAsync();

                return newOrder.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to create event order");
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<long> CreateSeasonOrderAsync(SeasonBookingRequest request)
        {
            IDbContextTransaction transaction = await _orderRepository.BeginTransactionAsync();

            try
            {
                Season? season = await _seasonRepository
                    .Get(x => x.ExternalSeasonKey == request.SeasonKey)
                    .SingleOrDefaultAsync();

                var client = await UpsertClientFromOrderContactAsync(request.ClientContact);

                request.ClientContact.Id = client.Id;

                // TODO: Temporarily create tickets for season passes to support ticket sharing.
                // Season passes should eventually handle sharing natively without relying on ticket entities.
                // Remove this workaround once sharing is implemented for season passes.
                List<SeasonPass> seasonPasses = await CreateSeasonPassesAsync(request.Seats, season.Id, client);
                List<Ticket> tickets = await CreateSeasonTicketsAsync(request.Seats, season.Id, client);
                await CreateSeasonPassEventTicketsAsync(seasonPasses, tickets);

                var subtotal = (request.PaymentInfoRequest.IsCourtesy ?? false)
                                ? 0
                                : request.Seats.Sum(x => x.Value);
                var newOrder = new Order
                {
                    ClientId = client.Id,
                    UserId = null,
                    Reference = request.Localizer,
                    Status = OrderStatus.Paid,
                    SubTotal = subtotal,
                    TotalFees = 0,
                    TotalTaxes = 0,
                    Total = subtotal,
                    OrderType = OrderType.SeasonPass,
                    PayformType = PayformType.BoxOffice,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = Guid.Empty,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = Guid.Empty,
                    Items = [.. seasonPasses.Select(x => new OrderItem
                    {
                        ItemType = Commons.Enums.ItemType.SeasonPass,
                        ItemReferenceId = x.Id,
                        Price = x.Price,
                        IsCourtesy = request.PaymentInfoRequest.IsCourtesy ?? false
                    })],
                    RelatedOrderId = request.ReferenceOrderId
                };

                await _orderRepository.InsertAsync(newOrder);
                await _orderRepository.CommitAsync();

                foreach (var ticket in tickets)
                {
                    ticket.OriginalOrderId = newOrder.Id;
                }

                await _ticketRepository.UpdateRangeAsync(tickets);

                await transaction.CommitAsync();

                return newOrder.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to create season pass order");
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task<Client> UpsertClientFromOrderContactAsync(ClientInfoRequest clientInfo)
        {
            var effectiveFullName = ResolveFullName(clientInfo);

            var client = await FindClientByContactAsync(clientInfo);
            if (client is not null)
            {
                ApplyOrderContact(client, clientInfo, effectiveFullName);
                await _clientRepository.UpdateAsync(client);
                return client;
            }

            client = await CreateClientAsync(clientInfo, effectiveFullName);
            return client;
        }

        private async Task<Client?> FindClientByContactAsync(ClientInfoRequest clientInfo)
        {
            if (!string.IsNullOrWhiteSpace(clientInfo.Email))
            {
                var email = clientInfo.Email.Trim().ToUpperInvariant();
                var client = await _clientRepository
                    .Get(filter: client => client.Email != null && client.Email.ToUpper().Equals(email))
                    .OrderByDescending(client => client.FirebaseUid != null)
                    .ThenByDescending(client => client.Id)
                    .FirstOrDefaultAsync();

                if (client is not null)
                {
                    return client;
                }
            }

            if (!string.IsNullOrWhiteSpace(clientInfo.PhoneNumber))
            {
                return await _clientRepository.GetByContactPhoneNumberAsync(clientInfo.PhoneNumber);
            }

            return null;
        }

        private static void ApplyOrderContact(Client client, ClientInfoRequest clientInfo, string? effectiveFullName)
        {
            if (!string.IsNullOrWhiteSpace(clientInfo.Email))
            {
                client.Email = clientInfo.Email.Trim();
            }

            if (!string.IsNullOrWhiteSpace(clientInfo.PhoneNumber))
            {
                var phoneNumber = NormalizePhoneNumber(clientInfo.PhoneNumber);
                if (phoneNumber.Length > 0 && NormalizePhoneNumber(client.PhoneNumber) != phoneNumber)
                {
                    client.PhoneNumber = phoneNumber;
                }
            }

            if (!string.IsNullOrWhiteSpace(effectiveFullName))
            {
                client.FullName = effectiveFullName;
            }

            if (clientInfo.Gender.HasValue)
            {
                client.Gender = (Gender?)clientInfo.Gender;
            }

            if (clientInfo.Birthday.HasValue)
            {
                client.DateOfBirth = clientInfo.Birthday;
            }

            client.IsActive = true;
            client.UpdatedAt = DateTime.UtcNow;
            client.UpdatedBy = Guid.Empty;
        }

        private async Task<Client> CreateClientAsync(ClientInfoRequest clientInfo, string? effectiveFullName)
        {
            var client = new Client
            {
                Email = clientInfo.Email?.Trim() ?? "",
                PhoneNumber = string.IsNullOrWhiteSpace(clientInfo.PhoneNumber)
                    ? ""
                    : NormalizePhoneNumber(clientInfo.PhoneNumber),
                FullName = effectiveFullName,
                BusinessName = clientInfo.FullName,
                DateOfBirth = clientInfo.Birthday,
                Gender = (Gender?)clientInfo.Gender,
                ClientType = ClientType.Individual,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = Guid.Empty
            };

            await _clientRepository.InsertAsync(client);
            await _clientRepository.CommitAsync();

            return client;
        }

        private static string? ResolveFullName(ClientInfoRequest clientInfo)
        {
            if (!string.IsNullOrWhiteSpace(clientInfo.FullName))
            {
                return clientInfo.FullName.Trim();
            }

            var composed = $"{clientInfo.FirstName} {clientInfo.LastName}".Trim();
            return string.IsNullOrWhiteSpace(composed) ? null : composed;
        }

        private static string NormalizePhoneNumber(string value)
        {
            return new string(value.Where(char.IsDigit).ToArray());
        }

        private async Task<List<Ticket>> CreateTicketsAsync(IDictionary<string, decimal> seats, long eventId, Client client)
        {
            List<Ticket> tickets = new List<Ticket>();

            var seatKeys = seats.Keys.ToList();
            var seatKeysSet = seatKeys.ToHashSet();

            var eventSeats = await _eventSeatRepository
                                    .Get()
                                    .Include(x => x.EventSection)
                                        .ThenInclude(x => x.EventSchedule)
                                    .AsNoTracking()
                                    .Where(x =>
                                        x.EventSection.EventSchedule.EventId == eventId &&
                                        seatKeysSet.Contains(x.ExternalSeatObjectKey)
                                    )
                                    .ToListAsync();

            var now = DateTimeOffset.UtcNow;

            foreach (var seat in eventSeats)
            {
                var ticket = new Ticket
                {
                    EventScheduleId = seat.EventSection.EventScheduleId,
                    EventSectionId = seat.EventSectionId,
                    EventSeatId = seat.Id,
                    OriginalClientId = client.Id,
                    CurrentClientId = client.Id,
                    TicketCode = seat.ExternalSeatObjectKey,
                    TicketType = "General Admission", // TODO: Define how to manage different ticket types
                    PrivateToken = Guid.NewGuid().ToString("N"), // TODO: Define the logic for the private token
                    PricePaid = seats[seat.ExternalSeatObjectKey],
                    Status = TicketStatus.Issued,
                    SeatLabelSnapshot = seat.ExternalSeatObjectKey,
                    SectionLabelSnapshot = seat.EventSection.DisplayName,
                    CreatedAt = now,
                    CreatedBy = Guid.Empty,
                    UpdatedAt = now,
                    UpdatedBy = Guid.Empty
                };

                await _ticketRepository.InsertAsync(ticket);
                tickets.Add(ticket);
            }

            await _ticketRepository.CommitAsync();
            return tickets;
        }

        private async Task<List<Ticket>> CreateSeasonTicketsAsync(IDictionary<string, decimal> seats, long seasonId, Client client)
        {
            List<Ticket> tickets = new List<Ticket>();

            var seatKeys = seats.Keys.ToList();
            var seatKeysSet = seatKeys.ToHashSet();

            var seasonSeats = await _eventSeatRepository
                .Get()
                .Include(x => x.EventSection)
                    .ThenInclude(x => x.EventSchedule)
                    .ThenInclude(x => x.Event)
                .AsNoTracking()
                .Where(x =>
                    x.EventSection.EventSchedule.Event.SeasonId == seasonId &&
                    seatKeysSet.Contains(x.ExternalSeatObjectKey)
                )
                .ToListAsync();

            var now = DateTimeOffset.UtcNow;

            foreach (var seat in seasonSeats)
            {
                var ticket = new Ticket
                {
                    EventScheduleId = seat.EventSection.EventScheduleId,
                    EventSectionId = seat.EventSectionId,
                    EventSeatId = seat.Id,
                    OriginalClientId = client.Id,
                    CurrentClientId = client.Id,
                    TicketCode = seat.ExternalSeatObjectKey,
                    TicketType = "SeasonPass", // TODO: Define how to manage different ticket types
                    PrivateToken = Guid.NewGuid().ToString("N"), // TODO: Define the logic for the private token
                    PricePaid = seats[seat.ExternalSeatObjectKey],
                    Status = TicketStatus.Issued,
                    SeatLabelSnapshot = seat.ExternalSeatObjectKey,
                    SectionLabelSnapshot = seat.EventSection.DisplayName,
                    CreatedAt = now,
                    CreatedBy = Guid.Empty,
                    UpdatedAt = now,
                    UpdatedBy = Guid.Empty
                };

                await _ticketRepository.InsertAsync(ticket);
                tickets.Add(ticket);
            }

            await _ticketRepository.CommitAsync();
            return tickets;
        }

        private async Task<List<SeasonPass>> CreateSeasonPassesAsync(IDictionary<string, decimal> seats, long seasonId, Client client)
        {
            List<SeasonPass> seasonPasses = new List<SeasonPass>();
            var seatKeys = seats.Keys.ToList();

            var seasonSeats = await _seasonSeatRepository
                .Get()
                .AsNoTracking()
                .Include(ss => ss.SeasonSection)
                    .ThenInclude(s => s.Season)
                .Where(ss =>
                    ss.SeasonSection.SeasonId == seasonId
                    && seatKeys.Contains(ss.ExternalSeatObjectKey)
                )
                .ToListAsync();

            var now = DateTimeOffset.UtcNow;

            foreach (var seat in seasonSeats)
            {
                var seasonPass = new SeasonPass
                {
                    ClientId = client.Id,
                    UserId = null,
                    SeasonId = seasonId,
                    Price = seats[seat.ExternalSeatObjectKey],
                    PurchasedAt = now,
                    SeasonPassType = SeasonPassType.Full,
                    TrackingCode = seat.ExternalSeatObjectKey,
                    PrivateToken = Guid.NewGuid().ToString("N"), // TODO: Define the logic for the private token
                    Status = SeasonPassStatus.Active,
                    CreatedAt = now,
                    CreatedBy = Guid.Empty,
                    UpdatedAt = now,
                    UpdatedBy = Guid.Empty
                };

                await _seasonPassRepository.InsertAsync(seasonPass);
                seasonPasses.Add(seasonPass);
            }

            await _seasonPassRepository.CommitAsync();
            return seasonPasses;
        }

        private async Task CreateSeasonPassEventTicketsAsync(List<SeasonPass> seasonPasses, List<Ticket> tickets)
        {
            var passByCode = seasonPasses.ToDictionary(p => p.TrackingCode);
            var joins = new List<SeasonPassEventTicket>();

            foreach (var ticket in tickets)
            {
                if (!passByCode.TryGetValue(ticket.TicketCode, out var pass))
                    continue;

                var join = new SeasonPassEventTicket
                {
                    SeasonPassId = pass.Id,
                    TicketId = ticket.Id
                };

                await _seasonPassEventTicketRepository.InsertAsync(join);
                joins.Add(join);
            }

            await _seasonPassEventTicketRepository.CommitAsync();

            foreach (var join in joins)
            {
                var ticket = tickets.First(t => t.Id == join.TicketId);
                ticket.SeasonPassEventTicketId = join.Id;
            }
            await _ticketRepository.UpdateRangeAsync(tickets);
        }

        public async Task<SeasonToRenovateDTO> GetOrderToRenovate(long orderId, long clientId)
        {
            var now = DateTimeOffset.UtcNow;
            Order? order = await _orderRepository.GetOrderWithItems(orderId);

            if (order == null)
            {
                throw new Exception("Order not found");
            }

            if (order.ClientId != clientId)
            {
                throw new Exception("This order does not belong to the user");
            }

            var canOrderBeRenew = await CanOrderBeRenewedAsync(order.Reference);

            if (!canOrderBeRenew.CanRenew)
            {
                throw new Exception("The order cannot be renewed");
            }

            OrderItem? item = order.Items.FirstOrDefault();

            if (item == null)
            {
                throw new Exception("No tickets found for this order");
            }

            SeasonPass? seasonPass = await _seasonPassRepository.Get(
                    filter: sp => sp.Id == item.ItemReferenceId,
                    includedProperties: ["Season"]
                )
                .FirstOrDefaultAsync();

            if (seasonPass == null)
            {
                throw new Exception("Season pass not found");
            }

            if (seasonPass.Season.EndDate > now)
            {
                throw new Exception("The season has not ended yet");
            }

            Season? season = await _seasonRepository.Get(
                    filter: season =>
                        season.PreviousSeasonId == seasonPass.SeasonId
                ).FirstOrDefaultAsync();

            if (season == null)
            {
                throw new Exception("Season not found");
            }

            if (season.EndDate < now)
            {
                throw new Exception("The season is no longer available");
            }

            List<Ticket> tickets = await _ticketRepository.Get(
                    filter: ticket =>
                        ticket.OriginalOrderId == order.Id
                ).ToListAsync();

            if (!tickets.Any())
            {
                throw new Exception("No tickets found for the specified order");
            }

            List<SeatDTO>? prevSeatPrices = null;
            var seatLabels = tickets.Select(t => t.SeatLabelSnapshot);
            if (seatLabels != null)
            {
                prevSeatPrices = await _seasonSeatRepository.GetSeasonSeatPricesAsync(season.Id, seatLabels.ToList());
            }

            return new SeasonToRenovateDTO
            {
                SeasonId = season.Id,
                SeasonKey = season.ExternalSeasonKey,
                PreviousSeasonId = seasonPass.SeasonId,
                RelatedOrderId = order.Id,
                PreviousSeats = tickets
                    .Select(t => new
                    {
                        sectionLabel = t.SectionLabelSnapshot,
                        seatLabel = t.SeatLabelSnapshot
                    })
                    .Distinct()
                    .GroupBy(g => g.sectionLabel)
                    .Select(gs => new MyEventSeatDTO
                    {
                        Section = gs.Key,
                        Seats = string.Join(", ", gs.Select(s => s.seatLabel))
                    })
                    .ToList(),
                PreviousSeatPrices = prevSeatPrices
            };
        }

        public async Task<CanRenewOrderResponse> CanOrderBeRenewedAsync(string referenceId)
        {
            Order? order = await _orderRepository.Get()
                                .Include(x => x.Items)
                                .AsNoTracking()
                                .Where(o => o.Reference == referenceId)
                                .SingleOrDefaultAsync();

            if (order == null)
            {
                return new() { OrderId = null, CanRenew = false, NewSeasonId = null, Reference = null };
            }

            CanRenewOrderResponse response = new() { OrderId = order.Id, CanRenew = false, NewSeasonId = null, Reference = order.Reference };

            if (order.OrderType == OrderType.SeasonPass)
            {
                var passIds = order.Items.Select(oi => oi.ItemReferenceId).ToList();

                var passData = await _seasonPassRepository.Get()
                    .Where(sp => passIds.Contains(sp.Id))
                    .Select(sp => new { sp.SeasonId, sp.TrackingCode })
                    .ToListAsync();

                if (!passData.Any())
                {
                    return response;
                }

                long originalSeasonId = passData.First().SeasonId;
                var passTrackingCodes = passData.Select(p => p.TrackingCode).ToList();

                Season? latestSeason = await _seasonService.GetLatestSeasonAsync(originalSeasonId);

                if (latestSeason == null || originalSeasonId == latestSeason.Id)
                {
                    return response;
                }

                response.NewSeasonId = latestSeason.Id;

                var soldCount = await _seasonPassRepository.Get()
                    .Where(sp => sp.SeasonId == latestSeason.Id && passTrackingCodes.Contains(sp.TrackingCode))
                    .CountAsync();

                response.CanRenew = soldCount < passTrackingCodes.Count;

                return response;
            }
            else
            {
                return new() { OrderId = order.Id, CanRenew = false, NewSeasonId = null, Reference = order.Reference };
            }
        }
    }
}
