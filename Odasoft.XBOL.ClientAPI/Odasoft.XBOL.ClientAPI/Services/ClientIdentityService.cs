using FirebaseAdmin;
using FirebaseAdmin.Auth;
using FuzzySharp.SimilarityRatio.Scorer.StrategySensitive;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Odasoft.XBOL.ClientAPI.Auth;
using Odasoft.XBOL.Commons.Enums;
using Odasoft.XBOL.Data.Repositories;
using Odasoft.XBOL.DTO;
using Odasoft.XBOL.DTO.Requests;
using Odasoft.XBOL.DTO.Responses;
using Odasoft.XBOL.Models;
using PhoneNumbers;
using System.Net.Mail;
using System.Security.Claims;

namespace Odasoft.XBOL.ClientAPI.Services;

public sealed class ClientIdentityService(
    ClientRepository clientRepository,
    ClientLoginIdentifierRepository clientLoginIdentifierRepository,
    OrderRepository orderRepository,
    UserRepository userRepository,
    PhoneRegionCodeRepository phoneRegionCodeRepository,
    IFirebaseClientAuthClient firebaseAuth) : IClientIdentityService
{
    private static readonly HashSet<string> SupportedPhoneRegions = new(StringComparer.OrdinalIgnoreCase)
    {
        "US",
        "MX",
        "CA"
    };

    private static readonly PhoneNumberUtil PhoneNumberParser = PhoneNumberUtil.GetInstance();

    public async Task<ClientDTO?> TryResolveCurrentClientAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var identity = GetFirebaseIdentity(principal);
        var client = await ResolveClientAsync(identity);

        return client is null ? null : ToDto(client);
    }

    public async Task<ClientDTO> RequireCurrentClientAsync(ClaimsPrincipal principal)
    {
        var identity = GetFirebaseIdentity(principal);
        var client = await ResolveClientAsync(identity);
        if (client is not null)
        {
            if (!HasVerifiedAccountAccessIdentifier(identity))
            {
                throw new ClientAuthException(
                    "Client profile requires a verified Firebase email or phone before account data can be accessed.",
                    StatusCodes.Status403Forbidden,
                    ClientAuthProblemCodes.VerificationRequired);
            }

            return ToDto(client);
        }

        throw new ClientAuthException(
            "Authenticated Firebase identity is not linked to a client profile.",
            StatusCodes.Status404NotFound,
            ClientAuthProblemCodes.UnlinkedClientProfile);
    }

    public async Task<AuthMeResponse> GetMeAsync(ClaimsPrincipal principal)
    {
        var identity = GetFirebaseIdentity(principal);
        var client = await ResolveClientAsync(identity);

        return ToAuthMeResponse(identity, client);
    }

    public async Task<RegisterResponse> RegisterAsync(
        RegisterRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var identifier = NormalizeRequired(request.Identifier, nameof(RegisterRequest.Identifier));
        var fullName = NormalizeRequired(request.FullName, nameof(RegisterRequest.FullName));

        //if (TryNormalizeEmailIdentifier(identifier, out var email))
        //{
        //    var password = ValidateRequiredPassword(request.Password, nameof(RegisterRequest.Password));
        //    return await RegisterEmailAsync(email, password, fullName, cancellationToken);
        //}

        var requestPhone = NormalizePhoneIdentifier(
            identifier,
            request.IdentifierCountryCode,
            nameof(RegisterRequest.Identifier));
        RegisterResponse registerResponse = await RegisterPhoneAsync(principal, requestPhone, fullName, cancellationToken);

        return registerResponse;
    }

    private async Task<RegisterResponse> RegisterEmailAsync(
        string email,
        string password,
        string fullName,
        CancellationToken cancellationToken)
    {
        var firebaseUser = await CreateFirebaseUserAsync(email, password, fullName, cancellationToken);

        try
        {
            var identity = new AuthenticatedClientIdentity(
                firebaseUser.Uid,
                firebaseUser.Email,
                firebaseUser.EmailVerified,
                firebaseUser.DisplayName,
                NormalizeOptionalClaim(firebaseUser.PhoneNumber),
                "password");

            var customToken = await firebaseAuth.CreateCustomTokenAsync(
                firebaseUser.Uid,
                cancellationToken);
            var now = DateTimeOffset.UtcNow;

            var client = new Client
            {
                FirebaseUid = firebaseUser.Uid,
                Email = firebaseUser.Email ?? email,
                PhoneNumber = string.Empty,
                FullName = firebaseUser.DisplayName ?? fullName,
                ClientType = ClientType.Individual,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = Guid.Empty,
                UpdatedAt = now,
                UpdatedBy = Guid.Empty
            };
            client.LoginIdentifiers.Add(CreateLoginIdentifier(
                client,
                ClientLoginIdentifierType.FirebaseUid,
                NormalizeFirebaseUid(firebaseUser.Uid),
                now));

            await clientRepository.InsertAsync(client);
            await clientRepository.CommitAsync();

            return new RegisterResponse
            {
                FirebaseUid = firebaseUser.Uid,
                CustomToken = customToken,
                Client = ToDto(client),
                OnboardingStatus = "linked",
                VerificationStatus = GetVerificationStatus(identity)
            };
        }
        catch
        {
            using var cleanupCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await firebaseAuth.DeleteUserAsync(firebaseUser.Uid, cleanupCancellation.Token);
            throw;
        }
    }

    private async Task<RegisterResponse> RegisterPhoneAsync(
        ClaimsPrincipal principal,
        string requestPhone,
        string fullName,
        CancellationToken cancellationToken)
    {
        var identity = GetFirebaseIdentity(principal);
        if (!string.Equals(identity.SignInProvider, "phone", StringComparison.Ordinal))
        {
            throw new ClientAuthException(
                "Phone registration requires a Firebase phone sign-in token.",
                StatusCodes.Status400BadRequest,
                ClientAuthProblemCodes.InvalidRegistration);
        }

        var phone = NormalizeFirebasePhoneClaim(identity.PhoneNumber);
        if (!string.Equals(requestPhone, phone, StringComparison.Ordinal))
        {
            throw new ClientAuthException(
                "Registration phone identifier must match the verified Firebase phone number.",
                StatusCodes.Status400BadRequest,
                ClientAuthProblemCodes.InvalidRegistration);
        }

        var phoneRegion = await ResolvePhoneRegionAsync(identity.PhoneNumber);
        var existingClient = await ResolveClientAsync(identity);
        if (existingClient is not null)
        {
            await EnsureClientDoesNotHaveDifferentFirebaseUidAsync(existingClient, identity);
            await clientLoginIdentifierRepository.AddMissingAsync(
                existingClient,
                BuildVerifiedIdentifierLookups(identity),
                DateTimeOffset.UtcNow);
            try
            {
                await clientLoginIdentifierRepository.CommitAsync();
            }
            catch (DbUpdateException ex) when (IsClientLoginIdentifierUniqueConflict(ex))
            {
                existingClient = await ResolveClientAfterIdentifierRaceAsync(identity);
            }

            return new RegisterResponse
            {
                FirebaseUid = identity.FirebaseUid,
                Client = ToDto(existingClient),
                OnboardingStatus = "linked",
                VerificationStatus = GetVerificationStatus(identity)
            };
        }

        var now = DateTimeOffset.UtcNow;
        var client = new Client
        {
            FirebaseUid = identity.FirebaseUid,
            Email = string.Empty,
            PhoneRegionCodeId = phoneRegion.Id,
            PhoneNumber = phone,
            FullName = fullName,
            ClientType = ClientType.Individual,
            IsActive = true,
            CreatedAt = now,
            CreatedBy = Guid.Empty,
            UpdatedAt = now,
            UpdatedBy = Guid.Empty
        };

        foreach (var lookup in BuildVerifiedIdentifierLookups(identity))
        {
            client.LoginIdentifiers.Add(CreateLoginIdentifier(
                client,
                lookup.Type,
                lookup.NormalizedValue,
                now));
        }

        try
        {
            await clientRepository.InsertAsync(client);
            await clientRepository.CommitAsync();
        }
        catch (DbUpdateException ex) when (IsClientLoginIdentifierUniqueConflict(ex))
        {
            var racedClient = await ResolveClientAfterIdentifierRaceAsync(identity);
            return new RegisterResponse
            {
                FirebaseUid = identity.FirebaseUid,
                Client = ToDto(racedClient),
                OnboardingStatus = "linked",
                VerificationStatus = GetVerificationStatus(identity)
            };
        }

        return new RegisterResponse
        {
            FirebaseUid = identity.FirebaseUid,
            Client = ToDto(client),
            OnboardingStatus = "linked",
            VerificationStatus = GetVerificationStatus(identity)
        };
    }

    private async Task<PhoneRegionCode> ResolvePhoneRegionAsync(string phone)
    {
        string regionCode;
        try
        {
            var parsed = PhoneNumberParser.Parse(phone, null);
            regionCode = PhoneNumberParser.GetRegionCodeForNumber(parsed);
        }
        catch (NumberParseException)
        {
            throw CreateInvalidRegistrationException("phoneNumber must be a valid phone number.");
        }

        if (string.IsNullOrWhiteSpace(regionCode))
        {
            throw CreateInvalidRegistrationException("phoneNumber must be a valid phone number.");
        }

        var phoneRegion = await phoneRegionCodeRepository.Get(x => x.RegionCode == regionCode)
            .SingleOrDefaultAsync();

        if (phoneRegion is null)
        {
            throw new ClientAuthException(
                "Phone registration requires a configured phone region.",
                StatusCodes.Status400BadRequest,
                ClientAuthProblemCodes.InvalidRegistration);
        }

        return phoneRegion;
    }

    public async Task<AuthMeResponse> CompleteLoginAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var identity = GetFirebaseIdentity(principal);
        var client = await ResolveClientAsync(identity);
        if (client is not null)
        {
            await EnsureClientDoesNotHaveDifferentFirebaseUidAsync(client, identity);
            await clientLoginIdentifierRepository.AddMissingAsync(
                client,
                BuildVerifiedIdentifierLookups(identity),
                DateTimeOffset.UtcNow);
            try
            {
                await clientLoginIdentifierRepository.CommitAsync();
            }
            catch (DbUpdateException ex) when (IsClientLoginIdentifierUniqueConflict(ex))
            {
                client = await ResolveClientAfterIdentifierRaceAsync(identity);
            }
        }

        return ToAuthMeResponse(identity, client);
    }

    private async Task<Client> ResolveClientAfterIdentifierRaceAsync(AuthenticatedClientIdentity identity)
    {
        clientLoginIdentifierRepository.DetachPendingClientIdentityChanges();

        var client = await ResolveClientAsync(identity);
        if (client is null)
        {
            throw new ClientAuthException(
                "Verified identifier was claimed concurrently.",
                StatusCodes.Status409Conflict,
                ClientAuthProblemCodes.ClientIdentityConflict);
        }

        await EnsureClientDoesNotHaveDifferentFirebaseUidAsync(client, identity);
        return client;
    }

    private async Task<FirebaseClientUser> CreateFirebaseUserAsync(
        string email,
        string password,
        string fullName,
        CancellationToken cancellationToken)
    {
        try
        {
            return await firebaseAuth.CreateUserAsync(
                new CreateFirebaseClientUserRequest(
                    email,
                    password,
                    fullName,
                    EmailVerified: false,
                    Disabled: false),
                cancellationToken);
        }
        catch (FirebaseAuthException ex)
        {
            if (TryMapFirebaseRegistrationException(ex, out var authException))
            {
                throw authException;
            }

            throw;
        }
    }

    private async Task<Client?> ResolveClientAsync(AuthenticatedClientIdentity identity)
    {
        var matches = await clientLoginIdentifierRepository.GetVerifiedMatchesAsync(
            BuildVerifiedIdentifierLookups(identity));

        var clientIds = matches
            .Select(x => x.ClientId)
            .Distinct()
            .ToList();

        if (clientIds.Count > 1)
        {
            throw new ClientAuthException(
                "Authenticated Firebase identity matches more than one client profile.",
                StatusCodes.Status409Conflict,
                ClientAuthProblemCodes.ClientIdentityConflict);
        }

        return clientIds.Count == 0
            ? null
            : matches.First(x => x.ClientId == clientIds[0]).Client;
    }

    private async Task EnsureClientDoesNotHaveDifferentFirebaseUidAsync(
        Client client,
        AuthenticatedClientIdentity identity)
    {
        var firebaseUid = NormalizeFirebaseUid(identity.FirebaseUid);
        if (!string.IsNullOrWhiteSpace(client.FirebaseUid)
            && !string.Equals(
                NormalizeFirebaseUid(client.FirebaseUid),
                firebaseUid,
                StringComparison.Ordinal))
        {
            throw new ClientAuthException(
                "Verified identifier is already linked to a different Firebase identity.",
                StatusCodes.Status409Conflict,
                ClientAuthProblemCodes.ClientIdentityConflict);
        }

        var clientLookups = await clientLoginIdentifierRepository.GetClientLookupsAsync(client.Id);
        if (clientLookups.Any(lookup =>
            lookup.Type == ClientLoginIdentifierType.FirebaseUid
            && lookup.NormalizedValue != firebaseUid))
        {
            throw new ClientAuthException(
                "Verified identifier is already linked to a different Firebase identity.",
                StatusCodes.Status409Conflict,
                ClientAuthProblemCodes.ClientIdentityConflict);
        }
    }

    private static IReadOnlyList<ClientLoginIdentifierLookup> BuildVerifiedIdentifierLookups(
        AuthenticatedClientIdentity identity)
    {
        var lookups = new List<ClientLoginIdentifierLookup>
        {
            new(
                ClientLoginIdentifierType.FirebaseUid,
                NormalizeFirebaseUid(identity.FirebaseUid))
        };

        if (identity.EmailVerified && identity.Email is not null)
        {
            lookups.Add(new ClientLoginIdentifierLookup(
                ClientLoginIdentifierType.Email,
                NormalizeEmailIdentifier(identity.Email)));
        }

        if (identity.PhoneNumber is not null)
        {
            lookups.Add(new ClientLoginIdentifierLookup(
                ClientLoginIdentifierType.Phone,
                NormalizeFirebasePhoneClaim(identity.PhoneNumber)));
        }

        return lookups;
    }

    private static ClientLoginIdentifier CreateLoginIdentifier(
        Client client,
        ClientLoginIdentifierType type,
        string normalizedValue,
        DateTimeOffset verifiedAt)
    {
        return new ClientLoginIdentifier
        {
            Client = client,
            Type = type,
            NormalizedValue = normalizedValue,
            VerifiedAt = verifiedAt,
            CreatedAt = verifiedAt,
            CreatedBy = Guid.Empty,
            UpdatedAt = verifiedAt,
            UpdatedBy = Guid.Empty
        };
    }

    private static AuthMeResponse ToAuthMeResponse(
        AuthenticatedClientIdentity identity,
        Client? client)
    {
        return new AuthMeResponse
        {
            FirebaseUid = identity.FirebaseUid,
            Email = identity.Email,
            EmailVerified = identity.EmailVerified,
            PhoneNumber = identity.PhoneNumber,
            SignInProvider = identity.SignInProvider,
            Client = client is null ? null : ToDto(client),
            OnboardingStatus = client is null ? "unlinked" : "linked",
            VerificationStatus = GetVerificationStatus(identity)
        };
    }

    private static AuthenticatedClientIdentity GetFirebaseIdentity(ClaimsPrincipal principal)
    {
        var firebaseUid = principal.FindFirst(GcipAuthenticationHandler.FirebaseUidClaimType)?.Value;
        if (string.IsNullOrWhiteSpace(firebaseUid))
        {
            throw new ClientAuthException(
                "Authenticated Firebase token did not include a UID.",
                StatusCodes.Status401Unauthorized,
                ClientAuthProblemCodes.InvalidRegistration);
        }

        return new AuthenticatedClientIdentity(
            firebaseUid,
            NormalizeOptionalClaim(principal.FindFirst(ClaimTypes.Email)?.Value),
            ParseBooleanClaim(principal, GcipAuthenticationHandler.EmailVerifiedClaimType),
            NormalizeOptionalClaim(principal.FindFirst(ClaimTypes.Name)?.Value),
            NormalizeOptionalClaim(principal.FindFirst(ClaimTypes.MobilePhone)?.Value),
            NormalizeOptionalClaim(principal.FindFirst(GcipAuthenticationHandler.SignInProviderClaimType)?.Value));
    }

    private static bool ParseBooleanClaim(ClaimsPrincipal principal, string claimType)
    {
        return bool.TryParse(principal.FindFirst(claimType)?.Value, out var value) && value;
    }

    private static string? NormalizeOptionalClaim(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string NormalizeFirebaseUid(string value)
    {
        return NormalizeRequired(value, "firebaseUid");
    }

    private static string NormalizeEmailIdentifier(string value)
    {
        return NormalizeRequired(value, "email").ToLowerInvariant();
    }

    private static bool TryNormalizeEmailIdentifier(string value, out string email)
    {
        email = string.Empty;
        if (!value.Contains('@', StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            var address = new MailAddress(value);
            if (!string.Equals(address.Address, value, StringComparison.OrdinalIgnoreCase))
            {
                throw CreateInvalidRegistrationException("identifier must be a valid email address.");
            }

            email = NormalizeEmailIdentifier(address.Address);
            return true;
        }
        catch (FormatException)
        {
            throw CreateInvalidRegistrationException("identifier must be a valid email address.");
        }
    }

    private static string NormalizeFirebasePhoneClaim(string? value)
    {
        return NormalizePhoneIdentifier(value, null, "phoneNumber");
    }

    private static string NormalizePhoneIdentifier(
        string? value,
        string? identifierCountryCode,
        string fieldName)
    {
        var raw = NormalizeRequired(value, fieldName);
        var region = raw.StartsWith("+", StringComparison.Ordinal)
            ? null
            : NormalizePhoneRegion(identifierCountryCode);

        try
        {
            var parsed = PhoneNumberParser.Parse(raw, region);
            if (!PhoneNumberParser.IsValidNumber(parsed))
            {
                throw CreateInvalidRegistrationException($"{fieldName} must be a valid phone number.");
            }

            var parsedRegion = PhoneNumberParser.GetRegionCodeForNumber(parsed);
            if (string.IsNullOrWhiteSpace(parsedRegion)
                || !SupportedPhoneRegions.Contains(parsedRegion))
            {
                throw CreateInvalidRegistrationException(
                    $"{fieldName} must be a US, Mexico, or Canada phone number.");
            }

            return PhoneNumberParser.Format(parsed, PhoneNumberFormat.NATIONAL).Replace(" ", "");
        }
        catch (NumberParseException)
        {
            throw CreateInvalidRegistrationException($"{fieldName} must be a valid phone number.");
        }
    }

    private static string NormalizePhoneRegion(string? value)
    {
        var region = NormalizeRequired(value, nameof(RegisterRequest.IdentifierCountryCode)).ToUpperInvariant();
        if (!SupportedPhoneRegions.Contains(region))
        {
            throw CreateInvalidRegistrationException(
                $"{nameof(RegisterRequest.IdentifierCountryCode)} must be US, MX, or CA.");
        }

        return region;
    }

    private static ClientAuthException CreateInvalidRegistrationException(string message)
    {
        return new ClientAuthException(
            message,
            StatusCodes.Status400BadRequest,
            ClientAuthProblemCodes.InvalidRegistration);
    }

    private static string NormalizeRequired(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ClientAuthException(
                $"{fieldName} is required.",
                StatusCodes.Status400BadRequest,
                ClientAuthProblemCodes.InvalidRegistration);
        }

        return value.Trim();
    }

    private static string ValidateRequiredPassword(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ClientAuthException(
                $"{fieldName} is required.",
                StatusCodes.Status400BadRequest,
                ClientAuthProblemCodes.InvalidRegistration);
        }

        return value;
    }

    private static ClientDTO ToDto(Client client)
    {
        return new ClientDTO
        {
            Id = client.Id,
            FirebaseUid = client.FirebaseUid ?? string.Empty,
            FullName = client.FullName ?? string.Empty,
            BusinessName = client.BusinessName,
            Email = client.Email ?? string.Empty,
            PhoneNumber = client.PhoneNumber,
            PhoneRegionCodeId = client.PhoneRegionCodeId,
            PhoneCode = NormalizePhoneDialCode(client.PhoneRegionCode?.DialCode)
                ?? InferPhoneCode(client.PhoneNumber)
        };
    }

    private static string? NormalizePhoneDialCode(string? dialCode)
    {
        if (string.IsNullOrWhiteSpace(dialCode))
        {
            return null;
        }

        var trimmed = dialCode.Trim();
        return trimmed.StartsWith("+", StringComparison.Ordinal)
            ? trimmed
            : $"+{trimmed}";
    }

    private static string InferPhoneCode(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return string.Empty;
        }

        try
        {
            var parsed = PhoneNumberParser.Parse(phoneNumber, null);
            return $"+{parsed.CountryCode}";
        }
        catch (NumberParseException)
        {
            return string.Empty;
        }
    }

    private static bool HasVerifiedAccountAccessIdentifier(AuthenticatedClientIdentity identity)
    {
        return identity.EmailVerified || identity.PhoneNumber is not null;
    }

    private static string GetVerificationStatus(AuthenticatedClientIdentity identity)
    {
        return HasVerifiedAccountAccessIdentifier(identity) ? "verified" : "pending";
    }

    private static bool TryMapFirebaseRegistrationException(
        FirebaseAuthException exception,
        out ClientAuthException authException)
    {
        switch (exception.AuthErrorCode)
        {
            case AuthErrorCode.EmailAlreadyExists:
                authException = new ClientAuthException(
                    "Email is already in use.",
                    StatusCodes.Status409Conflict,
                    ClientAuthProblemCodes.FirebaseEmailExists);
                return true;
        }

        if (exception.ErrorCode == ErrorCode.InvalidArgument)
        {
            authException = new ClientAuthException(
                "Firebase rejected one or more registration fields.",
                StatusCodes.Status400BadRequest,
                ClientAuthProblemCodes.InvalidRegistration);
            return true;
        }

        authException = null!;
        return false;
    }

    private static bool IsClientLoginIdentifierUniqueConflict(DbUpdateException exception)
    {
        if (exception.InnerException is PostgresException postgresException)
        {
            return postgresException.SqlState == PostgresErrorCodes.UniqueViolation
                && string.Equals(postgresException.TableName, "ClientLoginIdentifier", StringComparison.OrdinalIgnoreCase)
                && string.Equals(postgresException.ConstraintName, "IX_ClientLoginIdentifier_Type_NormalizedValue", StringComparison.OrdinalIgnoreCase);
        }

        var text = exception.ToString();
        return (text.Contains("ClientLoginIdentifier", StringComparison.OrdinalIgnoreCase)
                || text.Contains("ClientLoginIdentifiers", StringComparison.OrdinalIgnoreCase))
            && (text.Contains("IX_ClientLoginIdentifier_Type_NormalizedValue", StringComparison.OrdinalIgnoreCase)
                || text.Contains("IX_ClientLoginIdentifiers_Type_NormalizedValue", StringComparison.OrdinalIgnoreCase)
                || text.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase)
                || text.Contains("23505", StringComparison.OrdinalIgnoreCase));
    }
}