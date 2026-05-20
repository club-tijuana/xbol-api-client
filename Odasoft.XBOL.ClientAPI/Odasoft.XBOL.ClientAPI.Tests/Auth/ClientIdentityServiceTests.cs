using System.Security.Claims;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Odasoft.XBOL.ClientAPI.Auth;
using Odasoft.XBOL.ClientAPI.Services;
using Odasoft.XBOL.Commons.Enums;
using Odasoft.XBOL.Data;
using Odasoft.XBOL.Data.Repositories;
using Odasoft.XBOL.DTO.Requests;
using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.ClientAPI.Tests.Auth;

public sealed class ClientIdentityServiceTests
{
    [Fact]
    public async Task RegisterAsync_creates_client_and_returns_custom_token()
    {
        await using var db = await TestAuthDatabase.CreateAsync();
        var firebasePassword = new FakeFirebasePasswordAuthClient("firebase-123", "client@example.com");
        var firebaseTenant = new FakeFirebaseTenantAuthClient("custom-token");
        var service = db.CreateService(firebasePassword, firebaseTenant);

        var response = await service.RegisterAsync(new RegisterRequest
        {
            Email = "client@example.com",
            Password = "Password123!",
            FullName = "Client Name",
            PhoneNumber = "+526641234567"
        }, CancellationToken.None);

        Assert.Equal("firebase-123", response.FirebaseUid);
        Assert.Equal("custom-token", response.CustomToken);
        Assert.Equal("linked", response.OnboardingStatus);
        Assert.Equal("pending", response.VerificationStatus);
        Assert.Equal("firebase-123", response.Client.FirebaseUid);
        Assert.Equal("Client Name", response.Client.FullName);
        Assert.Equal("client@example.com", response.Client.Email);
        Assert.Equal("+526641234567", response.Client.PhoneNumber);
        Assert.Equal("+52", response.Client.PhoneCode);

        var client = await db.Context.Clients.SingleAsync();
        Assert.Equal("firebase-123", client.FirebaseUid);
        Assert.Equal("client@example.com", client.Email);
        Assert.Equal("Client Name", client.FullName);
        Assert.Equal("+526641234567", client.PhoneNumber);
        Assert.True(client.IsActive);
        Assert.Single(firebaseTenant.UpdatedUsers);
        Assert.Null(firebaseTenant.Updates.Single().PhoneNumber);
        Assert.Empty(firebaseTenant.DeletedUsers);
    }

    [Fact]
    public async Task RegisterAsync_creates_client_without_phone_as_empty_string()
    {
        await using var db = await TestAuthDatabase.CreateAsync();
        var firebasePassword = new FakeFirebasePasswordAuthClient("firebase-no-phone", "no-phone@example.com");
        var firebaseTenant = new FakeFirebaseTenantAuthClient("no-phone-custom-token");
        var service = db.CreateService(firebasePassword, firebaseTenant);

        var response = await service.RegisterAsync(new RegisterRequest
        {
            Email = "no-phone@example.com",
            Password = "Password123!",
            FullName = "No Phone",
            PhoneNumber = " "
        }, CancellationToken.None);

        Assert.Equal("firebase-no-phone", response.Client.FirebaseUid);
        Assert.Equal(string.Empty, response.Client.PhoneNumber);
        Assert.Equal(string.Empty, (await db.Context.Clients.SingleAsync()).PhoneNumber);
    }

    [Fact]
    public async Task RegisterAsync_creates_new_client_when_unclaimed_client_has_same_email()
    {
        await using var db = await TestAuthDatabase.CreateAsync();
        var unclaimed = await db.InsertClientAsync("existing@example.com", "+526641111111", "Existing Name");
        var firebasePassword = new FakeFirebasePasswordAuthClient("firebase-existing", "existing@example.com");
        var firebaseTenant = new FakeFirebaseTenantAuthClient("existing-custom-token");
        var service = db.CreateService(firebasePassword, firebaseTenant);

        var response = await service.RegisterAsync(new RegisterRequest
        {
            Email = "existing@example.com",
            Password = "Password123!",
            FullName = "Updated Existing",
            PhoneNumber = "+526649999999"
        }, CancellationToken.None);

        Assert.NotEqual(unclaimed.Id, response.Client.Id);
        Assert.Equal("firebase-existing", response.Client.FirebaseUid);
        Assert.Equal("Updated Existing", response.Client.FullName);
        Assert.Equal("+526649999999", response.Client.PhoneNumber);
        Assert.Equal("pending", response.VerificationStatus);
        Assert.Equal(2, await db.Context.Clients.CountAsync());
        var originalClient = await db.Context.Clients.SingleAsync(client => client.Id == unclaimed.Id);
        Assert.Null(originalClient.FirebaseUid);
        Assert.Equal("Existing Name", originalClient.FullName);
        Assert.Equal("+526641111111", originalClient.PhoneNumber);
    }

    [Fact]
    public async Task RegisterAsync_does_not_create_client_when_custom_token_creation_fails()
    {
        await using var db = await TestAuthDatabase.CreateAsync();
        var firebasePassword = new FakeFirebasePasswordAuthClient("firebase-token-failure", "token-failure@example.com");
        var firebaseTenant = new FakeFirebaseTenantAuthClient(
            "unused-token",
            new InvalidOperationException("custom token failed"));
        var service = db.CreateService(firebasePassword, firebaseTenant);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterAsync(new RegisterRequest
        {
            Email = "token-failure@example.com",
            Password = "Password123!",
            FullName = "Token Failure",
            PhoneNumber = "+526641111111"
        }, CancellationToken.None));

        Assert.Equal(["firebase-token-failure"], firebaseTenant.DeletedUsers);
        Assert.Empty(await db.Context.Clients.ToListAsync());
    }

    [Fact]
    public async Task RegisterAsync_deletes_firebase_user_with_uncanceled_cleanup_token_when_request_is_canceled()
    {
        await using var db = await TestAuthDatabase.CreateAsync();
        using var requestCancellation = new CancellationTokenSource();
        var firebasePassword = new FakeFirebasePasswordAuthClient("firebase-cleanup", "cleanup@example.com");
        var firebaseTenant = new FakeFirebaseTenantAuthClient(
            "unused-token",
            new InvalidOperationException("custom token failed"),
            requestCancellation.Cancel);
        var service = db.CreateService(firebasePassword, firebaseTenant);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterAsync(new RegisterRequest
        {
            Email = "cleanup@example.com",
            Password = "Password123!",
            FullName = "Cleanup Failure",
            PhoneNumber = "+526641111111"
        }, requestCancellation.Token));

        Assert.Equal(["firebase-cleanup"], firebaseTenant.DeletedUsers);
        Assert.Equal([false], firebaseTenant.DeleteCancellationTokenWasCanceled);
        Assert.Equal([true], firebaseTenant.DeleteCancellationTokenCanBeCanceled);
        Assert.Empty(await db.Context.Clients.ToListAsync());
    }

    [Fact]
    public async Task RegisterAsync_does_not_link_existing_client_when_custom_token_creation_fails()
    {
        await using var db = await TestAuthDatabase.CreateAsync();
        await db.InsertClientAsync("link-token-failure@example.com", "+526641111111", "Unclaimed");
        var firebasePassword = new FakeFirebasePasswordAuthClient("firebase-link-token-failure", "link-token-failure@example.com");
        var firebaseTenant = new FakeFirebaseTenantAuthClient(
            "unused-token",
            new InvalidOperationException("custom token failed"));
        var service = db.CreateService(firebasePassword, firebaseTenant);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterAsync(new RegisterRequest
        {
            Email = "link-token-failure@example.com",
            Password = "Password123!",
            FullName = "Still Unclaimed",
            PhoneNumber = "+526642222222"
        }, CancellationToken.None));

        Assert.Equal(["firebase-link-token-failure"], firebaseTenant.DeletedUsers);
        var client = await db.Context.Clients.SingleAsync();
        Assert.Null(client.FirebaseUid);
        Assert.Equal("Unclaimed", client.FullName);
        Assert.Equal("+526641111111", client.PhoneNumber);
    }

    [Fact]
    public async Task RegisterAsync_creates_new_client_when_multiple_unclaimed_clients_have_same_email()
    {
        await using var db = await TestAuthDatabase.CreateAsync();
        await db.InsertClientAsync("ambiguous@example.com", "+526641111111", "One");
        await db.InsertClientAsync("ambiguous@example.com", "+526642222222", "Two");
        var firebasePassword = new FakeFirebasePasswordAuthClient("firebase-ambiguous", "ambiguous@example.com");
        var firebaseTenant = new FakeFirebaseTenantAuthClient("ambiguous-custom-token");
        var service = db.CreateService(firebasePassword, firebaseTenant);

        var response = await service.RegisterAsync(new RegisterRequest
        {
            Email = "ambiguous@example.com",
            Password = "Password123!",
            FullName = "Ambiguous",
            PhoneNumber = "+526643333333"
        }, CancellationToken.None);

        Assert.Equal("firebase-ambiguous", response.Client.FirebaseUid);
        Assert.Equal(3, await db.Context.Clients.CountAsync());
        Assert.Equal(2, await db.Context.Clients.CountAsync(client => client.FirebaseUid == null));
        Assert.Empty(firebaseTenant.DeletedUsers);
    }

    [Fact]
    public async Task RegisterAsync_rejects_firebase_email_collision()
    {
        await using var db = await TestAuthDatabase.CreateAsync();
        var firebasePassword = new ThrowingFirebasePasswordAuthClient(new ClientAuthException(
            "EMAIL_EXISTS",
            StatusCodes.Status409Conflict,
            ClientAuthProblemCodes.FirebaseEmailExists));
        var firebaseTenant = new FakeFirebaseTenantAuthClient("unused-token");
        var service = db.CreateService(firebasePassword, firebaseTenant);

        var exception = await Assert.ThrowsAsync<ClientAuthException>(() => service.RegisterAsync(new RegisterRequest
        {
            Email = "exists@example.com",
            Password = "Password123!",
            FullName = "Existing",
            PhoneNumber = "+526641111111"
        }, CancellationToken.None));

        Assert.Equal(StatusCodes.Status409Conflict, exception.StatusCode);
        Assert.Equal(ClientAuthProblemCodes.FirebaseEmailExists, exception.Code);
        Assert.Empty(firebaseTenant.DeletedUsers);
        Assert.Empty(await db.Context.Clients.ToListAsync());
    }

    [Fact]
    public async Task RegisterAsync_deletes_firebase_user_when_profile_update_fails()
    {
        await using var db = await TestAuthDatabase.CreateAsync();
        var firebasePassword = new FakeFirebasePasswordAuthClient(
            "firebase-profile-failure",
            "profile-failure@example.com");
        var firebaseTenant = new FakeFirebaseTenantAuthClient(
            "unused-token",
            updateUserException: new InvalidOperationException("profile update failed"));
        var service = db.CreateService(firebasePassword, firebaseTenant);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterAsync(new RegisterRequest
        {
            Email = "profile-failure@example.com",
            Password = "Password123!",
            FullName = "Profile Failure",
            PhoneNumber = "+526641111111"
        }, CancellationToken.None));

        Assert.Equal(["firebase-profile-failure"], firebaseTenant.DeletedUsers);
        Assert.Empty(await db.Context.Clients.ToListAsync());
    }

    [Fact]
    public async Task RegisterAsync_rejects_invalid_registration()
    {
        await using var db = await TestAuthDatabase.CreateAsync();
        var service = db.CreateService();

        var exception = await Assert.ThrowsAsync<ClientAuthException>(() => service.RegisterAsync(new RegisterRequest
        {
            Email = " ",
            Password = "Password123!",
            FullName = "Invalid",
            PhoneNumber = "+526641111111"
        }, CancellationToken.None));

        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
        Assert.Equal(ClientAuthProblemCodes.InvalidRegistration, exception.Code);
        Assert.Empty(await db.Context.Clients.ToListAsync());
    }

    [Fact]
    public async Task GetMeAsync_returns_unlinked_profile_without_creating_client()
    {
        await using var db = await TestAuthDatabase.CreateAsync();
        var service = db.CreateService();

        var response = await service.GetMeAsync(CreatePrincipal("firebase-unlinked", emailVerified: true));

        Assert.Equal("firebase-unlinked", response.FirebaseUid);
        Assert.Null(response.Client);
        Assert.Equal("unlinked", response.OnboardingStatus);
        Assert.Equal("verified", response.VerificationStatus);
        Assert.Empty(await db.Context.Clients.ToListAsync());
    }

    [Fact]
    public async Task GetMeAsync_returns_linked_profile_without_mutating_client()
    {
        await using var db = await TestAuthDatabase.CreateAsync();
        var client = await db.InsertClientAsync("linked@example.com", "+526641111111", "Linked");
        client.FirebaseUid = "firebase-linked-profile";
        client.UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1);
        await db.Context.SaveChangesAsync();
        var originalUpdatedAt = client.UpdatedAt;
        var service = db.CreateService();

        var response = await service.GetMeAsync(CreatePrincipal("firebase-linked-profile", emailVerified: true));

        Assert.Equal("firebase-linked-profile", response.FirebaseUid);
        Assert.Equal("linked", response.OnboardingStatus);
        Assert.Equal("verified", response.VerificationStatus);
        Assert.NotNull(response.Client);
        Assert.Equal(client.Id, response.Client.Id);
        Assert.Equal(1, await db.Context.Clients.CountAsync());
        Assert.Equal(originalUpdatedAt, (await db.Context.Clients.SingleAsync()).UpdatedAt);
    }

    [Fact]
    public async Task RequireCurrentClientAsync_rejects_unlinked_identity_with_problem_code()
    {
        await using var db = await TestAuthDatabase.CreateAsync();
        var service = db.CreateService();

        var exception = await Assert.ThrowsAsync<ClientAuthException>(() =>
            service.RequireCurrentClientAsync(CreatePrincipal("firebase-missing", emailVerified: true)));

        Assert.Equal(StatusCodes.Status404NotFound, exception.StatusCode);
        Assert.Equal(ClientAuthProblemCodes.UnlinkedClientProfile, exception.Code);
    }

    [Fact]
    public async Task RequireCurrentClientAsync_rejects_unverified_linked_identity()
    {
        await using var db = await TestAuthDatabase.CreateAsync();
        var client = await db.InsertClientAsync("unverified@example.com", "+526641111111", "Unverified");
        client.FirebaseUid = "firebase-unverified";
        await db.Context.SaveChangesAsync();
        var service = db.CreateService();

        var exception = await Assert.ThrowsAsync<ClientAuthException>(() =>
            service.RequireCurrentClientAsync(CreatePrincipal("firebase-unverified", emailVerified: false)));

        Assert.Equal(StatusCodes.Status403Forbidden, exception.StatusCode);
        Assert.Equal(ClientAuthProblemCodes.VerificationRequired, exception.Code);
    }

    private static ClaimsPrincipal CreatePrincipal(string firebaseUid, bool emailVerified)
    {
        var claims = new List<Claim>
        {
            new(GcipAuthenticationHandler.FirebaseUidClaimType, firebaseUid),
            new(ClaimTypes.Email, $"{firebaseUid}@example.com"),
            new(GcipAuthenticationHandler.EmailVerifiedClaimType, emailVerified.ToString().ToLowerInvariant()),
            new(GcipAuthenticationHandler.SignInProviderClaimType, "password")
        };

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
    }

    private sealed class FakeFirebasePasswordAuthClient(string firebaseUid, string firebaseEmail) : IFirebasePasswordAuthClient
    {
        public int SignUpCalls { get; private set; }

        public Task<FirebasePasswordAuthResult> SignUpAsync(
            string email,
            string password,
            CancellationToken cancellationToken)
        {
            SignUpCalls++;
            return Task.FromResult(new FirebasePasswordAuthResult(
                firebaseUid,
                firebaseEmail));
        }
    }

    private sealed class ThrowingFirebasePasswordAuthClient(ClientAuthException exception) : IFirebasePasswordAuthClient
    {
        public Task<FirebasePasswordAuthResult> SignUpAsync(
            string email,
            string password,
            CancellationToken cancellationToken)
        {
            throw exception;
        }
    }

    private sealed class FakeFirebaseTenantAuthClient(
        string customToken,
        Exception? customTokenException = null,
        Action? beforeCustomTokenException = null,
        Exception? updateUserException = null) : IFirebaseTenantAuthClient
    {
        public List<string> DeletedUsers { get; } = [];
        public List<bool> DeleteCancellationTokenWasCanceled { get; } = [];
        public List<bool> DeleteCancellationTokenCanBeCanceled { get; } = [];
        public List<string> UpdatedUsers { get; } = [];
        public List<FirebaseClientUserUpdate> Updates { get; } = [];

        public Task UpdateUserAsync(FirebaseClientUserUpdate update, CancellationToken cancellationToken)
        {
            if (updateUserException is not null)
            {
                throw updateUserException;
            }

            UpdatedUsers.Add(update.FirebaseUid);
            Updates.Add(update);
            return Task.CompletedTask;
        }

        public Task DeleteUserAsync(string firebaseUid, CancellationToken cancellationToken)
        {
            DeletedUsers.Add(firebaseUid);
            DeleteCancellationTokenWasCanceled.Add(cancellationToken.IsCancellationRequested);
            DeleteCancellationTokenCanBeCanceled.Add(cancellationToken.CanBeCanceled);
            return Task.CompletedTask;
        }

        public Task<string> CreateCustomTokenAsync(string firebaseUid, CancellationToken cancellationToken)
        {
            if (customTokenException is not null)
            {
                beforeCustomTokenException?.Invoke();
                throw customTokenException;
            }

            return Task.FromResult(customToken);
        }
    }

    private sealed class TestAuthDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection connection;

        private TestAuthDatabase(
            SqliteConnection connection,
            XBOLDbContext context)
        {
            this.connection = connection;
            Context = context;
        }

        public XBOLDbContext Context { get; }

        public static async Task<TestAuthDatabase> CreateAsync()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<XBOLDbContext>()
                .UseSqlite(connection)
                .Options;
            var context = new XBOLDbContext(options);
            await context.Database.EnsureCreatedAsync();

            return new TestAuthDatabase(connection, context);
        }

        public ClientIdentityService CreateService(
            IFirebasePasswordAuthClient? passwordAuthClient = null,
            IFirebaseTenantAuthClient? tenantAuthClient = null)
        {
            return new ClientIdentityService(
                new ClientRepository(Context),
                tenantAuthClient ?? new FakeFirebaseTenantAuthClient("custom-token"),
                passwordAuthClient ?? new FakeFirebasePasswordAuthClient("firebase-default", "default@example.com"));
        }

        public async Task<Client> InsertClientAsync(string email, string phoneNumber, string fullName)
        {
            var client = new Client
            {
                ClientType = ClientType.Individual,
                Email = email,
                PhoneNumber = phoneNumber,
                FullName = fullName,
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
