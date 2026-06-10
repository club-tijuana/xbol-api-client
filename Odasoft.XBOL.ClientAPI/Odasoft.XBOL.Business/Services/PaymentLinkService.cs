using Microsoft.EntityFrameworkCore;
using Odasoft.XBOL.Business.Exceptions;
using Odasoft.XBOL.Commons.Enums;
using Odasoft.XBOL.Data.Repositories;
using Odasoft.XBOL.DTO;

namespace Odasoft.XBOL.Business.Services
{
    public class PaymentLinkService
    {
        private readonly PaymentLinkRepository _paymentLinkRepository;
        private readonly MediaRepository _mediaRepository;
        private readonly SeasonPassRepository _seasonPassRepository;
        private readonly SeasonRepository _seasonRepository;
        private readonly OrderService _orderService;
        private readonly PaymentRepository _paymentRepository;
        private readonly OrderRepository _orderRepository;
        private readonly TicketRepository _ticketRepository;
        private readonly ITicketingClient _ticketingClient;

        public PaymentLinkService(
            PaymentLinkRepository paymentLinkRepository,
            MediaRepository mediaRepository,
            SeasonPassRepository seasonPassRepository,
            SeasonRepository seasonRepository,
            OrderService orderService,
            PaymentRepository paymentRepository,
            OrderRepository orderRepository,
            TicketRepository ticketRepository,
            ITicketingClient ticketingClient
        )
        {
            _paymentLinkRepository = paymentLinkRepository;
            _mediaRepository = mediaRepository;
            _seasonPassRepository = seasonPassRepository;
            _seasonRepository = seasonRepository;
            _orderService = orderService;
            _paymentRepository = paymentRepository;
            _orderRepository = orderRepository;
            _ticketRepository = ticketRepository;
            _ticketingClient = ticketingClient;
        }

        public async Task<OrderDTO?> GetOrderToPayAsync(string code)
        {
            var paymentLink = await _paymentLinkRepository.Get(
                filter: pl => pl.Code == code,
                includedProperties: [
                        "Order",
                        "Order.Items"
                    ]
            ).FirstOrDefaultAsync();

            if (paymentLink == null)
            {
                return null;
            }

            var order = await _orderRepository.GetByIds(paymentLink.OrderId);
            if (order == null)
            {
                throw new Exception("The order associated with the payment link was not found.");
            }

            if (
                paymentLink.ExpirationDateTime < DateTimeOffset.UtcNow
                || paymentLink.Status == Commons.Enums.PaymentLinkStatus.Expired
            )
            {
                if (paymentLink.Status != Commons.Enums.PaymentLinkStatus.Expired)
                {
                    var itemsIds = paymentLink.Order.Items.Select(oi => oi.ItemReferenceId);
                    var orderType = paymentLink.Order.OrderType;
                    (string? key, List<string>? seatLabels) = await GetOrderSeats(itemsIds, orderType);

                    if (key == null || seatLabels == null)
                    {
                        throw new Exception("Failed to expire the payment link.");
                    }

                    try
                    {
                        await _ticketingClient.ReleaseSeatsActionAsync(new ReleaseSeatsByKeyRequest
                        {
                            EventKey = key,
                            Seats = seatLabels
                        });
                    }
                    catch
                    {
                        throw new Exception("Failed to release the reserved seats.");
                    }

                    paymentLink.Status = PaymentLinkStatus.Expired;
                    await _paymentLinkRepository.UpdateAsync(paymentLink);
                    await _paymentLinkRepository.CommitAsync();

                    order.Status = OrderStatus.Cancelled;
                    order.UpdatedAt = DateTimeOffset.UtcNow;

                    await _orderRepository.UpdateAsync(order);
                    await _orderRepository.CommitAsync();
                }

                throw new PaymentLinkExpiredException();
            }

            if (
                paymentLink.CancelledAt.HasValue
                || paymentLink.Status == PaymentLinkStatus.Cancelled
            )
            {
                throw new PaymentLinkCanceledException();
            }

            if (paymentLink.PaidAt.HasValue)
            {
                throw new PaymentLinkAlreadyUsedException();
            }

            var orderResponse = await _orderService.GetOrderAsync(null, paymentLink.OrderId, true);
            return orderResponse;
        }

        public async Task<long> PayOrderAsync(string code, PaymentInfoRequest paymentInfoRequest)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            if (paymentInfoRequest.CardAmount == null)
            {
                throw new Exception("Payment amount is missing.");
            }

            var paymentLink = await _paymentLinkRepository.Get(
                filter: pl => pl.Code == code
            ).FirstOrDefaultAsync();

            if (paymentLink == null)
            {
                throw new Exception("Payment link not found.");
            }

            await _paymentRepository.InsertAsync(new Models.Payment
            {
                OrderId = paymentLink.OrderId,
                Currency = Commons.Enums.CurrencyType.MXN,
                Amount = paymentInfoRequest.CardAmount.Value,
                AmountMXN = paymentInfoRequest.CardAmount.Value,
                ExchangeRateId = 0,
                PaymentType = Commons.Enums.PaymentType.Card,
                Provider = "",
                ProviderReference = "",
                TransactionReference = Guid.Empty,
                AppliedAt = now,
                CreatedAt = now,
                CreatedBy = Guid.Empty,
                UpdatedBy = Guid.Empty
            });
            await _paymentRepository.CommitAsync();

            paymentLink.PaidAt = now;
            paymentLink.Status = Commons.Enums.PaymentLinkStatus.Paid;
            await _paymentLinkRepository.UpdateAsync(paymentLink);
            await _paymentLinkRepository.CommitAsync();

            await _orderService.PayOrderAsync(paymentLink.OrderId);

            return paymentLink.OrderId;
        }

        public async Task<SeoMetadataDTO> GetEventMetadataByPaymentCodeAsync(string code)
        {
            var response = new SeoMetadataDTO();

            var paymentLink = await _paymentLinkRepository.Get(
                filter: pl => pl.Code == code,
                includedProperties: ["Order", "Order.Items"]
            ).FirstOrDefaultAsync();

            if (paymentLink == null)
            {
                return response;
            }

            var orderType = paymentLink.Order.OrderType;
            var orderItem = paymentLink.Order.Items.First();
            response = new SeoMetadataDTO();

            if (orderType == Commons.Enums.OrderType.Ticket)
            {
                var ticket = await _ticketRepository.Get(
                        filter: t => t.Id == orderItem.ItemReferenceId,
                        includedProperties: ["EventSchedule", "EventSchedule.Event"]
                    ).FirstOrDefaultAsync();

                if (ticket != null)
                {
                    var banner = await _mediaRepository.Get(
                            filter: m => m.ReferenceId == ticket.EventSchedule.EventId &&
                                m.ReferenceType == Commons.Enums.ClientSaleType.Event &&
                                m.MediaType == Commons.Enums.ClientMediaType.Banner &&
                                m.DeletedAt == null
                        )
                        .OrderBy(m => m.Order)
                        .FirstOrDefaultAsync();

                    var imageUrl = banner != null && banner.Url != null ? banner.Url : ticket.EventSchedule.Event.PosterImageUrl;

                    response.Title = ticket.EventSchedule.Event.Name;
                    response.Description = ticket.EventSchedule.Event.ShortDescription;
                    response.ImageUrl = imageUrl ?? "";
                }
            }
            else if (orderType == Commons.Enums.OrderType.SeasonPass)
            {
                var seasonPass = await _seasonPassRepository.Get(
                        filter: sp => sp.Id == orderItem.ItemReferenceId,
                        includedProperties: ["Season"]
                    ).FirstOrDefaultAsync();

                if (seasonPass != null)
                {
                    var banner = await _mediaRepository.Get(
                            filter: m => m.ReferenceId == seasonPass.SeasonId &&
                                m.ReferenceType == Commons.Enums.ClientSaleType.SeasonPass &&
                                m.MediaType == Commons.Enums.ClientMediaType.Banner &&
                                m.DeletedAt == null
                        )
                        .OrderBy(m => m.Order)
                        .FirstOrDefaultAsync();

                    var imageUrl = banner != null && banner.Url != null ? banner.Url : seasonPass.Season.PosterImageUrl;

                    response.Title = seasonPass.Season.Name;
                    response.Description = seasonPass.Season.Description;
                    response.ImageUrl = imageUrl;
                }
            }

            return response;
        }

        public async Task<(string?, List<string>?)> GetOrderSeats(IEnumerable<long> itemIds, OrderType orderType)
        {
            if (orderType == OrderType.Ticket)
            {
                var tickets = await _ticketRepository.Get(
                        t => itemIds.Contains(t.Id),
                        includedProperties: [
                            "EventSchedule"
                        ]
                    ).ToListAsync();

                if (tickets == null)
                {
                    return (null, null);
                }

                var key = tickets.Select(t => t.EventSchedule.ExternalEventKey)?.First();
                var seatLabels = tickets.Select(t => t.SeatLabelSnapshot).ToList();

                return (key, seatLabels);
            }
            else if (orderType == OrderType.SeasonPass)
            {
                var seasonPass = await _seasonPassRepository.Get(
                        sp => itemIds.Contains(sp.Id),
                        includedProperties: [
                            "Season",
                            "SeasonPassEventTickets",
                            "SeasonPassEventTickets.Ticket"
                        ]
                    )
                    .ToListAsync();

                if (seasonPass == null)
                {
                    return (null, null);
                }

                var key = seasonPass.Select(sp => sp.Season.ExternalSeasonKey)?.First();
                var seasonPassTickets = seasonPass.SelectMany(sp => sp.SeasonPassEventTickets).ToList();

                if (seasonPassTickets == null)
                {
                    return (null, null);
                }

                var seatLabels = seasonPassTickets.Select(spt => spt.Ticket.SeatLabelSnapshot).Distinct().ToList();

                return (key, seatLabels);
            }

            return (null, null);
        }
    }
}
