using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Odasoft.XBOL.Business;
using Odasoft.XBOL.Business.Services;
using Odasoft.XBOL.Commons.Enums;
using Odasoft.XBOL.Data;
using Odasoft.XBOL.Data.Repositories;
using Odasoft.XBOL.DTO;
using Odasoft.XBOL.Models;
using Xunit;

namespace Odasoft.XBOL.ClientAPI.Tests.Services;

public sealed class PaymentLinkServiceTests
{
    [Fact]
    public async Task ConfirmCheckoutAsync_DelegatesSettlementToTicketingAndMarksPaymentLinkPaid()
    {
        await using var database = await TestDatabase.CreateAsync();
        var now = DateTimeOffset.UtcNow;
        var order = new Order
        {
            Client = new Client
            {
                ClientType = ClientType.Individual,
                Email = "buyer@example.com",
                FullName = "Buyer",
                PhoneRegionCodeId = 1,
                PhoneNumber = "5551234567",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = Guid.Empty,
                UpdatedBy = Guid.Empty
            },
            Reference = "ORD-LINK-1",
            SubTotal = 500m,
            Total = 500m,
            Status = OrderStatus.Pending,
            OrderType = OrderType.Bundle,
            SaleChannel = SaleChannel.Online,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty
        };
        var paymentLink = new PaymentLink
        {
            Order = order,
            Code = "pay-link-code",
            Reference = "pay-link-reference",
            Status = PaymentLinkStatus.Pending,
            ActivationDateTime = now.AddMinutes(-1),
            ExpirationDateTime = now.AddHours(1),
            CreatedAt = now
        };
        database.Context.Set<PaymentLink>().Add(paymentLink);
        await database.Context.SaveChangesAsync();

        var ticketingClient = Substitute.For<ITicketingClient>();
        ticketingClient
            .ConfirmCheckoutAsync(Arg.Any<ConfirmCheckoutRequest>())
            .Returns(new ConfirmCheckoutResponse
            {
                OrderId = order.Id,
                OrderStatus = OrderStatus.Paid.ToString(),
                PaymentStatus = PaymentStatus.Captured.ToString(),
                TicketsIssued = 1,
                Reference = order.Reference
            });
        var service = CreateService(database.Context, ticketingClient);

        var result = await service.ConfirmCheckoutAsync("pay-link-code", new ConfirmPaymentLinkCheckoutRequest
        {
            OrderRefId = "evo-order-1",
            ResultIndicator = "success-indicator-1",
            SuccessIndicator = "success-indicator-1"
        });

        result.Should().Be(order.Id);
        await ticketingClient.Received(1).ConfirmCheckoutAsync(Arg.Is<ConfirmCheckoutRequest>(request =>
            request.LocalOrderId == order.Id &&
            request.OrderRefId == "evo-order-1" &&
            request.ResultIndicator == "success-indicator-1"));

        var refreshedLink = await database.Context.Set<PaymentLink>().SingleAsync();
        refreshedLink.Status.Should().Be(PaymentLinkStatus.Paid);
        refreshedLink.PaidAt.Should().NotBeNull();
        database.Context.Set<Payment>().Should().BeEmpty();
    }

    private static PaymentLinkService CreateService(XBOLDbContext context, ITicketingClient ticketingClient)
    {
        return new PaymentLinkService(
            new PaymentLinkRepository(context),
            new MediaRepository(context),
            new SeasonPassRepository(context),
            new SeasonRepository(context),
            null!,
            new PaymentRepository(context),
            new OrderRepository(context),
            new TicketRepository(context),
            ticketingClient);
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
                Id = 1,
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
