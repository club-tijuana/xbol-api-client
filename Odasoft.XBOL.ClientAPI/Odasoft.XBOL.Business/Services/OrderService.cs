using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Odasoft.XBOL.Commons.Enums;
using Odasoft.XBOL.Commons.Responses;
using Odasoft.XBOL.Data.Repositories;
using Odasoft.XBOL.DTO;
using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Business.Services
{
    public class OrderService
    {
        private const int MIN_PAGE = 1;
        private const int MAX_PAGE = 50;
        private const string SEASONPASS = "SEASONPASS";
        private const string BUNDLEPASS = "BUNDLEPASS";

        private readonly OrderRepository _orderRepository;
        private readonly ClientRepository _clientRepository;
        private readonly ClientService _clientService;
        private readonly EventScheduleRepository _eventScheduleRepository;
        private readonly EventSeatRepository _eventSeatRepository;
        private readonly TicketRepository _ticketRepository;
        private readonly SeasonPassRepository _seasonPassRepository;
        private readonly SeasonPassEventTicketRepository _seasonPassEventTicketRepository;
        private readonly SeasonRepository _seasonRepository;
        private readonly SeasonSeatRepository _seasonSeatRepository;
        private readonly BundlePassRepository _bundlePassRepository;
        private readonly EventRepository _eventRepository;
        private readonly SeasonService _seasonService;
        private readonly EventScheduleService _eventScheduleService;
        private readonly ITicketingClient _ticketingClient;
        private readonly ILogger<OrderService> _logger;
        private readonly ClientCreditTransactionService _clientCreditTransactionService;
        private readonly BundleService _bundleService;
        private readonly BundleRepository _bundleRepository;
        private readonly BundlePassEventTicketRepository _bundlePassEventTicketRepository;

        public OrderService(
            OrderRepository orderRepository,
            ClientRepository clientRepository,
            ClientService clientService,
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
            ITicketingClient ticketingClient,
            ILogger<OrderService> logger,
            ClientCreditTransactionService clientCreditTransactionService,
            BundlePassRepository bundlePassRepository,
            BundleService bundleService,
            BundleRepository bundleRepository,
            BundlePassEventTicketRepository bundlePassEventTicketRepository
        )
        {
            _orderRepository = orderRepository;
            _clientRepository = clientRepository;
            _clientService = clientService;
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
            _ticketingClient = ticketingClient;
            _logger = logger;
            _clientCreditTransactionService = clientCreditTransactionService;
            _bundlePassRepository = bundlePassRepository;
            _bundleService = bundleService;
            _bundleRepository = bundleRepository;
            _bundlePassEventTicketRepository = bundlePassEventTicketRepository;
        }

        public async Task<OrderDTO?> GetOrderAsync(long? clientId, long orderId, bool? isPaymentLink = false)
        {
            return await _orderRepository.GetOrderAsync(clientId, orderId, isPaymentLink);
        }

        public async Task<long> CreateEventOrderAsync(EventBookingRequest request)
        {
            IDbContextTransaction transaction = await _orderRepository.BeginTransactionAsync();
            OrderStatus orderStatus = OrderStatus.Pending;

            try
            {
                DateTimeOffset now = DateTimeOffset.UtcNow;
                EventSchedule schedule = await _eventScheduleRepository.Get(x =>
                    x.ExternalEventKey == request.EventKey,
                    includedProperties: ["Event"]
                ).FirstAsync();

                var client = await UpsertClientFromOrderContactAsync(request.ClientContact);
                request.ClientContact.Id = client.Id;

                List<Ticket> tickets = await CreateTicketsAsync(request.Seats, schedule.EventId, client);

                // TODO: Retrieve Fee and Tax once dynamic pricing is implemented
                // TODO: Retrieve prices from DB, do not trust in front
                decimal Subtotal = request.Seats.Sum(x => x.SeatPrice);
                decimal Fee = 0;
                decimal Tax = 0;
                decimal Discount = 0;
                decimal Total = (Subtotal + Fee + Tax) - Discount;

                PaymentInfoRequest? paymentInfo = request.PaymentInfoRequest;
                List<Payment> payments = [];
                bool hasPayments = paymentInfo == null ? false :
                    (
                        paymentInfo.CardAmount > 0
                    );

                if (!hasPayments)
                {
                    throw new Exception("No payments have been specified.");
                }

                payments = await PaymentInfoToPayments(
                        request.PaymentInfoRequest,
                        Total,
                        Guid.Empty // TODO: Temporary implementation. Review the new user relationship structure.
                    );

                var newOrder = new Order
                {
                    ClientId = request.ClientContact.Id.Value,
                    UserId = null,
                    Reference = request.Localizer,
                    Status = orderStatus,
                    PaidAt = now,
                    SubTotal = Subtotal,
                    TotalFees = Fee,
                    TotalTaxes = Tax,
                    Discount = Discount,
                    Total = Total,
                    OrderType = OrderType.Ticket,
                    SaleChannel = SaleChannel.Online,
                    CreatedAt = now,
                    CreatedBy = Guid.Empty,
                    UpdatedAt = now,
                    UpdatedBy = Guid.Empty,
                    Items = [.. tickets.Select(x => new OrderItem
                    {
                        ItemType = Commons.Enums.ItemType.Ticket,
                        ItemReferenceId = x.Id,
                        Price = x.PricePaid,
                        IsCourtesy = false,
                        PriceListItemId = request.Seats.FirstOrDefault(s => s.SeatKey == x.SeatLabelSnapshot)?.PriceListItemId
                    })],
                    Fees = [
                        new OrderFee {
                            FeeType = "test_type",
                            Amount = Fee
                        }
                    ],
                    Taxes = [
                        new OrderTax {
                            TaxType = "test_type",
                            Amount = Tax
                        }
                    ],
                    Payments = payments
                };

                await _orderRepository.InsertAsync(newOrder);
                await _orderRepository.CommitAsync();

                foreach (var ticket in tickets)
                {
                    ticket.OriginalOrderId = newOrder.Id;
                }

                await _ticketRepository.UpdateRangeAsync(tickets);
                await _ticketRepository.CommitAsync();

                //// We should validate in the request that the client has credit before even processing the payment
                //if (request.PaymentInfoRequest.CreditAmount > 0
                //    && client.ClientCreditAccount != null
                //    && client.IsActive)
                //{
                //    await CreateClientCreditTransactionAsync(client, request.PaymentInfoRequest.CreditAmount.Value, newOrder.Reference);
                //}

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
                DateTimeOffset now = DateTimeOffset.UtcNow;
                Season? season = await _seasonRepository
                    .Get(x => x.ExternalSeasonKey == request.SeasonKey)
                    .SingleOrDefaultAsync();

                var seatRequestLookup = request.Seats.ToDictionary(
                    x => x.SeatKey,
                    x => x
                );

                var client = await UpsertClientFromOrderContactAsync(request.ClientContact);

                request.ClientContact.Id = client.Id;

                if (request.ReferenceOrderId.HasValue)
                {
                    var ownsReferenceOrder = await _orderRepository
                        .Get(order => order.Id == request.ReferenceOrderId.Value
                            && order.ClientId == client.Id)
                        .AnyAsync();

                    if (!ownsReferenceOrder)
                    {
                        throw new InvalidOperationException("Reference order does not belong to the verified client.");
                    }
                }

                // TODO: Temporarily create tickets for season passes to support ticket sharing.
                // Season passes should eventually handle sharing natively without relying on ticket entities.
                // Remove this workaround once sharing is implemented for season passes.
                List<SeasonPass> seasonPasses = await CreateSeasonPassesAsync(request.Seats.ToDictionary(s => s.SeatKey, s => s.SeatPrice), season.Id, client);
                List<Ticket> tickets = await CreateSeasonTicketsAsync(request.Seats, season.Id, client);
                await CreateSeasonPassEventTicketsAsync(seasonPasses, tickets);

                // TODO: Retrieve Fee and Tax once dynamic pricing is implemented
                decimal Subtotal = request.Seats.Sum(x => x.SeatPrice);
                decimal Fee = 0;
                decimal Tax = 0;
                decimal Discount = 0;
                decimal Total = (Subtotal + Fee + Tax) - Discount;

                PaymentInfoRequest? paymentInfo = request.PaymentInfoRequest;
                List<Payment> payments = [];
                bool hasPayments = paymentInfo == null ? false :
                    (
                        paymentInfo.CardAmount > 0
                    );

                if (!hasPayments)
                {
                    throw new Exception("No payments have been specified.");
                }

                payments = await PaymentInfoToPayments(
                        request.PaymentInfoRequest,
                        Total,
                        Guid.Empty // TODO: Temporary implementation. Review the new user relationship structure.
                    );

                var newOrder = new Order
                {
                    ClientId = request.ClientContact.Id.Value,
                    UserId = null,
                    Reference = request.Localizer,
                    Status = OrderStatus.Paid,
                    PaidAt = now,
                    SubTotal = Subtotal,
                    TotalFees = Fee,
                    TotalTaxes = Tax,
                    Discount = Discount,
                    Total = Total,
                    OrderType = OrderType.SeasonPass,
                    SaleChannel = SaleChannel.Online,
                    CreatedAt = now,
                    CreatedBy = Guid.Empty,
                    UpdatedAt = now,
                    UpdatedBy = Guid.Empty,
                    Items = [.. seasonPasses.Select(x => new OrderItem
                    {
                        ItemType = Commons.Enums.ItemType.SeasonPass,
                        ItemReferenceId = x.Id,
                        Price = x.Price,
                        IsCourtesy = false,
                        PriceListItemId = seatRequestLookup[x.TrackingCode].PriceListItemId
                    })],
                    Fees = [
                        new OrderFee {
                            FeeType = "test_type",
                            Amount = Fee
                        }
                    ],
                    Taxes = [
                        new OrderTax {
                            TaxType = "test_type",
                            Amount = Tax
                        }
                    ],
                    Payments = payments,
                    RelatedOrderId = request.ReferenceOrderId
                };

                await _orderRepository.InsertAsync(newOrder);
                await _orderRepository.CommitAsync();

                foreach (var ticket in tickets)
                {
                    ticket.OriginalOrderId = newOrder.Id;
                }

                await _ticketRepository.UpdateRangeAsync(tickets);
                await _ticketRepository.CommitAsync();

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
            var updatClient = !string.IsNullOrEmpty(clientInfo.Email);
            string? effectiveFullName;

            if (clientInfo.Id.HasValue && updatClient)
            {
                EnsureUsableContact(clientInfo);

                effectiveFullName = ResolveFullName(clientInfo);

                var existingClient = await _clientRepository.GetByIdAsync(clientInfo.Id.Value);
                if (existingClient is null)
                {
                    throw new InvalidOperationException("Client not found.");
                }

                ApplyOrderContact(existingClient, clientInfo, effectiveFullName);
                await _clientRepository.UpdateAsync(existingClient);
                return existingClient;
            }
            else if (clientInfo.Id.HasValue && !updatClient)
            {
                var existingClient = await _clientRepository.GetByIdAsync(clientInfo.Id.Value);
                if (existingClient is null)
                {
                    throw new InvalidOperationException("Client not found.");
                }
                return existingClient;
            }

            effectiveFullName = ResolveFullName(clientInfo);
            return await CreateClientAsync(clientInfo, effectiveFullName);
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

        private static void EnsureUsableContact(ClientInfoRequest clientInfo)
        {
            var hasEmail = !string.IsNullOrWhiteSpace(clientInfo.Email);
            var hasPhone = !string.IsNullOrWhiteSpace(clientInfo.PhoneNumber)
                && NormalizePhoneNumber(clientInfo.PhoneNumber).Length > 0;

            if (!hasEmail && !hasPhone)
            {
                throw new InvalidOperationException("Client email or phone number must be provided.");
            }
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

        private async Task<List<Ticket>> CreateTicketsAsync(ICollection<BookingSeatRequest> seats, long eventId, Client client)
        {
            List<Ticket> tickets = new List<Ticket>();

            var seatLookup = seats.ToDictionary(
                x => x.SeatKey,
                x => x
            );
            var seatKeysSet = seatLookup.Keys.ToHashSet();

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
                var bookingSeat = seatLookup[seat.ExternalSeatObjectKey];

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
                    PricePaid = bookingSeat.SeatPrice,
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

        private async Task<List<Ticket>> CreateSeasonTicketsAsync(ICollection<BookingSeatRequest> seats, long seasonId, Client client)
        {
            List<Ticket> tickets = new List<Ticket>();

            var seatLookup = seats.ToDictionary(
                x => x.SeatKey,
                x => x
            );
            var seatKeysSet = seatLookup.Keys.ToHashSet();

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
                var bookingSeat = seatLookup[seat.ExternalSeatObjectKey];

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
                    PricePaid = bookingSeat.SeatPrice,
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
                    UpdatedBy = Guid.Empty,
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
                {
                    continue;
                }

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

        public async Task<BundleToRenovateDTO> GetOrderToRenovate(long orderId, long clientId)
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

            BundlePass? bundlePass = await _bundlePassRepository.Get(
                    filter: bp => bp.Id == item.ItemReferenceId,
                    includedProperties: ["Bundle"]
                )
                .FirstOrDefaultAsync();

            if (bundlePass == null)
            {
                throw new Exception("Bundle pass not found");
            }

            if (bundlePass.Bundle.EndDate > now)
            {
                throw new Exception("The bundle has not ended yet");
            }

            Bundle? bundle = await _bundleRepository.Get(
                    filter: bundle =>
                        bundle.PreviousBundleId == bundlePass.BundleId
                ).FirstOrDefaultAsync();

            if (bundle == null)
            {
                throw new Exception("Bundle not found");
            }

            if (bundle.ExternalKey == null)
            {
                throw new Exception("Bundle has no key");
            }

            if (bundle.EndDate < now)
            {
                throw new Exception("The bundle is no longer available");
            }

            List<Ticket> tickets = await _ticketRepository.Get(
                    filter: ticket => ticket.OriginalOrderId == order.Id,
                    includedProperties: [
                        "EventSeat.BaseSeat.BaseRow.BaseSection.BaseZone"
                    ]
                ).ToListAsync();

            if (!tickets.Any())
            {
                throw new Exception("No tickets found for the specified order");
            }

            if (bundle.RenewalEndDate < now)
            {
                var availableSeats = await CheckSeatStatus(bundle.ExternalKey, tickets.Select(t => t.SeatLabelSnapshot).ToList());

                tickets = tickets
                    .Where(t => availableSeats.Contains(t.SeatLabelSnapshot))
                    .ToList();

                if (!tickets.Any())
                {
                    throw new Exception("The seats are no longer available for renewal");
                }
            }

            List<SeatDTO>? prevSeatPrices = null;
            var seatLabels = tickets.Select(t => t.SeatLabelSnapshot);

            var renewedObjects = await _orderRepository.Get(
                    filter: o => o.RelatedOrderId == orderId,
                    includedProperties: [
                        "Items"
                    ]
                )
                .SelectMany(o => o.Items.Select(oi => oi.ItemReferenceId))
                .ToListAsync();

            if (renewedObjects != null)
            {
                var renewedTickets = await _bundlePassEventTicketRepository.Get(
                        filter: bp => renewedObjects.Contains(bp.BundlePassId),
                        includedProperties: ["Ticket"]
                    )
                    .Select(bpet => bpet.Ticket.SeatLabelSnapshot)
                    .ToListAsync();

                seatLabels = seatLabels.Except(renewedTickets);

                tickets = tickets
                    .Where(t => !renewedTickets.Contains(t.SeatLabelSnapshot))
                    .ToList();
            }

            if (seatLabels != null)
            {
                var bundlePrices = await _ticketingClient.GetSeatsIoPricesAsync(SaleType.Bundle, bundle.Id);

                var seatOverrides = bundlePrices
                    .Where(x => x.Objects != null)
                    .SelectMany(x => x.Objects!.Select(seat => new
                    {
                        Seat = seat,
                        Price = x.Price,
                        PriceListItemId = x.PriceListItemId
                    }))
                    .ToDictionary(x => x.Seat);

                var zonePrices = bundlePrices
                    .Where(x => x.Category.HasValue)
                    .ToDictionary(
                        x => x.Category!.Value,
                        x => new
                        {
                            x.Price,
                            x.PriceListItemId
                        });

                prevSeatPrices = tickets
                    .Select(ticket =>
                    {
                        if (seatOverrides.TryGetValue(
                                ticket.SeatLabelSnapshot,
                                out var overridePrice))
                        {
                            return new SeatDTO
                            {
                                ExternalSeatObjectKey = ticket.SeatLabelSnapshot,
                                PriceOverride = overridePrice.Price,
                                PriceListItemId = overridePrice.PriceListItemId
                            };
                        }

                        var zoneKey = ticket.EventSeat
                            .BaseSeat
                            .BaseRow
                            .BaseSection
                            .BaseZone
                            .ExternalZoneKey;

                        if (zoneKey.HasValue &&
                            zonePrices.TryGetValue(zoneKey.Value, out var zonePrice))
                        {
                            return new SeatDTO
                            {
                                ExternalSeatObjectKey = ticket.SeatLabelSnapshot,
                                PriceOverride = zonePrice.Price,
                                PriceListItemId = zonePrice.PriceListItemId
                            };
                        }

                        return new SeatDTO
                        {
                            ExternalSeatObjectKey = ticket.SeatLabelSnapshot
                        };
                    })
                    .ToList();
            }

            return new BundleToRenovateDTO
            {
                BundleId = bundle.Id,
                BundleKey = bundle.ExternalKey,
                PreviousBundleId = bundlePass.BundleId,
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
                    .GroupBy(x => x.ExternalSeatObjectKey)
                    .Select(g => g.First())
                    .ToList()
            };
        }

        public async Task<CanRenewOrderResponse> CanOrderBeRenewedAsync(string referenceId)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            Order? order = await _orderRepository.Get()
                                .Include(x => x.Items)
                                .AsNoTracking()
                                .Where(o => o.Reference == referenceId && o.Status == OrderStatus.Paid)
                                .SingleOrDefaultAsync();

            if (order == null)
            {
                return new CanRenewOrderResponse
                {
                    OrderId = null,
                    CanRenew = false,
                    NewSeasonId = null,
                    Reference = null,
                    RenewableSeats = 0,
                    TotalSeats = 0
                };
            }

            CanRenewOrderResponse response = new()
            {
                OrderId = order.Id,
                CanRenew = false,
                NewSeasonId = null,
                Reference = order.Reference,
                RenewableSeats = 0,
                TotalSeats = 0
            };

            if (order.OrderType != OrderType.Bundle)
            {
                return response;
            }

            var passIds = order.Items.Select(oi => oi.ItemReferenceId).ToList();

            //var passData = await _seasonPassRepository.Get()
            //    .Where(sp => passIds.Contains(sp.Id))
            //    .Select(sp => new { sp.Id, sp.SeasonId, sp.TrackingCode })
            //    .ToListAsync();
            var passData = await _bundlePassRepository.Get()
                .Where(bp => passIds.Contains(bp.Id))
                .Select(bp => new { bp.Id, bp.BundleId, bp.TrackingCode })
                .ToListAsync();

            if (!passData.Any())
            {
                return response;
            }

            long originalSeasonId = passData.First().BundleId;
            var passTrackingCodes = passData.Select(p => p.TrackingCode).Distinct().ToList();

            response.TotalSeats = passTrackingCodes.Count;

            Bundle? latestBundle = await _bundleService.GetLatestBundleAsync(originalSeasonId);

            if (latestBundle == null || originalSeasonId == latestBundle.Id)
            {
                return response;
            }

            response.NewSeasonId = latestBundle.Id;

            bool isRenewalWindow =
                latestBundle.RenewalStartDate <= now &&
                latestBundle.RenewalEndDate >= now;

            bool isPreSaleWindow =
                latestBundle.PreSaleDate <= now &&
                latestBundle.OnSaleDate > now;

            bool isOnSaleWindow =
                latestBundle.OnSaleDate <= now;

            bool isWithinRenewableWindow =
                isRenewalWindow ||
                isPreSaleWindow ||
                isOnSaleWindow;

            if (!isWithinRenewableWindow)
            {
                return response;
            }

            List<string>? renewedTrackingCodes;
            List<string>? remainingTrackingCodes;
            if (isRenewalWindow)
            {
                //renewedTrackingCodes = await _seasonPassRepository.Get()
                //.Where(sp =>
                //    sp.SeasonId == latestBundle.Id &&
                //    passTrackingCodes.Contains(sp.TrackingCode)
                //)
                //.Select(sp => sp.TrackingCode)
                //.Distinct()
                //.ToListAsync();
                renewedTrackingCodes = await _bundlePassRepository.Get()
                .Where(bp =>
                    bp.BundleId == latestBundle.Id &&
                    passTrackingCodes.Contains(bp.TrackingCode)
                )
                .Select(sp => sp.TrackingCode)
                .Distinct()
                .ToListAsync();

                remainingTrackingCodes = passTrackingCodes
                    .Except(renewedTrackingCodes)
                    .ToList();

                response.RenewableSeats = remainingTrackingCodes.Count;
                response.CanRenew = remainingTrackingCodes.Any();

                return response;
            }

            renewedTrackingCodes = await _seasonPassRepository.Get()
                .Where(sp =>
                    sp.SeasonId == latestBundle.Id &&
                    passTrackingCodes.Contains(sp.TrackingCode)
                )
                .Select(sp => sp.TrackingCode)
                .Distinct()
                .ToListAsync();

            remainingTrackingCodes = passTrackingCodes
                .Except(renewedTrackingCodes)
                .ToList();

            if (!remainingTrackingCodes.Any())
            {
                response.RenewableSeats = 0;
                response.CanRenew = false;

                return response;
            }

            var availableTickets = await CheckSeatStatus(latestBundle.ExternalKey, remainingTrackingCodes);

            response.RenewableSeats = availableTickets.Count;
            response.CanRenew = availableTickets.Any();

            return response;

            //long renewedSeatCount = await _orderRepository.Get()
            //    .Where(o => o.RelatedOrderId == order.Id)
            //    .SelectMany(o => o.Items)
            //    .LongCountAsync();

            //response.RenewableSeats =
            //    Math.Max(0L, (response.TotalSeats ?? 0L) - renewedSeatCount);

            //response.CanRenew =
            //    response.RenewableSeats > 0;

            //return response;
        }

        public async Task<SeoMetadataDTO> GetOrderMetadataAsync(long orderId)
        {
            Order? order = await _orderRepository.Get(
                    filter: order => order.Id == orderId,
                    includedProperties: [
                        "Items"
                    ]
                )
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return new SeoMetadataDTO();
            }

            var referenceIds = order.Items.Select(oi => oi.ItemReferenceId);
            if (order.OrderType == OrderType.Ticket)
            {
                var ticket = await _ticketRepository.Get(
                        filter: ticket => referenceIds.Contains(ticket.Id)
                    )
                    .FirstOrDefaultAsync();

                if (ticket == null)
                {
                    return new SeoMetadataDTO();
                }

                var schedule = await _eventScheduleRepository.Get(
                        filter: schedule => schedule.Id == ticket.EventScheduleId,
                        includedProperties: [
                            "Event"
                        ]
                    ).FirstOrDefaultAsync();

                if (schedule == null)
                {
                    return new SeoMetadataDTO();
                }

                var evnt = await _eventRepository.GetByIdAsync(schedule.EventId);

                if (evnt == null)
                {
                    return new SeoMetadataDTO();
                }

                return new SeoMetadataDTO
                {
                    Title = evnt.Name,
                    Description = evnt.ShortDescription,
                    ImageUrl = evnt.PosterImageUrl ?? ""
                };
            }
            else if (order.OrderType == OrderType.SeasonPass)
            {
                var seasonPass = await _seasonPassRepository.Get(
                        filter: sp => referenceIds.Contains(sp.Id)
                    )
                    .FirstOrDefaultAsync();

                if (seasonPass == null)
                {
                    return new SeoMetadataDTO();
                }

                var season = await _seasonRepository.GetByIdAsync(seasonPass.SeasonId);

                if (season == null)
                {
                    return new SeoMetadataDTO();
                }

                return new SeoMetadataDTO
                {
                    Title = season.Name,
                    Description = season.Description,
                    ImageUrl = season.PosterImageUrl
                };
            }
            else
            {
                return new SeoMetadataDTO();
            }
        }

        public async Task<SeoMetadataDTO> GetRenovationMetadataAsync(long orderId)
        {
            Order? order = await _orderRepository.Get(
                    filter: order => order.Id == orderId,
                    includedProperties: [
                        "Items"
                    ]
                )
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return new SeoMetadataDTO();
            }

            if (order.OrderType == OrderType.Ticket)
            {
                return new SeoMetadataDTO();
            }

            var referenceIds = order.Items.Select(oi => oi.ItemReferenceId);

            var seasonPass = await _seasonPassRepository.Get(
                        filter: sp => referenceIds.Contains(sp.Id)
                    )
                    .FirstOrDefaultAsync();

            if (seasonPass == null)
            {
                return new SeoMetadataDTO();
            }

            var season = await _seasonRepository.GetByIdAsync(seasonPass.SeasonId);

            if (season == null)
            {
                return new SeoMetadataDTO();
            }

            var nextSeason = await _seasonRepository.Get(
                    filter: s => s.PreviousSeasonId == season.Id
                ).FirstOrDefaultAsync();

            if (nextSeason == null)
            {
                return new SeoMetadataDTO();
            }

            return new SeoMetadataDTO
            {
                Title = nextSeason.Name,
                Description = nextSeason.Description,
                ImageUrl = nextSeason.PosterImageUrl
            };
        }

        public async Task<PagedResponse<MyEventDTO>> GetMyEventsAsync(
            int? page,
            int? pageSize,
            OrderType orderType,
            long idClient)
        {
            int currentPage = page ?? MIN_PAGE;
            int currentPageSize = pageSize ?? MAX_PAGE;

            DateTimeOffset now = DateTimeOffset.UtcNow;

            var result = await _orderRepository.GetMyEventsAsync(
                currentPage,
                currentPageSize,
                orderType,
                idClient);

            List<MyEventDTO> events = [];

            foreach (var order in result.Orders)
            {
                bool isBundle = order.Tickets.Any(t =>
                    t.TicketType.ToUpper().Trim() == BUNDLEPASS
                );

                var currentSchedule = order.Tickets
                    .GroupBy(t => t.EventScheduleId)
                    .Select(g => g.First())
                    .OrderBy(t => t.StartDateTime < now)
                    .ThenByDescending(t => t.StartDateTime)
                    .First();

                bool isPastEvent;

                if (isBundle)
                {
                    isPastEvent = order.Tickets.All(t => t.EndDateTime < now);
                }
                else
                {
                    isPastEvent = currentSchedule.EndDateTime < now;
                }

                bool canRenovateSeasonPass = false;

                if (isBundle)
                {
                    var renewalResult = await CanOrderBeRenewedAsync(order.Reference);
                    canRenovateSeasonPass = renewalResult.CanRenew;
                }

                events.Add(new MyEventDTO
                {
                    OrderId = order.Id,
                    EventId = currentSchedule.EventId,
                    EventImage = !string.IsNullOrWhiteSpace(currentSchedule.BannerUrl)
                        ? currentSchedule.BannerUrl
                        : currentSchedule.LegacyPosterUrl ?? string.Empty,
                    Name = isBundle
                        ? currentSchedule.SeasonName
                        : currentSchedule.EventName,
                    StartDate = currentSchedule.StartDateTime,
                    Location = currentSchedule.Location,
                    IsSeasonPass = isBundle, // TODO: Update name
                    IsPastEvent = isPastEvent,
                    CanRenovateSeasonPass = canRenovateSeasonPass
                });
            }

            return new PagedResponse<MyEventDTO>
            {
                Items = events,
                TotalCount = result.TotalCount,
                Page = currentPage,
                PageSize = currentPageSize,
                TotalPages = (int)Math.Ceiling(
                    result.TotalCount / (double)currentPageSize)
            };
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

        public async Task PayOrderAsync(long orderId)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);

            if (order == null)
            {
                throw new Exception("Order not found.");
            }

            order.Status = OrderStatus.Paid;
            order.PaidAt = DateTimeOffset.UtcNow;

            await _orderRepository.UpdateAsync(order);
            await _orderRepository.CommitAsync();
        }

        private async Task<HashSet<string?>> CheckSeatStatus(string bundleKey, List<string> seatLabels)
        {
            if (!seatLabels.Any())
            {
                return [];
            }

            var response = await _ticketingClient.GetSeatsInfoAsync(bundleKey, seatLabels);

            if (response == null)
            {
                return [];
            }

            return response
                .Where(r => r.Value.IsAvailable == true)
                .Select(r => r.Value.Label)
                .ToHashSet();
        }

        private async Task<List<Payment>> PaymentInfoToPayments(
            PaymentInfoRequest paymentInfo,
            decimal Total,
            Guid userId
        )
        {
            List<Payment> payments = new List<Payment>();
            DateTimeOffset now = DateTimeOffset.UtcNow;

            decimal cardAmount = paymentInfo.CardAmount ?? 0;
            decimal _total = Total;

            decimal totalPaid = cardAmount;

            if (totalPaid < Total)
            {
                throw new Exception("The payment amount is insufficient to pay for the order.");
            }

            if (cardAmount > 0)
            {
                payments.Add(new Payment
                {
                    Currency = CurrencyType.MXN,
                    Amount = Total,
                    AmountMXN = Total,
                    ReceivedAmount = cardAmount,
                    ReceivedAmountMXN = cardAmount,
                    ExchangeRateId = 0,
                    ExchangeRate = 0,
                    PaymentType = PaymentType.Card,
                    Provider = "",
                    ProviderReference = "",
                    TransactionReference = Guid.NewGuid(),
                    AppliedAt = now,
                    CreatedAt = now,
                    CreatedBy = userId,
                    UpdatedBy = userId,
                });

                _total = Total - cardAmount;
            }

            return payments;
        }
    }
}