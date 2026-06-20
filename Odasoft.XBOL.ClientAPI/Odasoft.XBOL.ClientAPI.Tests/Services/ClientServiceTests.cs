using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Odasoft.XBOL.Business.Services;
using Odasoft.XBOL.Commons.Enums;
using Odasoft.XBOL.Commons.Requests;
using Odasoft.XBOL.Data;
using Odasoft.XBOL.Data.Repositories;
using Odasoft.XBOL.Models;
using Xunit;

namespace Odasoft.XBOL.ClientAPI.Tests.Services;

public sealed class ClientServiceTests
{
    private const long MxPhoneRegionId = 2;

    [Fact]
    public async Task GetClientByContactAsync_does_not_resolve_by_contact_email()
    {
        await using var database = await TestDatabase.CreateAsync();
        var client = CreateClient("buyer@example.com", "");
        client.FirebaseUid = "firebase-uid";
        database.Context.Clients.Add(client);
        await database.Context.SaveChangesAsync();
        var service = CreateService(database.Context);

        var result = await service.GetClientByContactAsync(new ClientContactRequest
        {
            Email = "buyer@example.com"
        });

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetClientByContactAsync_resolves_by_verified_email_login_identifier()
    {
        await using var database = await TestDatabase.CreateAsync();
        var client = CreateClient("contact@example.com", "");
        client.FirebaseUid = "firebase-uid";
        database.Context.Clients.Add(client);
        database.Context.ClientLoginIdentifiers.Add(CreateLoginIdentifier(
            client,
            ClientLoginIdentifierType.Email,
            "buyer@example.com"));
        await database.Context.SaveChangesAsync();
        var service = CreateService(database.Context);

        var result = await service.GetClientByContactAsync(new ClientContactRequest
        {
            Email = "Buyer@Example.COM"
        });

        result.Should().NotBeNull();
        result!.Id.Should().Be(client.Id);
    }

    [Fact]
    public async Task GetClientByContactAsync_requires_region_context_for_national_phone_identifier()
    {
        await using var database = await TestDatabase.CreateAsync();
        var client = CreateClient("", "+526641234567");
        client.FirebaseUid = "firebase-uid";
        database.Context.Clients.Add(client);
        database.Context.ClientLoginIdentifiers.Add(CreateLoginIdentifier(
            client,
            ClientLoginIdentifierType.Phone,
            "+526641234567"));
        await database.Context.SaveChangesAsync();
        var service = CreateService(database.Context);

        var dialCodeResult = await service.GetClientByContactAsync(new ClientContactRequest
        {
            Phone = "6641234567",
            PhoneIsoCode = "+52",
            PhoneCode = "+52"
        });

        dialCodeResult.Should().BeNull();

        var result = await service.GetClientByContactAsync(new ClientContactRequest
        {
            Phone = "6641234567",
            PhoneIsoCode = "MX",
            PhoneCode = "MX"
        });

        result.Should().NotBeNull();
        result!.Id.Should().Be(client.Id);
    }

    private static ClientService CreateService(XBOLDbContext context)
    {
        return new ClientService(
            new OrderRepository(context),
            new ClientRepository(context),
            new ClientLoginIdentifierRepository(context));
    }

    private static Client CreateClient(string email, string phoneNumber)
    {
        return new Client
        {
            ClientType = ClientType.Individual,
            Email = email,
            PhoneRegionCodeId = MxPhoneRegionId,
            PhoneNumber = phoneNumber,
            FullName = "Existing Client",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = Guid.Empty,
            UpdatedAt = DateTimeOffset.UtcNow,
            UpdatedBy = Guid.Empty
        };
    }

    private static ClientLoginIdentifier CreateLoginIdentifier(
        Client client,
        ClientLoginIdentifierType type,
        string normalizedValue)
    {
        return new ClientLoginIdentifier
        {
            Client = client,
            Type = type,
            NormalizedValue = normalizedValue,
            VerifiedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = Guid.Empty,
            UpdatedAt = DateTimeOffset.UtcNow,
            UpdatedBy = Guid.Empty
        };
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
            context.Set<PhoneRegionCode>().AddRange(
                CreatePhoneRegion(1, "US", "1"),
                CreatePhoneRegion(MxPhoneRegionId, "MX", "52"),
                CreatePhoneRegion(3, "CA", "1"));
            await context.SaveChangesAsync();
            return new TestDatabase(connection, context);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }

    private static PhoneRegionCode CreatePhoneRegion(long id, string regionCode, string dialCode)
    {
        return new PhoneRegionCode
        {
            Id = id,
            RegionCode = regionCode,
            DialCode = dialCode,
            FlagEmoji = string.Empty
        };
    }
}
