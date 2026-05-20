using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Odasoft.XBOL.Business.Services;
using Odasoft.XBOL.Commons.Enums;
using Odasoft.XBOL.Commons.Requests;
using Odasoft.XBOL.Data;
using Odasoft.XBOL.Data.Repositories;
using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.ClientAPI.Tests.Clients;

public sealed class ClientServiceContactTests
{
    [Fact]
    public async Task GetClientByContactAsync_returns_linked_client_when_unclaimed_contact_also_matches()
    {
        await using var db = await TestClientDatabase.CreateAsync();
        await db.InsertClientAsync(
            email: "recipient@example.com",
            phoneNumber: "+526641234567",
            fullName: "Unclaimed Checkout Client",
            firebaseUid: null);
        var linked = await db.InsertClientAsync(
            email: "recipient@example.com",
            phoneNumber: "+526641234567",
            fullName: "Linked Recipient",
            firebaseUid: "firebase-linked");
        var service = db.CreateClientService();

        var client = await service.GetClientByContactAsync(new ClientContactRequest
        {
            Email = " RECIPIENT@example.com ",
            Phone = "+526641234567"
        });

        Assert.NotNull(client);
        Assert.Equal(linked.Id, client.Id);
        Assert.Equal("firebase-linked", client.UserId);
    }

    [Fact]
    public async Task GetClientByContactAsync_returns_linked_client_by_email_when_phone_is_missing()
    {
        await using var db = await TestClientDatabase.CreateAsync();
        var linked = await db.InsertClientAsync(
            email: "recipient@example.com",
            phoneNumber: "+526641234567",
            fullName: "Linked Recipient",
            firebaseUid: "firebase-linked");
        var service = db.CreateClientService();

        var client = await service.GetClientByContactAsync(new ClientContactRequest
        {
            Email = "recipient@example.com",
            Phone = ""
        });

        Assert.NotNull(client);
        Assert.Equal(linked.Id, client.Id);
    }

    [Fact]
    public async Task GetClientByContactAsync_returns_linked_client_by_email_when_phone_differs()
    {
        await using var db = await TestClientDatabase.CreateAsync();
        var linked = await db.InsertClientAsync(
            email: "recipient@example.com",
            phoneNumber: "+526641234567",
            fullName: "Linked Recipient",
            firebaseUid: "firebase-linked");
        var service = db.CreateClientService();

        var client = await service.GetClientByContactAsync(new ClientContactRequest
        {
            Email = "recipient@example.com",
            Phone = "+526649999999"
        });

        Assert.NotNull(client);
        Assert.Equal(linked.Id, client.Id);
    }

    [Fact]
    public async Task GetClientByContactAsync_does_not_match_normalized_phone_for_different_email()
    {
        await using var db = await TestClientDatabase.CreateAsync();
        await db.InsertClientAsync(
            email: "other@example.com",
            phoneNumber: "+526641234567",
            fullName: "Other Linked Client",
            firebaseUid: "firebase-other");
        var service = db.CreateClientService();

        var client = await service.GetClientByContactAsync(new ClientContactRequest
        {
            Email = "recipient@example.com",
            Phone = "6641234567",
            PhoneCode = "+52"
        });

        Assert.Null(client);
    }

    private sealed class TestClientDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection connection;

        private TestClientDatabase(SqliteConnection connection, XBOLDbContext context)
        {
            this.connection = connection;
            Context = context;
        }

        public XBOLDbContext Context { get; }

        public static async Task<TestClientDatabase> CreateAsync()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<XBOLDbContext>()
                .UseSqlite(connection)
                .Options;
            var context = new XBOLDbContext(options);
            await context.Database.EnsureCreatedAsync();

            return new TestClientDatabase(connection, context);
        }

        public ClientService CreateClientService()
        {
            return new ClientService(
                new OrderRepository(Context),
                new ClientRepository(Context));
        }

        public async Task<Client> InsertClientAsync(
            string email,
            string phoneNumber,
            string fullName,
            string? firebaseUid)
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
