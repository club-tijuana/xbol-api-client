using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Odasoft.XBOL.ClientAPI.Auth;
using Odasoft.XBOL.ClientAPI.Services;
using Odasoft.XBOL.Commons.Enums;
using Odasoft.XBOL.Data;
using Odasoft.XBOL.Data.Repositories;
using Odasoft.XBOL.DTO.Requests;
using Odasoft.XBOL.Models;
using System.Security.Claims;
using Xunit;

namespace Odasoft.XBOL.ClientAPI.Tests.Services;

public sealed class ClientIdentityServiceTests
{
    private const long UsPhoneRegionId = 1;
    private const long MxPhoneRegionId = 2;
    private const long CaPhoneRegionId = 3;

    [Fact]
    public async Task GetMeAsync_resolves_client_by_verified_phone_identifier()
    {
        await using var database = await TestDatabase.CreateAsync();
        var client = CreateClient(email: "contact@example.com", phoneNumber: "6641234567");
        database.Context.Clients.Add(client);
        database.Context.ClientLoginIdentifiers.Add(new ClientLoginIdentifier
        {
            Client = client,
            Type = ClientLoginIdentifierType.Phone,
            NormalizedValue = "+526641234567",
            VerifiedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = Guid.Empty,
            UpdatedAt = DateTimeOffset.UtcNow,
            UpdatedBy = Guid.Empty
        });
        await database.Context.SaveChangesAsync();
        var service = CreateService(database.Context);

        var result = await service.GetMeAsync(CreatePrincipal(
            firebaseUid: "phone-root-uid",
            phoneNumber: "+526641234567",
            signInProvider: "phone"));

        result.OnboardingStatus.Should().Be("linked");
        result.Client.Should().NotBeNull();
        result.Client!.Id.Should().Be(client.Id);
    }

    [Fact]
    public async Task RequireCurrentClientAsync_allows_verified_phone_identity_without_verified_email()
    {
        await using var database = await TestDatabase.CreateAsync();
        var client = CreateClient(email: "", phoneNumber: "+526641234567");
        database.Context.Clients.Add(client);
        database.Context.ClientLoginIdentifiers.Add(new ClientLoginIdentifier
        {
            Client = client,
            Type = ClientLoginIdentifierType.Phone,
            NormalizedValue = "+526641234567",
            VerifiedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = Guid.Empty,
            UpdatedAt = DateTimeOffset.UtcNow,
            UpdatedBy = Guid.Empty
        });
        await database.Context.SaveChangesAsync();
        var service = CreateService(database.Context);

        var result = await service.RequireCurrentClientAsync(CreatePrincipal(
            firebaseUid: "phone-root-uid",
            emailVerified: false,
            phoneNumber: "+526641234567",
            signInProvider: "phone"));

        result.Id.Should().Be(client.Id);
    }

    [Fact]
    public async Task GetMeAsync_fails_closed_when_verified_identifiers_match_different_clients()
    {
        await using var database = await TestDatabase.CreateAsync();
        var uidClient = CreateClient(email: "uid@example.com", phoneNumber: "");
        var phoneClient = CreateClient(email: "phone@example.com", phoneNumber: "+526641234567");
        database.Context.Clients.AddRange(uidClient, phoneClient);
        database.Context.ClientLoginIdentifiers.AddRange(
            new ClientLoginIdentifier
            {
                Client = uidClient,
                Type = ClientLoginIdentifierType.FirebaseUid,
                NormalizedValue = "root-uid",
                VerifiedAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = Guid.Empty,
                UpdatedAt = DateTimeOffset.UtcNow,
                UpdatedBy = Guid.Empty
            },
            new ClientLoginIdentifier
            {
                Client = phoneClient,
                Type = ClientLoginIdentifierType.Phone,
                NormalizedValue = "+526641234567",
                VerifiedAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = Guid.Empty,
                UpdatedAt = DateTimeOffset.UtcNow,
                UpdatedBy = Guid.Empty
            });
        await database.Context.SaveChangesAsync();
        var service = CreateService(database.Context);

        var act = () => service.GetMeAsync(CreatePrincipal(
            firebaseUid: "root-uid",
            phoneNumber: "+526641234567",
            signInProvider: "phone"));

        var exception = await act.Should().ThrowAsync<ClientAuthException>();
        exception.Which.StatusCode.Should().Be(409);
        exception.Which.Code.Should().Be(ClientAuthProblemCodes.ClientIdentityConflict);
    }

    [Fact]
    public async Task RegisterAsync_creates_phone_client_from_matching_verified_phone_identifier()
    {
        await using var database = await TestDatabase.CreateAsync();
        var mxRegion = await database.GetPhoneRegionAsync("MX");
        var service = CreateService(database.Context);

        var result = await service.RegisterAsync(
            new RegisterRequest
            {
                Identifier = "6641234567",
                IdentifierCountryCode = "MX",
                FullName = "Phone Buyer"
            },
            CreatePrincipal(
                firebaseUid: "phone-root-uid",
                phoneNumber: "+526641234567",
                signInProvider: "phone"),
            CancellationToken.None);

        result.OnboardingStatus.Should().Be("linked");
        result.FirebaseUid.Should().Be("phone-root-uid");
        result.Client.UserId.Should().Be("phone-root-uid");
        result.Client.FullName.Should().Be("Phone Buyer");
        result.Client.PhoneNumber.Should().Be("+526641234567");
        result.Client.PhoneRegionCodeId.Should().Be(mxRegion.Id);
        result.Client.PhoneCode.Should().Be("+52");
        var client = await database.Context.Clients.SingleAsync();
        client.PhoneRegionCodeId.Should().Be(mxRegion.Id);
        var identifiers = await database.Context.ClientLoginIdentifiers
            .OrderBy(x => x.Type)
            .ToListAsync();
        identifiers.Should().HaveCount(2);
        identifiers.Should().Contain(x =>
            x.Type == ClientLoginIdentifierType.FirebaseUid
            && x.NormalizedValue == "phone-root-uid"
            && x.ClientId == result.Client.Id);
        identifiers.Should().Contain(x =>
            x.Type == ClientLoginIdentifierType.Phone
            && x.NormalizedValue == "+526641234567"
            && x.ClientId == result.Client.Id);
    }

    [Fact]
    public async Task RegisterAsync_returns_country_calling_code_for_us_phone_identifier()
    {
        await using var database = await TestDatabase.CreateAsync();
        var usRegion = await database.GetPhoneRegionAsync("US");
        var service = CreateService(database.Context);

        var result = await service.RegisterAsync(
            new RegisterRequest
            {
                Identifier = "4155550100",
                IdentifierCountryCode = "US",
                FullName = "Phone Buyer"
            },
            CreatePrincipal(
                firebaseUid: "us-phone-root-uid",
                phoneNumber: "+14155550100",
                signInProvider: "phone"),
            CancellationToken.None);

        result.Client.PhoneNumber.Should().Be("+14155550100");
        result.Client.PhoneCode.Should().Be("+1");
        result.Client.PhoneRegionCodeId.Should().Be(usRegion.Id);
    }

    [Fact]
    public async Task CompleteLoginAsync_returns_unlinked_without_creating_client()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = CreateService(database.Context);

        var result = await service.CompleteLoginAsync(
            CreatePrincipal(
                firebaseUid: "unlinked-phone-root-uid",
                phoneNumber: "+526641234567",
                signInProvider: "phone"),
            CancellationToken.None);

        result.OnboardingStatus.Should().Be("unlinked");
        result.Client.Should().BeNull();
        (await database.Context.Clients.CountAsync()).Should().Be(0);
        (await database.Context.ClientLoginIdentifiers.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task CompleteLoginAsync_adds_email_identifier_only_after_firebase_email_is_verified()
    {
        await using var database = await TestDatabase.CreateAsync();
        var client = CreateClient(email: "contact@example.com", phoneNumber: "");
        client.FirebaseUid = "email-root-uid";
        database.Context.Clients.Add(client);
        database.Context.ClientLoginIdentifiers.Add(
            CreateLoginIdentifier(client, ClientLoginIdentifierType.FirebaseUid, "email-root-uid"));
        await database.Context.SaveChangesAsync();
        var service = CreateService(database.Context);

        await service.CompleteLoginAsync(
            CreatePrincipal(
                firebaseUid: "email-root-uid",
                email: "Buyer@Example.COM",
                emailVerified: false,
                signInProvider: "password"),
            CancellationToken.None);

        (await database.Context.ClientLoginIdentifiers.CountAsync(x =>
            x.Type == ClientLoginIdentifierType.Email)).Should().Be(0);

        await service.CompleteLoginAsync(
            CreatePrincipal(
                firebaseUid: "email-root-uid",
                email: "Buyer@Example.COM",
                emailVerified: true,
                signInProvider: "password"),
            CancellationToken.None);

        var emailIdentifier = await database.Context.ClientLoginIdentifiers
            .SingleAsync(x => x.Type == ClientLoginIdentifierType.Email);
        emailIdentifier.ClientId.Should().Be(client.Id);
        emailIdentifier.NormalizedValue.Should().Be("buyer@example.com");
    }

    [Fact]
    public async Task CompleteLoginAsync_fails_closed_when_verified_identifiers_match_different_clients()
    {
        await using var database = await TestDatabase.CreateAsync();
        var uidClient = CreateClient(email: "uid@example.com", phoneNumber: "");
        var emailClient = CreateClient(email: "email@example.com", phoneNumber: "");
        database.Context.Clients.AddRange(uidClient, emailClient);
        database.Context.ClientLoginIdentifiers.AddRange(
            CreateLoginIdentifier(uidClient, ClientLoginIdentifierType.FirebaseUid, "root-uid"),
            CreateLoginIdentifier(emailClient, ClientLoginIdentifierType.Email, "buyer@example.com"));
        await database.Context.SaveChangesAsync();
        var service = CreateService(database.Context);

        var act = () => service.CompleteLoginAsync(
            CreatePrincipal(
                firebaseUid: "root-uid",
                email: "buyer@example.com",
                emailVerified: true,
                signInProvider: "password"),
            CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ClientAuthException>();
        exception.Which.StatusCode.Should().Be(409);
        exception.Which.Code.Should().Be(ClientAuthProblemCodes.ClientIdentityConflict);
    }

    [Fact]
    public async Task RegisterAsync_rejects_phone_identifier_that_does_not_match_verified_token()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = CreateService(database.Context);

        var act = () => service.RegisterAsync(
            new RegisterRequest
            {
                Identifier = "+14155550100",
                IdentifierCountryCode = "US",
                FullName = "Phone Buyer"
            },
            CreatePrincipal(
                firebaseUid: "phone-root-uid",
                phoneNumber: "+526641234567",
                signInProvider: "phone"),
            CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ClientAuthException>();
        exception.Which.StatusCode.Should().Be(400);
        exception.Which.Code.Should().Be(ClientAuthProblemCodes.InvalidRegistration);
        database.Context.Clients.Should().BeEmpty();
        database.Context.ClientLoginIdentifiers.Should().BeEmpty();
    }

    [Fact]
    public async Task RegisterAsync_rejects_phone_identifier_when_token_was_not_phone_sign_in()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = CreateService(database.Context);

        var act = () => service.RegisterAsync(
            new RegisterRequest
            {
                Identifier = "+526641234567",
                FullName = "Phone Buyer"
            },
            CreatePrincipal(
                firebaseUid: "phone-root-uid",
                phoneNumber: "+526641234567",
                signInProvider: "password"),
            CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ClientAuthException>();
        exception.Which.StatusCode.Should().Be(400);
        exception.Which.Code.Should().Be(ClientAuthProblemCodes.InvalidRegistration);
        database.Context.Clients.Should().BeEmpty();
        database.Context.ClientLoginIdentifiers.Should().BeEmpty();
    }

    [Fact]
    public async Task RegisterAsync_returns_existing_client_when_phone_identity_is_already_linked()
    {
        await using var database = await TestDatabase.CreateAsync();
        var client = CreateClient(email: "", phoneNumber: "+526641234567");
        client.FirebaseUid = "phone-root-uid";
        database.Context.Clients.Add(client);
        database.Context.ClientLoginIdentifiers.AddRange(
            CreateLoginIdentifier(client, ClientLoginIdentifierType.FirebaseUid, "phone-root-uid"),
            CreateLoginIdentifier(client, ClientLoginIdentifierType.Phone, "+526641234567"));
        await database.Context.SaveChangesAsync();
        var service = CreateService(database.Context);

        var result = await service.RegisterAsync(
            new RegisterRequest
            {
                Identifier = "+526641234567",
                FullName = "Phone Buyer"
            },
            CreatePrincipal(
                firebaseUid: "phone-root-uid",
                phoneNumber: "+526641234567",
                signInProvider: "phone"),
            CancellationToken.None);

        result.OnboardingStatus.Should().Be("linked");
        result.Client.Id.Should().Be(client.Id);
        (await database.Context.Clients.CountAsync()).Should().Be(1);
        (await database.Context.ClientLoginIdentifiers.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task RegisterAsync_rejects_phone_identifier_linked_to_different_firebase_uid()
    {
        await using var database = await TestDatabase.CreateAsync();
        var client = CreateClient(email: "", phoneNumber: "+526641234567");
        client.FirebaseUid = "other-root-uid";
        database.Context.Clients.Add(client);
        database.Context.ClientLoginIdentifiers.AddRange(
            CreateLoginIdentifier(client, ClientLoginIdentifierType.FirebaseUid, "other-root-uid"),
            CreateLoginIdentifier(client, ClientLoginIdentifierType.Phone, "+526641234567"));
        await database.Context.SaveChangesAsync();
        var service = CreateService(database.Context);

        var act = () => service.RegisterAsync(
            new RegisterRequest
            {
                Identifier = "+526641234567",
                FullName = "Phone Buyer"
            },
            CreatePrincipal(
                firebaseUid: "phone-root-uid",
                phoneNumber: "+526641234567",
                signInProvider: "phone"),
            CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ClientAuthException>();
        exception.Which.StatusCode.Should().Be(409);
        exception.Which.Code.Should().Be(ClientAuthProblemCodes.ClientIdentityConflict);
        (await database.Context.ClientLoginIdentifiers.CountAsync(x =>
            x.Type == ClientLoginIdentifierType.FirebaseUid)).Should().Be(1);
    }

    [Fact]
    public async Task RegisterAsync_rejects_existing_client_with_conflicting_legacy_firebase_uid()
    {
        await using var database = await TestDatabase.CreateAsync();
        var client = CreateClient(email: "", phoneNumber: "+526641234567");
        client.FirebaseUid = "other-root-uid";
        database.Context.Clients.Add(client);
        database.Context.ClientLoginIdentifiers.Add(
            CreateLoginIdentifier(client, ClientLoginIdentifierType.Phone, "+526641234567"));
        await database.Context.SaveChangesAsync();
        var service = CreateService(database.Context);

        var act = () => service.RegisterAsync(
            new RegisterRequest
            {
                Identifier = "+526641234567",
                FullName = "Phone Buyer"
            },
            CreatePrincipal(
                firebaseUid: "phone-root-uid",
                phoneNumber: "+526641234567",
                signInProvider: "phone"),
            CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ClientAuthException>();
        exception.Which.StatusCode.Should().Be(409);
        exception.Which.Code.Should().Be(ClientAuthProblemCodes.ClientIdentityConflict);
        (await database.Context.ClientLoginIdentifiers.CountAsync(x =>
            x.Type == ClientLoginIdentifierType.FirebaseUid)).Should().Be(0);
    }

    [Fact]
    public async Task RegisterAsync_claims_imported_client_with_matching_phone_contact()
    {
        await using var database = await TestDatabase.CreateAsync();
        var importedClient = CreateClient(email: "boxoffice@example.com", phoneNumber: "+526641234567");
        importedClient.Orders.Add(CreateImportedOrder());
        database.Context.Clients.Add(importedClient);
        await database.Context.SaveChangesAsync();
        var service = CreateService(database.Context);

        var result = await service.RegisterAsync(
            new RegisterRequest
            {
                Identifier = "+526641234567",
                FullName = "Phone Buyer"
            },
            CreatePrincipal(
                firebaseUid: "phone-root-uid",
                phoneNumber: "+526641234567",
                signInProvider: "phone"),
            CancellationToken.None);

        result.Client.Id.Should().Be(importedClient.Id);
        result.Client.PhoneNumber.Should().Be("+526641234567");
        (await database.Context.Clients.CountAsync()).Should().Be(1);
        var claimedClient = await database.Context.Clients.SingleAsync();
        claimedClient.FirebaseUid.Should().Be("phone-root-uid");
        var identifiers = await database.Context.ClientLoginIdentifiers.ToListAsync();
        identifiers.Should().HaveCount(2);
        identifiers.Should().Contain(identifier =>
            identifier.ClientId == importedClient.Id
            && identifier.Type == ClientLoginIdentifierType.FirebaseUid
            && identifier.NormalizedValue == "phone-root-uid");
        identifiers.Should().Contain(identifier =>
            identifier.ClientId == importedClient.Id
            && identifier.Type == ClientLoginIdentifierType.Phone
            && identifier.NormalizedValue == "+526641234567");
    }

    [Fact]
    public async Task RegisterAsync_claims_imported_client_with_digits_only_phone_contact()
    {
        await using var database = await TestDatabase.CreateAsync();
        var importedClient = CreateClient(email: "boxoffice@example.com", phoneNumber: "526642322873");
        importedClient.Orders.Add(CreateImportedOrder());
        database.Context.Clients.Add(importedClient);
        await database.Context.SaveChangesAsync();
        var service = CreateService(database.Context);

        var result = await service.RegisterAsync(
            new RegisterRequest
            {
                Identifier = "6642322873",
                IdentifierCountryCode = "MX",
                FullName = "Phone Buyer"
            },
            CreatePrincipal(
                firebaseUid: "phone-root-uid",
                phoneNumber: "+526642322873",
                signInProvider: "phone"),
            CancellationToken.None);

        result.Client.Id.Should().Be(importedClient.Id);
        (await database.Context.Clients.CountAsync()).Should().Be(1);
        var identifiers = await database.Context.ClientLoginIdentifiers.ToListAsync();
        identifiers.Should().Contain(identifier =>
            identifier.ClientId == importedClient.Id
            && identifier.Type == ClientLoginIdentifierType.Phone
            && identifier.NormalizedValue == "+526642322873");
    }

    [Fact]
    public async Task RegisterAsync_fails_closed_when_phone_claim_matches_multiple_imported_clients()
    {
        await using var database = await TestDatabase.CreateAsync();
        var firstClient = CreateClient(email: "first@example.com", phoneNumber: "+526641234567");
        firstClient.Orders.Add(CreateImportedOrder("FIRST"));
        var secondClient = CreateClient(email: "second@example.com", phoneNumber: "+526641234567");
        secondClient.Orders.Add(CreateImportedOrder("SECOND"));
        database.Context.Clients.AddRange(firstClient, secondClient);
        await database.Context.SaveChangesAsync();
        var service = CreateService(database.Context);

        var act = () => service.RegisterAsync(
            new RegisterRequest
            {
                Identifier = "+526641234567",
                FullName = "Phone Buyer"
            },
            CreatePrincipal(
                firebaseUid: "phone-root-uid",
                phoneNumber: "+526641234567",
                signInProvider: "phone"),
            CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ClientAuthException>();
        exception.Which.StatusCode.Should().Be(409);
        exception.Which.Code.Should().Be(ClientAuthProblemCodes.ClientIdentityConflict);
        (await database.Context.ClientLoginIdentifiers.CountAsync()).Should().Be(0);
        (await database.Context.Clients.CountAsync(x => x.FirebaseUid != null)).Should().Be(0);
    }

    [Fact]
    public async Task RegisterAsync_claims_imported_client_with_only_bundle_pass_ownership()
    {
        await using var database = await TestDatabase.CreateAsync();
        var importedClient = CreateClient(email: "boxoffice@example.com", phoneNumber: "526642322873");
        var bundlePass = CreateBundlePass();
        bundlePass.Client = importedClient;
        database.Context.Clients.Add(importedClient);
        database.Context.Set<BundlePass>().Add(bundlePass);
        await database.Context.SaveChangesAsync();
        var service = CreateService(database.Context);

        var result = await service.RegisterAsync(
            new RegisterRequest
            {
                Identifier = "6642322873",
                IdentifierCountryCode = "MX",
                FullName = "Phone Buyer"
            },
            CreatePrincipal(
                firebaseUid: "phone-root-uid",
                phoneNumber: "+526642322873",
                signInProvider: "phone"),
            CancellationToken.None);

        result.Client.Id.Should().Be(importedClient.Id);
        (await database.Context.Clients.CountAsync()).Should().Be(1);
        var identifiers = await database.Context.ClientLoginIdentifiers.ToListAsync();
        identifiers.Should().Contain(identifier =>
            identifier.ClientId == importedClient.Id
            && identifier.Type == ClientLoginIdentifierType.Phone
            && identifier.NormalizedValue == "+526642322873");
    }

    [Fact]
    public async Task RegisterAsync_does_not_claim_contact_only_client_without_owned_items()
    {
        await using var database = await TestDatabase.CreateAsync();
        var contactOnlyClient = CreateClient(email: "boxoffice@example.com", phoneNumber: "+526641234567");
        database.Context.Clients.Add(contactOnlyClient);
        await database.Context.SaveChangesAsync();
        var service = CreateService(database.Context);

        var result = await service.RegisterAsync(
            new RegisterRequest
            {
                Identifier = "+526641234567",
                FullName = "Phone Buyer"
            },
            CreatePrincipal(
                firebaseUid: "phone-root-uid",
                phoneNumber: "+526641234567",
                signInProvider: "phone"),
            CancellationToken.None);

        result.Client.Id.Should().NotBe(contactOnlyClient.Id);
        (await database.Context.Clients.CountAsync()).Should().Be(2);
        var identifiers = await database.Context.ClientLoginIdentifiers.ToListAsync();
        identifiers.Should().OnlyContain(identifier => identifier.ClientId == result.Client.Id);
    }

    [Fact]
    public async Task RegisterAsync_rejects_email_identifier_while_phone_only_registration_is_enabled()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = CreateService(database.Context);

        var act = () => service.RegisterAsync(new RegisterRequest
        {
            Identifier = " Buyer@Example.COM ",
            Password = "ValidPassword123!",
            FullName = "Email Buyer"
        }, CreateAnonymousPrincipal(), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ClientAuthException>();
        exception.Which.StatusCode.Should().Be(400);
        exception.Which.Code.Should().Be(ClientAuthProblemCodes.InvalidRegistration);
        database.Context.Clients.Should().BeEmpty();
        database.Context.ClientLoginIdentifiers.Should().BeEmpty();
    }

    private static ClientIdentityService CreateService(
        XBOLDbContext context,
        IFirebaseClientAuthClient? firebaseAuth = null)
    {
        return new ClientIdentityService(
            new ClientRepository(context),
            new ClientLoginIdentifierRepository(context),
            new PhoneRegionCodeRepository(context),
            firebaseAuth ?? Substitute.For<IFirebaseClientAuthClient>());
    }

    private static ClaimsPrincipal CreatePrincipal(
        string firebaseUid,
        string? email = null,
        bool emailVerified = false,
        string? phoneNumber = null,
        string? signInProvider = null)
    {
        var claims = new List<Claim>
        {
            new(GcipAuthenticationHandler.FirebaseUidClaimType, firebaseUid),
            new(GcipAuthenticationHandler.EmailVerifiedClaimType, emailVerified.ToString().ToLowerInvariant())
        };

        if (!string.IsNullOrWhiteSpace(email))
        {
            claims.Add(new Claim(ClaimTypes.Email, email));
        }

        if (!string.IsNullOrWhiteSpace(phoneNumber))
        {
            claims.Add(new Claim(ClaimTypes.MobilePhone, phoneNumber));
        }

        if (!string.IsNullOrWhiteSpace(signInProvider))
        {
            claims.Add(new Claim(GcipAuthenticationHandler.SignInProviderClaimType, signInProvider));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, GcipAuthenticationHandler.SchemeName));
    }

    private static ClaimsPrincipal CreateAnonymousPrincipal()
    {
        return new ClaimsPrincipal(new ClaimsIdentity());
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

    private static Client CreateClient(string email, string phoneNumber)
    {
        return new Client
        {
            ClientType = ClientType.Individual,
            Email = email,
            PhoneNumber = phoneNumber,
            FullName = "Existing Client",
            PhoneRegionCodeId = MxPhoneRegionId,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = Guid.Empty,
            UpdatedAt = DateTimeOffset.UtcNow,
            UpdatedBy = Guid.Empty
        };
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

    private static Order CreateImportedOrder(string reference = "IMPORTED")
    {
        return new Order
        {
            Reference = reference,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty
        };
    }

    private static BundlePass CreateBundlePass()
    {
        var now = DateTimeOffset.UtcNow;
        return new BundlePass
        {
            Bundle = CreateBundle(),
            TrackingCode = Guid.NewGuid().ToString("N"),
            PrivateToken = Guid.NewGuid().ToString("N"),
            BundlePassType = BundlePassType.Full,
            Status = BundlePassStatus.Active,
            IsDigital = true,
            Price = 100,
            PurchasedAt = now,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty
        };
    }

    private static Bundle CreateBundle()
    {
        var now = DateTimeOffset.UtcNow;
        return new Bundle
        {
            VenueMap = CreateVenueMap(),
            Name = "Imported Bundle",
            Status = EventStatus.Published,
            BundleType = BundleType.SeasonPass,
            BundlePricingType = BundlePricingType.Composite,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty
        };
    }

    private static VenueMap CreateVenueMap()
    {
        var now = DateTimeOffset.UtcNow;
        return new VenueMap
        {
            Venue = CreateVenue(),
            Name = "Imported Venue Map",
            ExternalMapKey = Guid.NewGuid().ToString("N"),
            Capacity = 1,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty
        };
    }

    private static Venue CreateVenue()
    {
        var now = DateTimeOffset.UtcNow;
        return new Venue
        {
            Name = "Imported Venue",
            Category = VenueCategory.Stadium,
            Status = VenueStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = Guid.Empty,
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
                CreatePhoneRegion(UsPhoneRegionId, "US", "1"),
                CreatePhoneRegion(MxPhoneRegionId, "MX", "52"),
                CreatePhoneRegion(CaPhoneRegionId, "CA", "1"));
            await context.SaveChangesAsync();
            return new TestDatabase(connection, context);
        }

        public Task<PhoneRegionCode> GetPhoneRegionAsync(string regionCode)
        {
            return Context.Set<PhoneRegionCode>().SingleAsync(x => x.RegionCode == regionCode);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
