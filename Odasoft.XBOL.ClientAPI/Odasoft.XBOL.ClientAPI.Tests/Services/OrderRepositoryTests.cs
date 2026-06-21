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

namespace Odasoft.XBOL.ClientAPI.Tests.Services;

public sealed class OrderRepositoryTests
{
    private const long MxPhoneRegionId = 2;

    [Fact]
    public async Task GetMyEventsAsync_includes_paid_bundle_order_items_when_order_has_no_tickets()
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
        var historyItem = order.Tickets.Should().ContainSingle().Subject;
        historyItem.EventName.Should().Be("Imported Season Pass");
        historyItem.TicketType.Should().Be("BUNDLEPASS");
        historyItem.GetType().GetProperty("Source")!.GetValue(historyItem).Should().Be("OrderItem");
        historyItem.GetType().GetProperty("CanViewTickets")!.GetValue(historyItem).Should().Be(false);
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
                    SeatNumber = "1",
                    SeatType = SeatType.Standard
                },
                ExternalSeatObjectKey = "A-1"
            },
            TrackingCode = "A-1",
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

    private static OrderService CreateOrderService(XBOLDbContext context)
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
            Substitute.For<TicketingClientInterface>(),
            Substitute.For<ILogger<OrderService>>(),
            new ClientCreditTransactionService(
                new ClientCreditTransactionRepository(context),
                new SequenceTrackerService(new SequenceTrackerRepository(context))),
            bundlePassRepository,
            new BundleService(
                bundleRepository,
                bundlePassRepository,
                mediaRepository),
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

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
