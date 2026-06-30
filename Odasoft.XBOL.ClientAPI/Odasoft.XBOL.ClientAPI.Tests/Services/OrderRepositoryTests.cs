using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Odasoft.XBOL.Business.Services;
using Odasoft.XBOL.Commons.Enums;
using Odasoft.XBOL.Data;
using Odasoft.XBOL.Data.Repositories;
using Odasoft.XBOL.DTO;
using Odasoft.XBOL.Models;
using Xunit;
using TicketingClientInterface = Odasoft.XBOL.Business.ITicketingClient;
using TicketingSaleType = Odasoft.XBOL.Business.SaleType;
using TicketingSeatsIoPriceDTO = Odasoft.XBOL.Business.SeatsIoPriceDTO;
using TicketingTicketTypeDTO = Odasoft.XBOL.Business.TicketTypeDTO;

namespace Odasoft.XBOL.ClientAPI.Tests.Services;

public sealed class OrderRepositoryTests
{
    private const long MxPhoneRegionId = 2;

    [Fact]
    public async Task GetMyEventsAsync_includes_bundle_orders_without_tickets_as_pass_summary()
    {
        await using var database = await TestDatabase.CreateAsync();
        var client = CreateClient();
        database.Context.Clients.Add(client);
        await database.Context.SaveChangesAsync();

        var bundlePass = CreateBundlePass(client);
        database.Context.BundlePasses.Add(bundlePass);
        await database.Context.SaveChangesAsync();

        database.Context.Orders.Add(new Order
        {
            ClientId = client.Id,
            Reference = "LEGACY-ORDER",
            OrderType = OrderType.Bundle,
            Status = OrderStatus.Paid,
            SaleChannel = SaleChannel.Online,
            PaidAt = DateTimeOffset.UtcNow.AddMonths(-6),
            CreatedAt = DateTimeOffset.UtcNow.AddMonths(-6),
            UpdatedAt = DateTimeOffset.UtcNow.AddMonths(-6),
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty,
            Items =
            [
                new OrderItem
                {
                    ItemType = ItemType.BundlePass,
                    ItemReferenceId = bundlePass.Id,
                    Price = bundlePass.Price
                }
            ]
        });
        await database.Context.SaveChangesAsync();

        var repository = new OrderRepository(database.Context);

        var result = await repository.GetMyEventsAsync(1, 10, OrderType.Bundle, client.Id);

        result.TotalCount.Should().Be(1);
        var order = result.Orders.Should().ContainSingle().Subject;
        order.Reference.Should().Be("LEGACY-ORDER");
        var passSummary = order.Tickets.Should().ContainSingle().Subject;
        passSummary.EventScheduleId.Should().Be(0);
        passSummary.EventId.Should().Be(0);
        passSummary.TicketType.Should().Be(ItemType.BundlePass.ToString());
        passSummary.SeasonName.Should().Be("Imported Season Pass");

        var service = CreateOrderService(database.Context);

        var serviceResult = await service.GetMyEventsAsync(1, 10, OrderType.Bundle, client.Id);

        var item = serviceResult.Items.Should().ContainSingle().Subject;
        item.Name.Should().Be("Imported Season Pass");
        item.IsSeasonPass.Should().BeTrue();
        item.CanViewTickets.Should().BeFalse();
    }

    [Fact]
    public async Task GetMyEventsAsync_uses_event_banner_for_materialized_bundle_ticket()
    {
        await using var database = await TestDatabase.CreateAsync();
        var client = CreateClient();
        database.Context.Clients.Add(client);
        await database.Context.SaveChangesAsync();

        var bundlePass = CreateBundlePass(client);
        database.Context.BundlePasses.Add(bundlePass);
        await database.Context.SaveChangesAsync();
        var bundlePassId = bundlePass.Id;
        var bundleId = bundlePass.Bundle.Id;
        var bundlePrice = bundlePass.Price;
        database.Context.ChangeTracker.Clear();

        var materializedTicket = await database.SeedEventTicketAsync(client);
        var ticket = materializedTicket.Ticket;

        var order = new Order
        {
            ClientId = client.Id,
            Reference = "BUNDLE-TICKETS",
            OrderType = OrderType.Bundle,
            Status = OrderStatus.Paid,
            SaleChannel = SaleChannel.Online,
            PaidAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty,
            Items =
            [
                new OrderItem
                {
                    ItemType = ItemType.BundlePass,
                    ItemReferenceId = bundlePassId,
                    Price = bundlePrice
                }
            ]
        };
        database.Context.Orders.Add(order);
        await database.Context.SaveChangesAsync();

        ticket.OriginalOrderId = order.Id;
        database.Context.BundlePassEventTickets.Add(new BundlePassEventTicket
        {
            BundlePassId = bundlePassId,
            TicketId = ticket.Id
        });
        await database.Context.SaveChangesAsync();
        database.Context.Media.Add(CreateMedia(ClientSaleType.Bundle, bundleId, "https://cdn.example.test/bundle-banner.jpg"));
        database.Context.Media.Add(CreateMedia(ClientSaleType.Event, materializedTicket.EventId, "https://cdn.example.test/deleted-event-banner.jpg", blobDeletedAt: DateTimeOffset.UtcNow, order: -1));
        database.Context.Media.Add(CreateMedia(ClientSaleType.Event, materializedTicket.EventId, "https://cdn.example.test/event-banner.jpg"));
        await database.Context.SaveChangesAsync();

        var repository = new OrderRepository(database.Context);
        var result = await repository.GetMyEventsAsync(1, 10, OrderType.Bundle, client.Id);

        var projectedTicket = result.Orders.Should().ContainSingle().Subject.Tickets.Should().ContainSingle().Subject;
        projectedTicket.BannerUrl.Should().Be("https://cdn.example.test/event-banner.jpg");
        projectedTicket.LegacyPosterUrl.Should().Be("https://example.test/event-poster.png");

        var service = CreateOrderService(database.Context);
        var serviceResult = await service.GetMyEventsAsync(1, 10, OrderType.Bundle, client.Id);

        var item = serviceResult.Items.Should().ContainSingle().Subject;
        item.EventImage.Should().Be("https://cdn.example.test/event-banner.jpg");
        item.CanViewTickets.Should().BeTrue();
    }

    [Fact]
    public async Task GetMyEventsAsync_uses_bundle_banner_for_ticketless_bundle_summary()
    {
        await using var database = await TestDatabase.CreateAsync();
        var client = CreateClient();
        database.Context.Clients.Add(client);
        await database.Context.SaveChangesAsync();

        var bundlePass = CreateBundlePass(client);
        database.Context.BundlePasses.Add(bundlePass);
        await database.Context.SaveChangesAsync();
        database.Context.Media.Add(CreateMedia(ClientSaleType.Bundle, bundlePass.Bundle.Id, "https://cdn.example.test/deleted-bundle-banner.jpg", blobDeletedAt: DateTimeOffset.UtcNow, order: -1));
        database.Context.Media.Add(CreateMedia(ClientSaleType.Bundle, bundlePass.Bundle.Id, "https://cdn.example.test/ticketless-bundle-banner.jpg"));
        database.Context.Orders.Add(new Order
        {
            ClientId = client.Id,
            Reference = "BUNDLE-SUMMARY",
            OrderType = OrderType.Bundle,
            Status = OrderStatus.Paid,
            SaleChannel = SaleChannel.Online,
            PaidAt = DateTimeOffset.UtcNow.AddMonths(-6),
            CreatedAt = DateTimeOffset.UtcNow.AddMonths(-6),
            UpdatedAt = DateTimeOffset.UtcNow.AddMonths(-6),
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty,
            Items =
            [
                new OrderItem
                {
                    ItemType = ItemType.BundlePass,
                    ItemReferenceId = bundlePass.Id,
                    Price = bundlePass.Price
                }
            ]
        });
        await database.Context.SaveChangesAsync();

        var repository = new OrderRepository(database.Context);
        var result = await repository.GetMyEventsAsync(1, 10, OrderType.Bundle, client.Id);

        result.Orders.Should().ContainSingle().Subject.Tickets.Should().ContainSingle()
            .Which.BannerUrl.Should().Be("https://cdn.example.test/ticketless-bundle-banner.jpg");

        var service = CreateOrderService(database.Context);
        var serviceResult = await service.GetMyEventsAsync(1, 10, OrderType.Bundle, client.Id);

        serviceResult.Items.Should().ContainSingle().Subject.EventImage.Should().Be("https://cdn.example.test/ticketless-bundle-banner.jpg");
    }

    [Fact]
    public async Task GetOrderToRenovate_uses_bundle_pass_seats_when_order_has_no_tickets()
    {
        await using var database = await TestDatabase.CreateAsync();
        var client = CreateClient();
        database.Context.Clients.Add(client);
        await database.Context.SaveChangesAsync();

        var bundlePass = CreateBundlePass(client);
        database.Context.BundlePasses.Add(bundlePass);
        await database.Context.SaveChangesAsync();

        var latestBundle = CreateRenewalBundle(bundlePass.Bundle);
        database.Context.Bundles.Add(latestBundle);
        var order = new Order
        {
            ClientId = client.Id,
            Reference = "LEGACY-RENEW",
            OrderType = OrderType.Bundle,
            Status = OrderStatus.Paid,
            SaleChannel = SaleChannel.Online,
            PaidAt = DateTimeOffset.UtcNow.AddMonths(-6),
            CreatedAt = DateTimeOffset.UtcNow.AddMonths(-6),
            UpdatedAt = DateTimeOffset.UtcNow.AddMonths(-6),
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty,
            Items =
            [
                new OrderItem
                {
                    ItemType = ItemType.BundlePass,
                    ItemReferenceId = bundlePass.Id,
                    Price = bundlePass.Price
                }
            ]
        };
        database.Context.Orders.Add(order);
        await database.Context.SaveChangesAsync();

        var service = CreateOrderService(database.Context);

        var result = await service.GetOrderToRenovate(order.Id, client.Id);

        result.BundleId.Should().Be(latestBundle.Id);
        result.BundleKey.Should().Be("renewal-bundle");
        result.RelatedOrderId.Should().Be(order.Id);
        result.PreviousSeats.Should().ContainSingle()
            .Which.Should().Match<MyEventSeatDTO>(seat =>
                seat.Section == "Club" && seat.Seats == "A-1");
        result.PreviousSeatPrices.Should().ContainSingle()
            .Which.ExternalSeatObjectKey.Should().Be("A-1");
    }

    [Fact]
    public async Task GetOrderToRenovate_omits_ticketless_bundle_pass_seats_already_renewed_by_tracking_code()
    {
        await using var database = await TestDatabase.CreateAsync();
        var client = CreateClient();
        database.Context.Clients.Add(client);
        await database.Context.SaveChangesAsync();

        var sourcePassA1 = CreateBundlePass(client);
        var sourcePassA2 = CreateBundlePass(client, sourcePassA1.Bundle, "A-2");
        database.Context.BundlePasses.AddRange(sourcePassA1, sourcePassA2);
        await database.Context.SaveChangesAsync();

        var renewalBundle = CreateRenewalBundle(sourcePassA1.Bundle);
        database.Context.Bundles.Add(renewalBundle);
        var sourceOrder = CreateRenewalOrder(client, sourcePassA1);
        sourceOrder.Items.Add(new OrderItem
        {
            ItemType = ItemType.BundlePass,
            ItemReferenceId = sourcePassA2.Id,
            Price = sourcePassA2.Price
        });
        database.Context.Orders.Add(sourceOrder);
        await database.Context.SaveChangesAsync();

        var renewedPassA1 = CreateBundlePass(client, renewalBundle, "A-1");
        database.Context.BundlePasses.Add(renewedPassA1);
        await database.Context.SaveChangesAsync();
        database.Context.Orders.Add(CreateRenewedOrder(client, sourceOrder, renewedPassA1));
        await database.Context.SaveChangesAsync();

        var service = CreateOrderService(database.Context);

        var result = await service.GetOrderToRenovate(sourceOrder.Id, client.Id);

        result.PreviousSeatPrices.Should().ContainSingle()
            .Which.ExternalSeatObjectKey.Should().Be("A-2");
        result.PreviousSeats.Should().ContainSingle()
            .Which.Seats.Should().Be("A-2");
    }

    [Fact]
    public async Task GetOrderToRenovate_rejects_ticketless_bundle_pass_source_order_when_all_seats_are_renewed()
    {
        await using var database = await TestDatabase.CreateAsync();
        var client = CreateClient();
        database.Context.Clients.Add(client);
        await database.Context.SaveChangesAsync();

        var sourcePass = CreateBundlePass(client);
        database.Context.BundlePasses.Add(sourcePass);
        await database.Context.SaveChangesAsync();

        var renewalBundle = CreateRenewalBundle(sourcePass.Bundle);
        database.Context.Bundles.Add(renewalBundle);
        var sourceOrder = CreateRenewalOrder(client, sourcePass);
        database.Context.Orders.Add(sourceOrder);
        await database.Context.SaveChangesAsync();

        var renewedPass = CreateBundlePass(client, renewalBundle, "A-1");
        database.Context.BundlePasses.Add(renewedPass);
        await database.Context.SaveChangesAsync();
        database.Context.Orders.Add(CreateRenewedOrder(client, sourceOrder, renewedPass));
        await database.Context.SaveChangesAsync();

        var service = CreateOrderService(database.Context);

        var act = () => service.GetOrderToRenovate(sourceOrder.Id, client.Id);

        await act.Should().ThrowAsync<Exception>()
            .WithMessage("*order cannot be renewed*");
    }

    [Fact]
    public async Task GetOrderToRenovate_uses_ticketing_base_price_when_category_price_has_no_wrapper_price()
    {
        await using var database = await TestDatabase.CreateAsync();
        var client = CreateClient();
        database.Context.Clients.Add(client);
        await database.Context.SaveChangesAsync();

        var bundlePass = CreateBundlePass(client);
        bundlePass.BundleSeat!.BaseSeat!.BaseRow!.BaseSection!.BaseZone.ExternalZoneKey = 10;
        database.Context.BundlePasses.Add(bundlePass);
        await database.Context.SaveChangesAsync();

        var latestBundle = CreateRenewalBundle(bundlePass.Bundle);
        database.Context.Bundles.Add(latestBundle);
        var order = new Order
        {
            ClientId = client.Id,
            Reference = "LEGACY-RENEW",
            OrderType = OrderType.Bundle,
            Status = OrderStatus.Paid,
            SaleChannel = SaleChannel.Online,
            PaidAt = DateTimeOffset.UtcNow.AddMonths(-6),
            CreatedAt = DateTimeOffset.UtcNow.AddMonths(-6),
            UpdatedAt = DateTimeOffset.UtcNow.AddMonths(-6),
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty,
            Items =
            [
                new OrderItem
                {
                    ItemType = ItemType.BundlePass,
                    ItemReferenceId = bundlePass.Id,
                    Price = bundlePass.Price
                }
            ]
        };
        database.Context.Orders.Add(order);
        await database.Context.SaveChangesAsync();
        var ticketingClient = Substitute.For<TicketingClientInterface>();
        ticketingClient.GetSeatsIoPricesAsync(TicketingSaleType.Bundle, latestBundle.Id)
            .Returns([
                new TicketingSeatsIoPriceDTO
                {
                    Category = 10,
                    BasePrice = 1.18m,
                    BasePriceListItemId = 1280,
                    TicketTypes =
                    [
                        new TicketingTicketTypeDTO { PriceListItemId = 1281, TicketType = "Xolos Precio Nuevo", Price = 2.36m },
                        new TicketingTicketTypeDTO { PriceListItemId = 1280, TicketType = "General", Price = 1.18m, Primary = true }
                    ]
                }
            ]);
        var service = CreateOrderService(database.Context, ticketingClient);

        var result = await service.GetOrderToRenovate(order.Id, client.Id);

        result.PreviousSeatPrices.Should().ContainSingle()
            .Which.Should().Match<SeatDTO>(seat =>
                seat.ExternalSeatObjectKey == "A-1" &&
                seat.PriceOverride == 1.18m &&
                seat.PriceListItemId == 1280);
    }

    [Fact]
    public async Task GetOrderToRenovate_uses_ticketing_base_price_before_ticket_type_labels()
    {
        await using var database = await TestDatabase.CreateAsync();
        var client = CreateClient();
        database.Context.Clients.Add(client);
        await database.Context.SaveChangesAsync();

        var bundlePass = CreateBundlePass(client);
        bundlePass.BundleSeat!.BaseSeat!.BaseRow!.BaseSection!.BaseZone.ExternalZoneKey = 10;
        database.Context.BundlePasses.Add(bundlePass);
        await database.Context.SaveChangesAsync();

        var latestBundle = CreateRenewalBundle(bundlePass.Bundle);
        database.Context.Bundles.Add(latestBundle);
        var order = CreateRenewalOrder(client, bundlePass);
        database.Context.Orders.Add(order);
        await database.Context.SaveChangesAsync();
        var ticketingClient = Substitute.For<TicketingClientInterface>();
        ticketingClient.GetSeatsIoPricesAsync(TicketingSaleType.Bundle, latestBundle.Id)
            .Returns([
                new TicketingSeatsIoPriceDTO
                {
                    Category = 10,
                    BasePrice = 1.18m,
                    BasePriceListItemId = 1280,
                    TicketTypes =
                    [
                        new TicketingTicketTypeDTO { PriceListItemId = 1281, TicketType = "General", Price = 2.36m },
                        new TicketingTicketTypeDTO { PriceListItemId = 1280, TicketType = "Xolos Precio Nuevo", Price = 1.18m, Primary = true }
                    ]
                }
            ]);
        var service = CreateOrderService(database.Context, ticketingClient);

        var result = await service.GetOrderToRenovate(order.Id, client.Id);

        result.PreviousSeatPrices.Should().ContainSingle()
            .Which.Should().Match<SeatDTO>(seat =>
                seat.ExternalSeatObjectKey == "A-1" &&
                seat.PriceOverride == 1.18m &&
                seat.PriceListItemId == 1280);
    }

    [Fact]
    public async Task GetOrderToRenovatePrices_uses_ticketing_base_price_when_category_price_has_no_wrapper_price()
    {
        await using var database = await TestDatabase.CreateAsync();
        var client = CreateClient();
        database.Context.Clients.Add(client);
        await database.Context.SaveChangesAsync();

        var bundlePass = CreateBundlePass(client);
        bundlePass.BundleSeat!.BaseSeat!.BaseRow!.BaseSection!.BaseZone.ExternalZoneKey = 10;
        database.Context.BundlePasses.Add(bundlePass);
        await database.Context.SaveChangesAsync();

        var latestBundle = CreateRenewalBundle(bundlePass.Bundle);
        database.Context.Bundles.Add(latestBundle);
        var order = CreateRenewalOrder(client, bundlePass);
        database.Context.Orders.Add(order);
        await database.Context.SaveChangesAsync();
        var ticketingClient = Substitute.For<TicketingClientInterface>();
        ticketingClient.GetSeatsIoPricesAsync(TicketingSaleType.Bundle, latestBundle.Id)
            .Returns([
                new TicketingSeatsIoPriceDTO
                {
                    Category = 10,
                    BasePrice = 1.18m,
                    BasePriceListItemId = 1280,
                    TicketTypes =
                    [
                        new TicketingTicketTypeDTO { PriceListItemId = 1281, TicketType = "Xolos Precio Nuevo", Price = 2.36m },
                        new TicketingTicketTypeDTO { PriceListItemId = 1280, TicketType = "General", Price = 1.18m, Primary = true }
                    ]
                }
            ]);
        var service = CreateOrderService(database.Context, ticketingClient);

        var result = await service.GetOrderToRenovatePrices(order.Id, client.Id);

        result.Should().ContainSingle()
            .Which.Should().Match<SeatDTO>(seat =>
                seat.ExternalSeatObjectKey == "A-1" &&
                seat.PriceOverride == 1.18m &&
                seat.PriceListItemId == 1280);
    }

    [Fact]
    public async Task GetOrderToRenovatePrices_prefers_object_override_over_category_price()
    {
        await using var database = await TestDatabase.CreateAsync();
        var client = CreateClient();
        database.Context.Clients.Add(client);
        await database.Context.SaveChangesAsync();

        var bundlePass = CreateBundlePass(client);
        bundlePass.BundleSeat!.BaseSeat!.BaseRow!.BaseSection!.BaseZone.ExternalZoneKey = 10;
        database.Context.BundlePasses.Add(bundlePass);
        await database.Context.SaveChangesAsync();

        var latestBundle = CreateRenewalBundle(bundlePass.Bundle);
        database.Context.Bundles.Add(latestBundle);
        var order = CreateRenewalOrder(client, bundlePass);
        database.Context.Orders.Add(order);
        await database.Context.SaveChangesAsync();
        var ticketingClient = Substitute.For<TicketingClientInterface>();
        ticketingClient.GetSeatsIoPricesAsync(TicketingSaleType.Bundle, latestBundle.Id)
            .Returns([
                new TicketingSeatsIoPriceDTO { Category = 10, Price = 1.18m, PriceListItemId = 1280 },
                new TicketingSeatsIoPriceDTO { Objects = ["A-1"], Price = 3.50m, PriceListItemId = 1400 }
            ]);
        var service = CreateOrderService(database.Context, ticketingClient);

        var result = await service.GetOrderToRenovatePrices(order.Id, client.Id);

        result.Should().ContainSingle()
            .Which.Should().Match<SeatDTO>(seat =>
                seat.ExternalSeatObjectKey == "A-1" &&
                seat.PriceOverride == 3.50m &&
                seat.PriceListItemId == 1400);
    }

    [Fact]
    public async Task GetOrderToRenovatePrices_keeps_price_fields_null_when_no_price_matches()
    {
        await using var database = await TestDatabase.CreateAsync();
        var client = CreateClient();
        database.Context.Clients.Add(client);
        await database.Context.SaveChangesAsync();

        var bundlePass = CreateBundlePass(client);
        database.Context.BundlePasses.Add(bundlePass);
        await database.Context.SaveChangesAsync();

        var latestBundle = CreateRenewalBundle(bundlePass.Bundle);
        database.Context.Bundles.Add(latestBundle);
        var order = CreateRenewalOrder(client, bundlePass);
        database.Context.Orders.Add(order);
        await database.Context.SaveChangesAsync();
        var ticketingClient = Substitute.For<TicketingClientInterface>();
        ticketingClient.GetSeatsIoPricesAsync(TicketingSaleType.Bundle, latestBundle.Id)
            .Returns([
                new TicketingSeatsIoPriceDTO { Category = 999, Price = 9.99m, PriceListItemId = 1999 }
            ]);
        var service = CreateOrderService(database.Context, ticketingClient);

        var result = await service.GetOrderToRenovatePrices(order.Id, client.Id);

        result.Should().ContainSingle()
            .Which.Should().Match<SeatDTO>(seat =>
                seat.ExternalSeatObjectKey == "A-1" &&
                seat.PriceOverride == null &&
                seat.PriceListItemId == null);
    }

    [Fact]
    public async Task GetMyEventsDiagnosticsAsync_counts_bundle_filter_boundaries()
    {
        await using var database = await TestDatabase.CreateAsync();
        var client = CreateClient();
        var otherClient = CreateClient();
        otherClient.Email = "other@example.com";
        otherClient.PhoneNumber = "+526641111111";
        database.Context.Clients.AddRange(client, otherClient);
        await database.Context.SaveChangesAsync();

        var inactivePass = CreateBundlePass(client);
        inactivePass.Status = BundlePassStatus.Suspended;
        database.Context.BundlePasses.Add(inactivePass);
        var activePass = CreateBundlePass(client);
        activePass.TrackingCode = "B-1";
        database.Context.BundlePasses.Add(activePass);
        var otherPass = CreateBundlePass(otherClient);
        otherPass.TrackingCode = "C-1";
        database.Context.BundlePasses.Add(otherPass);
        await database.Context.SaveChangesAsync();

        database.Context.Orders.Add(new Order
        {
            ClientId = client.Id,
            Reference = "INACTIVE-PASS",
            OrderType = OrderType.Bundle,
            Status = OrderStatus.Paid,
            SaleChannel = SaleChannel.Online,
            PaidAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty,
            Items =
            [
                new OrderItem
                {
                    ItemType = ItemType.BundlePass,
                    ItemReferenceId = inactivePass.Id,
                    Price = inactivePass.Price
                }
            ]
        });
        database.Context.Orders.Add(new Order
        {
            ClientId = client.Id,
            Reference = "ACTIVE-PASS",
            OrderType = OrderType.Bundle,
            Status = OrderStatus.Paid,
            SaleChannel = SaleChannel.Online,
            PaidAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty,
            Items =
            [
                new OrderItem
                {
                    ItemType = ItemType.BundlePass,
                    ItemReferenceId = activePass.Id,
                    Price = activePass.Price
                }
            ]
        });
        await database.Context.SaveChangesAsync();
        var repository = new OrderRepository(database.Context);

        var result = await repository.GetMyEventsDiagnosticsAsync(OrderType.Bundle, client.Id);

        result.PaidOrdersByType.Should().Be(2);
        result.BundlePassItemOrders.Should().Be(2);
        result.OwnedBundlePasses.Should().Be(2);
        result.ActiveOwnedBundlePasses.Should().Be(1);
        result.PublishedActiveBundlePassItemOrders.Should().Be(1);
    }

    private static Client CreateClient()
    {
        return new Client
        {
            ClientType = ClientType.Individual,
            Email = "buyer@example.com",
            PhoneRegionCodeId = MxPhoneRegionId,
            PhoneNumber = "+526641234567",
            FullName = "Imported Client",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = Guid.Empty,
            UpdatedAt = DateTimeOffset.UtcNow,
            UpdatedBy = Guid.Empty
        };
    }

    private static BundlePass CreateBundlePass(Client client)
    {
        var now = DateTimeOffset.UtcNow;
        var bundle = CreateBundle(now);
        return CreateBundlePass(client, bundle, "A-1");
    }

    private static BundlePass CreateBundlePass(Client client, Bundle bundle, string seatKey)
    {
        var now = DateTimeOffset.UtcNow;
        var baseSection = new BaseSection
        {
            BaseZone = new BaseZone
            {
                VenueMap = bundle.VenueMap,
                Name = "Zona Club",
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = Guid.Empty,
                UpdatedBy = Guid.Empty
            },
            Name = "Club",
            SectionType = SectionType.Vip
        };
        var bundleSection = new BundleSection
        {
            Bundle = bundle,
            BaseSection = baseSection,
            DisplayName = "Club",
            TotalSeats = 1,
            AvailableSeats = 0
        };
        var seatNumber = seatKey.Split('-').Last();

        return new BundlePass
        {
            Client = client,
            Bundle = bundle,
            BundleSeat = new BundleSeat
            {
                BundleSection = bundleSection,
                BaseSeat = new BaseSeat
                {
                    BaseRow = new BaseRow
                    {
                        BaseSection = baseSection,
                        RowLabel = "A"
                    },
                    SeatNumber = seatNumber,
                    SeatType = SeatType.Standard
                },
                ExternalSeatObjectKey = seatKey
            },
            TrackingCode = seatKey,
            PrivateToken = Guid.NewGuid().ToString("N"),
            BundlePassType = BundlePassType.Full,
            Status = BundlePassStatus.Active,
            IsDigital = true,
            Price = 100,
            PurchasedAt = now.AddMonths(-6),
            CreatedAt = now.AddMonths(-6),
            UpdatedAt = now.AddMonths(-6),
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty
        };
    }

    private static Bundle CreateBundle(DateTimeOffset now)
    {
        return new Bundle
        {
            VenueMap = CreateVenueMap(now),
            Name = "Imported Season Pass",
            Status = EventStatus.Published,
            BundleType = BundleType.SeasonPass,
            BundlePricingType = BundlePricingType.Composite,
            PosterImageUrl = "https://example.test/imported-season.png",
            StartDate = now.AddMonths(-8),
            EndDate = now.AddMonths(-2),
            CreatedAt = now.AddMonths(-8),
            UpdatedAt = now.AddMonths(-8),
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty
        };
    }

    private static Bundle CreateRenewalBundle(Bundle previousBundle)
    {
        var now = DateTimeOffset.UtcNow;
        return new Bundle
        {
            VenueMap = previousBundle.VenueMap,
            PreviousBundleId = previousBundle.Id,
            Name = "Renewal Season Pass",
            Status = EventStatus.Published,
            BundleType = BundleType.SeasonPass,
            BundlePricingType = BundlePricingType.Composite,
            ExternalKey = "renewal-bundle",
            StartDate = now.AddMonths(1),
            EndDate = now.AddMonths(8),
            PublishedDate = now.AddDays(-1),
            RenewalStartDate = now.AddDays(-1),
            RenewalEndDate = now.AddDays(30),
            PreSaleDate = now.AddDays(31),
            OnSaleDate = now.AddDays(60),
            OffSaleDate = now.AddMonths(8),
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty
        };
    }

    private static Order CreateRenewalOrder(Client client, BundlePass bundlePass)
    {
        return new Order
        {
            ClientId = client.Id,
            Reference = $"LEGACY-RENEW-{Guid.NewGuid():N}",
            OrderType = OrderType.Bundle,
            Status = OrderStatus.Paid,
            SaleChannel = SaleChannel.Online,
            PaidAt = DateTimeOffset.UtcNow.AddMonths(-6),
            CreatedAt = DateTimeOffset.UtcNow.AddMonths(-6),
            UpdatedAt = DateTimeOffset.UtcNow.AddMonths(-6),
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty,
            Items =
            [
                new OrderItem
                {
                    ItemType = ItemType.BundlePass,
                    ItemReferenceId = bundlePass.Id,
                    Price = bundlePass.Price
                }
            ]
        };
    }

    private static Order CreateRenewedOrder(Client client, Order sourceOrder, BundlePass renewedPass)
    {
        return new Order
        {
            ClientId = client.Id,
            Reference = $"LEGACY-RENEWED-{Guid.NewGuid():N}",
            RelatedOrderId = sourceOrder.Id,
            OrderType = OrderType.Bundle,
            Status = OrderStatus.Paid,
            SaleChannel = SaleChannel.Online,
            PaidAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty,
            Items =
            [
                new OrderItem
                {
                    ItemType = ItemType.BundlePass,
                    ItemReferenceId = renewedPass.Id,
                    Price = renewedPass.Price
                }
            ]
        };
    }

    private static Media CreateMedia(
        ClientSaleType referenceType,
        long referenceId,
        string url,
        BlobAssetStatus status = BlobAssetStatus.Available,
        DateTimeOffset? blobDeletedAt = null,
        int order = 0)
    {
        return new Media
        {
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            MediaType = ClientMediaType.Banner,
            BlobAsset = new BlobAsset
            {
                BucketName = "media",
                ObjectName = $"{referenceType}/{Guid.NewGuid():N}.jpg",
                FileName = "banner.jpg",
                ContentType = "image/jpeg",
                Url = url,
                Status = status,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                DeletedAt = blobDeletedAt
            },
            Order = order,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty
        };
    }

    private static VenueMap CreateVenueMap(DateTimeOffset now)
    {
        return new VenueMap
        {
            Venue = new Venue
            {
                Name = "Imported Stadium",
                Category = VenueCategory.Stadium,
                Status = VenueStatus.Active,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = Guid.Empty,
                UpdatedBy = Guid.Empty
            },
            Name = "Imported Venue Map",
            ExternalMapKey = Guid.NewGuid().ToString("N"),
            Capacity = 1,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty
        };
    }

    private static OrderService CreateOrderService(
        XBOLDbContext context,
        TicketingClientInterface? ticketingClient = null)
    {
        var orderRepository = new OrderRepository(context);
        var clientRepository = new ClientRepository(context);
        var clientLoginIdentifierRepository = new ClientLoginIdentifierRepository(context);
        var seasonRepository = new SeasonRepository(context);
        var seasonPassRepository = new SeasonPassRepository(context);
        var mediaRepository = new MediaRepository(context);
        var bundleRepository = new BundleRepository(context);
        var bundlePassRepository = new BundlePassRepository(context);

        return new OrderService(
            orderRepository,
            clientRepository,
            new ClientService(
                orderRepository,
                clientRepository,
                clientLoginIdentifierRepository),
            new EventScheduleRepository(context),
            new EventSeatRepository(context),
            new TicketRepository(context),
            seasonPassRepository,
            new SeasonPassEventTicketRepository(context),
            seasonRepository,
            new SeasonSeatRepository(context),
            new EventRepository(context),
            new SeasonService(
                seasonRepository,
                mediaRepository,
                orderRepository,
                seasonPassRepository),
            new EventScheduleService(new EventScheduleRepository(context)),
            ticketingClient ?? Substitute.For<TicketingClientInterface>(),
            Substitute.For<ILogger<OrderService>>(),
            new ClientCreditTransactionService(
                new ClientCreditTransactionRepository(context),
                new SequenceTrackerService(new SequenceTrackerRepository(context))),
            bundlePassRepository,
            new BundleService(
                bundleRepository,
                bundlePassRepository,
                orderRepository,
                mediaRepository,
                Substitute.For<ILogger<BundleService>>()),
            bundleRepository,
            new BundlePassEventTicketRepository(context));
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection connection;

        private TestDatabase(SqliteConnection connection, XBOLDbContext context)
        {
            this.connection = connection;
            Context = context;
        }

        public XBOLDbContext Context { get; }

        public static async Task<TestDatabase> CreateAsync()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<XBOLDbContext>()
                .UseSqlite(connection)
                .Options;
            var context = new XBOLDbContext(options);
            await context.Database.EnsureCreatedAsync();
            context.Set<PhoneRegionCode>().Add(new PhoneRegionCode
            {
                Id = MxPhoneRegionId,
                RegionCode = "MX",
                DialCode = "52",
                FlagEmoji = string.Empty
            });
            await context.SaveChangesAsync();
            return new TestDatabase(connection, context);
        }

        public async Task<MaterializedTicketFixture> SeedEventTicketAsync(Client client)
        {
            var now = DateTimeOffset.UtcNow;
            var venueMap = CreateVenueMap(now);
            var baseSection = new BaseSection
            {
                BaseZone = new BaseZone
                {
                    VenueMap = venueMap,
                    Name = "Event Zone",
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedBy = Guid.Empty,
                    UpdatedBy = Guid.Empty
                },
                Name = "Event Section",
                SectionType = SectionType.General
            };
            var eventItem = new Event
            {
                Id = 10001,
                VenueMap = venueMap,
                Name = "Bundle Child Event",
                Status = EventStatus.Published,
                PosterImageUrl = "https://example.test/event-poster.png",
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = Guid.Empty,
                UpdatedBy = Guid.Empty
            };
            var schedule = new EventSchedule
            {
                Event = eventItem,
                StartDateTime = now.AddDays(7),
                EndDateTime = now.AddDays(7).AddHours(2),
                OnSaleDate = now.AddDays(-1),
                OffSaleDate = now.AddDays(6),
                Status = ScheduleStatus.OnSale,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = Guid.Empty,
                UpdatedBy = Guid.Empty
            };
            var eventSection = new EventSection
            {
                EventSchedule = schedule,
                BaseSection = baseSection,
                DisplayName = "Main",
                TotalSeats = 1,
                AvailableSeats = 0
            };
            var eventSeat = new EventSeat
            {
                EventSection = eventSection,
                BaseSeat = new BaseSeat
                {
                    BaseRow = new BaseRow
                    {
                        BaseSection = baseSection,
                        RowLabel = "A"
                    },
                    SeatNumber = "1",
                    SeatType = SeatType.Standard
                },
                ExternalSeatObjectKey = "A-1"
            };

            Context.Set<EventSeat>().Add(eventSeat);
            await Context.SaveChangesAsync();

            var ticket = new Ticket
            {
                EventScheduleId = schedule.Id,
                EventSectionId = eventSection.Id,
                EventSeatId = eventSeat.Id,
                OriginalClientId = client.Id,
                CurrentClientId = client.Id,
                TicketCode = Guid.NewGuid().ToString("N"),
                TicketType = TicketType.Adult.ToString(),
                PrivateToken = Guid.NewGuid().ToString("N"),
                SectionLabelSnapshot = "Main",
                SeatLabelSnapshot = "A-1",
                PricePaid = 100,
                Status = TicketStatus.Issued,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = Guid.Empty,
                UpdatedBy = Guid.Empty
            };

            Context.Tickets.Add(ticket);
            await Context.SaveChangesAsync();
            return new MaterializedTicketFixture(ticket, eventItem.Id);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }

    private sealed record MaterializedTicketFixture(Ticket Ticket, long EventId);
}
