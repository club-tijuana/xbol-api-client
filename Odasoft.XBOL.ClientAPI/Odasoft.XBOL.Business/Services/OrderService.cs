using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
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
        private readonly SeasonRepository _seasonRepository;
        private readonly SeasonSeatRepository _seasonSeatRepository;
        private readonly EventRepository _eventRepository;
        private readonly UserManager<User> _userManager;

        public OrderService(
            OrderRepository orderRepository,
            ClientRepository clientRepository,
            EventScheduleRepository eventScheduleRepository,
            EventSeatRepository eventSeatRepository,
            TicketRepository ticketRepository,
            SeasonPassRepository seasonPassRepository,
            SeasonRepository seasonRepository,
            SeasonSeatRepository seasonSeatRepository,
            EventRepository eventRepository,
            UserManager<User> userManager
        )
        {
            _orderRepository = orderRepository;
            _clientRepository = clientRepository;
            _eventScheduleRepository = eventScheduleRepository;
            _eventSeatRepository = eventSeatRepository;
            _ticketRepository = ticketRepository;
            _seasonPassRepository = seasonPassRepository;
            _seasonRepository = seasonRepository;
            _seasonSeatRepository = seasonSeatRepository;
            _eventRepository = eventRepository;
            _userManager = userManager;
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
                EventSchedule schedule = await _eventScheduleRepository.Get(x => x.Id == request.ScheduleId).FirstAsync();

                string emailRequest = request.ClientContact.Email.ToUpper();
                var client = await _clientRepository.Get(filter: client => client.Email.ToUpper().Equals(emailRequest)).FirstOrDefaultAsync();

                if (client == null)
                {
                    client = await CreateClientAsync(request.ClientContact);
                }

                request.ClientContact.Id = client.Id;

                List<Ticket> tickets = await CreateTicketsAsync(request.Seats, schedule.EventId, client);

                var newOrder = new Order
                {
                    ClientId = client.Id,
                    UserId = client.UserId,
                    Reference = request.HoldToken,
                    Status = OrderStatus.Paid,
                    SubTotal = (request.PaymentInfoRequest.IsCourtesy ?? false)
                                ? 0
                                : request.Seats.Sum(x => x.Value),
                    TotalFees = 0,
                    TotalTaxes = 0,
                    Total = (request.PaymentInfoRequest.IsCourtesy ?? false)
                                ? 0
                                : request.Seats.Sum(x => x.Value),
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
                        Price = x.PricePaid
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
                Console.WriteLine($"Unable to create event order. Error: {ex.Message}");
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<long> CreateSeasonOrderAsync(SeasonBookingRequest request)
        {
            IDbContextTransaction transaction = await _orderRepository.BeginTransactionAsync();

            try
            {
                //var season = await _seasonPassRepository.Get(x => x.Season.ExternalSeasonKey == request.SeasonKey)
                //                    .Include(x => x.Season)
                //                    .FirstAsync();
                var season = await _seasonRepository.Get(x => x.ExternalSeasonKey == request.SeasonKey)
                    .FirstOrDefaultAsync();

                string emailRequest = request.ClientContact.Email.ToUpper();
                var client = await _clientRepository.Get(filter: client => client.Email.ToUpper().Equals(emailRequest)).FirstOrDefaultAsync();

                if (client == null)
                {
                    client = await CreateClientAsync(request.ClientContact);
                }

                request.ClientContact.Id = client.Id;

                // TODO: Temporarily create tickets for season passes to support ticket sharing.
                // Season passes should eventually handle sharing natively without relying on ticket entities.
                // Remove this workaround once sharing is implemented for season passes.
                List<SeasonPass> seasonPasses = await CreateSeasonPassesAsync(request.Seats, season.Id, client);
                List<Ticket> tickets = await CreateSeasonTicketsAsync(request.Seats, season.Id, client);

                var newOrder = new Order
                {
                    ClientId = client.Id,
                    UserId = client.UserId,
                    Reference = request.HoldToken,
                    Status = OrderStatus.Paid,
                    SubTotal = 0,
                    TotalFees = 0,
                    TotalTaxes = 0,
                    Total = (request.PaymentInfoRequest.IsCourtesy ?? false)
                                ? 0
                                : request.Seats.Sum(x => x.Value),
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
                    })],
                    RelatedOrderId = request.RefereceOrderId
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
                Console.WriteLine($"Unable to create season pass order. Error: {ex.Message}");
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task<Client> CreateClientAsync(ClientInfoRequest clientInfo)
        {
            // TODO: Better use the client service to create the client
            var client = new Client
            {
                Email = clientInfo.Email ?? "",
                PhoneNumber = clientInfo.PhoneNumber ?? "",
                FullName = $"{clientInfo.FirstName} {clientInfo.LastName}",
                BusinessName = clientInfo.FullName,
                ClientType = ClientType.Individual,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = Guid.Empty
            };

            await _clientRepository.InsertAsync(client);
            await _clientRepository.CommitAsync();

            return client;
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
                    UserId = client.UserId,
                    SeasonId = seasonId,
                    BaseSeatId = seat.BaseSeatId,
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

        public async Task<SeasonToRenovateDTO> GetOrderToRenovate(long orderId, long clientId)
        {
            var now = DateTimeOffset.UtcNow;
            Order? order = await _orderRepository.GetOrderWithItems(orderId);

            if (order == null)
            {
                throw new Exception(""); // Throw order not found
            }

            if (order.ClientId != clientId)
            {
                throw new Exception(""); // The order is not of this user
            }

            OrderItem? item = order.Items.FirstOrDefault();

            if (item == null)
            {
                throw new Exception(""); // Throw order item not found
            }

            //SeasonPass? seasonPass = await _seasonPassRepository.GetByIdAsync(item.ItemReferenceId);
            SeasonPass? seasonPass = await _seasonPassRepository.Get(
                    filter: sp => sp.Id == item.ItemReferenceId,
                    includedProperties: ["Season"]
                )
                .FirstOrDefaultAsync();

            if (seasonPass == null)
            {
                throw new Exception(""); // Throw season pass not found
            }

            if (seasonPass.Season.EndDate > now)
            {
                throw new Exception(""); // Throw season not finished yet
            }

            Season? season = await _seasonRepository.Get(
                    filter: season =>
                        season.PreviousSeasonId == seasonPass.SeasonId
                ).FirstOrDefaultAsync();

            if (season == null)
            {
                throw new Exception(""); // Throw season not found
            }

            if (season.EndDate < now)
            {
                throw new Exception(""); // Throw new season already finished
            }

            List<Ticket> tickets = await _ticketRepository.Get(
                    filter: ticket =>
                        ticket.OriginalOrderId == order.Id
                ).ToListAsync();

            if (!tickets.Any())
            {
                throw new Exception(""); // Throw Tickets not found
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
    }
}