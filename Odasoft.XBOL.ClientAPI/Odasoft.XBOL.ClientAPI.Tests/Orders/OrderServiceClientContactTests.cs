using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Odasoft.XBOL.Business;
using Odasoft.XBOL.Business.Services;
using Odasoft.XBOL.Commons.Enums;
using Odasoft.XBOL.Data;
using Odasoft.XBOL.Data.Repositories;
using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.ClientAPI.Tests.Orders;

public sealed class OrderServiceClientContactTests
{
    [Fact]
    public async Task Order_contact_upsert_does_not_match_linked_client_by_phone_only()
    {
        await using var db = await TestOrderDatabase.CreateAsync();
        var linked = await db.InsertClientAsync(
            email: "linked@example.com",
            phoneNumber: "+526641234567",
            fullName: "Linked Client",
            firebaseUid: "firebase-linked");
        var service = db.CreateOrderService();

        var client = await InvokeUpsertClientFromOrderContactAsync(service, new ClientInfoRequest
        {
            PhoneNumber = "526641234567",
            FullName = "Box Office Name"
        });

        Assert.NotEqual(linked.Id, client.Id);
        Assert.Equal(2, await db.Context.Clients.CountAsync());

        var storedLinked = await db.Context.Clients.SingleAsync(client => client.Id == linked.Id);
        Assert.Equal("linked@example.com", storedLinked.Email);
        Assert.Equal("+526641234567", storedLinked.PhoneNumber);
        Assert.Equal("Linked Client", storedLinked.FullName);
    }

    [Fact]
    public async Task Order_contact_upsert_does_not_match_empty_phone_when_contact_phone_has_no_digits()
    {
        await using var db = await TestOrderDatabase.CreateAsync();
        var existing = await db.InsertClientAsync(
            email: string.Empty,
            phoneNumber: string.Empty,
            fullName: "No Phone Client");
        var service = db.CreateOrderService();

        var client = await InvokeUpsertClientFromOrderContactAsync(service, new ClientInfoRequest
        {
            PhoneNumber = "abc",
            FullName = "Invalid Phone Contact"
        });

        Assert.NotEqual(existing.Id, client.Id);
        Assert.Equal(2, await db.Context.Clients.CountAsync());

        var storedExisting = await db.Context.Clients.SingleAsync(client => client.Id == existing.Id);
        Assert.Equal(string.Empty, storedExisting.PhoneNumber);
        Assert.Equal("No Phone Client", storedExisting.FullName);
    }

    [Fact]
    public async Task Order_contact_upsert_prefers_unclaimed_client_when_phone_matches_duplicates()
    {
        await using var db = await TestOrderDatabase.CreateAsync();
        var linked = await db.InsertClientAsync(
            email: "linked@example.com",
            phoneNumber: "+526641234567",
            fullName: "Linked Client",
            firebaseUid: "firebase-linked");
        var unclaimed = await db.InsertClientAsync(
            email: string.Empty,
            phoneNumber: "526641234567",
            fullName: "Unclaimed Client",
            firebaseUid: null);
        var service = db.CreateOrderService();

        var client = await InvokeUpsertClientFromOrderContactAsync(service, new ClientInfoRequest
        {
            PhoneNumber = "(526) 641-234-567",
            FullName = "Box Office Update"
        });

        Assert.Equal(unclaimed.Id, client.Id);
        Assert.Equal(2, await db.Context.Clients.CountAsync());

        var storedLinked = await db.Context.Clients.SingleAsync(client => client.Id == linked.Id);
        Assert.Equal("firebase-linked", storedLinked.FirebaseUid);
        Assert.Equal("Linked Client", storedLinked.FullName);
        Assert.Equal("+526641234567", storedLinked.PhoneNumber);

        var storedUnclaimed = await db.Context.Clients.SingleAsync(client => client.Id == unclaimed.Id);
        Assert.Null(storedUnclaimed.FirebaseUid);
        Assert.Equal("Box Office Update", storedUnclaimed.FullName);
        Assert.Equal("526641234567", storedUnclaimed.PhoneNumber);
    }

    [Fact]
    public async Task Order_contact_upsert_prefers_linked_client_when_email_matches_duplicates()
    {
        await using var db = await TestOrderDatabase.CreateAsync();
        var unclaimed = await db.InsertClientAsync(
            email: "duplicate@example.com",
            phoneNumber: "526641111111",
            fullName: "Unclaimed Client",
            firebaseUid: null);
        var linked = await db.InsertClientAsync(
            email: "duplicate@example.com",
            phoneNumber: "+526642222222",
            fullName: "Linked Client",
            firebaseUid: "firebase-linked");
        var service = db.CreateOrderService();

        var originalUpdatedAt = linked.UpdatedAt;

        var client = await InvokeUpsertClientFromOrderContactAsync(service, new ClientInfoRequest
        {
            Email = " DUPLICATE@example.com ",
            PhoneNumber = "+526649999999",
            FullName = "Box Office Update"
        });

        Assert.Equal(linked.Id, client.Id);
        Assert.Equal(2, await db.Context.Clients.CountAsync());

        var storedLinked = await db.Context.Clients.SingleAsync(client => client.Id == linked.Id);
        Assert.Equal("firebase-linked", storedLinked.FirebaseUid);
        Assert.Equal("Linked Client", storedLinked.FullName);
        Assert.Equal("+526642222222", storedLinked.PhoneNumber);
        Assert.Equal(originalUpdatedAt, storedLinked.UpdatedAt);

        var storedUnclaimed = await db.Context.Clients.SingleAsync(client => client.Id == unclaimed.Id);
        Assert.Null(storedUnclaimed.FirebaseUid);
        Assert.Equal("Unclaimed Client", storedUnclaimed.FullName);
        Assert.Equal("526641111111", storedUnclaimed.PhoneNumber);
    }

    [Fact]
    public async Task Order_contact_upsert_preserves_existing_phone_when_email_matches_and_contact_phone_has_no_digits()
    {
        await using var db = await TestOrderDatabase.CreateAsync();
        var existing = await db.InsertClientAsync(
            email: "preserve-phone@example.com",
            phoneNumber: "526641111111",
            fullName: "Existing Client",
            firebaseUid: null);
        var service = db.CreateOrderService();

        var client = await InvokeUpsertClientFromOrderContactAsync(service, new ClientInfoRequest
        {
            Email = "preserve-phone@example.com",
            PhoneNumber = "N/A",
            FullName = "Updated Client"
        });

        Assert.Equal(existing.Id, client.Id);
        Assert.Equal(1, await db.Context.Clients.CountAsync());

        var stored = await db.Context.Clients.SingleAsync();
        Assert.Equal("526641111111", stored.PhoneNumber);
        Assert.Equal("Updated Client", stored.FullName);
    }

    [Fact]
    public async Task Order_contact_upsert_uses_newest_unclaimed_client_when_email_matches_duplicates()
    {
        await using var db = await TestOrderDatabase.CreateAsync();
        var older = await db.InsertClientAsync(
            email: "unclaimed@example.com",
            phoneNumber: "526641111111",
            fullName: "Older Unclaimed",
            firebaseUid: null);
        var newer = await db.InsertClientAsync(
            email: "unclaimed@example.com",
            phoneNumber: "526642222222",
            fullName: "Newer Unclaimed",
            firebaseUid: null);
        var service = db.CreateOrderService();

        var client = await InvokeUpsertClientFromOrderContactAsync(service, new ClientInfoRequest
        {
            Email = "unclaimed@example.com",
            FullName = "Newest Update"
        });

        Assert.Equal(newer.Id, client.Id);
        Assert.Equal(2, await db.Context.Clients.CountAsync());

        var storedNewer = await db.Context.Clients.SingleAsync(client => client.Id == newer.Id);
        Assert.Null(storedNewer.FirebaseUid);
        Assert.Equal("Newest Update", storedNewer.FullName);

        var storedOlder = await db.Context.Clients.SingleAsync(client => client.Id == older.Id);
        Assert.Null(storedOlder.FirebaseUid);
        Assert.Equal("Older Unclaimed", storedOlder.FullName);
    }

    private static async Task<Client> InvokeUpsertClientFromOrderContactAsync(
        OrderService service,
        ClientInfoRequest clientInfo)
    {
        var method = typeof(OrderService).GetMethod(
            "UpsertClientFromOrderContactAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(method);
        var task = (Task<Client>)method.Invoke(service, [clientInfo])!;
        return await task;
    }

    private sealed class TestOrderDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection connection;

        private TestOrderDatabase(SqliteConnection connection, XBOLDbContext context)
        {
            this.connection = connection;
            Context = context;
        }

        public XBOLDbContext Context { get; }

        public static async Task<TestOrderDatabase> CreateAsync()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<XBOLDbContext>()
                .UseSqlite(connection)
                .Options;
            var context = new XBOLDbContext(options);
            await context.Database.EnsureCreatedAsync();

            return new TestOrderDatabase(connection, context);
        }

        public OrderService CreateOrderService()
        {
            return new OrderService(
                new OrderRepository(Context),
                new ClientRepository(Context),
                new EventScheduleRepository(Context),
                new EventSeatRepository(Context),
                new TicketRepository(Context),
                new SeasonPassRepository(Context),
                new SeasonPassEventTicketRepository(Context),
                new SeasonRepository(Context),
                new SeasonSeatRepository(Context),
                new EventRepository(Context),
                new SeasonService(
                    new SeasonRepository(Context),
                    new OrderRepository(Context),
                    new SeasonPassRepository(Context)),
                new EventScheduleService(new EventScheduleRepository(Context)),
                NullLogger<OrderService>.Instance);
        }

        public async Task<Client> InsertClientAsync(
            string email,
            string phoneNumber,
            string fullName,
            string? firebaseUid = "firebase-registered")
        {
            var client = new Client
            {
                ClientType = ClientType.Individual,
                Email = email,
                PhoneNumber = phoneNumber,
                FullName = fullName,
                FirebaseUid = firebaseUid,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = Guid.Empty,
                UpdatedAt = DateTimeOffset.UtcNow,
                UpdatedBy = Guid.Empty
            };

            Context.Clients.Add(client);
            await Context.SaveChangesAsync();
            return client;
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
