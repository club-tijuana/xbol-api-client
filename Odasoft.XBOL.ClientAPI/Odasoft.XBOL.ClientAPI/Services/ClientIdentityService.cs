using System.Security.Claims;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Odasoft.XBOL.ClientAPI.Auth;
using Odasoft.XBOL.Commons.Enums;
using Odasoft.XBOL.Data.Repositories;
using Odasoft.XBOL.DTO;
using Odasoft.XBOL.DTO.Requests;
using Odasoft.XBOL.DTO.Responses;
using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.ClientAPI.Services;

public sealed class ClientIdentityService(
    ClientRepository clientRepository,
    IFirebaseTenantAuthClient tenantAuth,
    IFirebasePasswordAuthClient passwordAuthClient) : IClientIdentityService
{
    public async Task<ClientDTO?> TryResolveCurrentClientAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var identity = GetFirebaseIdentity(principal);
        var client = await clientRepository.GetByFirebaseUidAsync(identity.FirebaseUid);

        return client is null ? null : ToDto(client);
    }

    public async Task<ClientDTO> RequireCurrentClientAsync(ClaimsPrincipal principal)
    {
        var identity = GetFirebaseIdentity(principal);
        var client = await clientRepository.GetByFirebaseUidAsync(identity.FirebaseUid);
        if (client is not null)
        {
            if (!identity.EmailVerified)
            {
                throw new ClientAuthException(
                    "Client profile requires a verified Firebase email before account data can be accessed.",
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
        var client = await clientRepository.GetByFirebaseUidAsync(identity.FirebaseUid);

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

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizeRequired(request.Email, nameof(RegisterRequest.Email));
        var password = NormalizeRequired(request.Password, nameof(RegisterRequest.Password));
        var fullName = NormalizeRequired(request.FullName, nameof(RegisterRequest.FullName));
        var suppliedPhone = NormalizeOptionalClaim(request.PhoneNumber);

        if (!string.IsNullOrWhiteSpace(request.ClaimToken))
        {
            throw ClaimTokenLinkingUnsupported();
        }

        var firebaseRegistration = await passwordAuthClient.SignUpAsync(email, password, cancellationToken);
        var firebasePhoneNumber = ToFirebasePhoneNumber(suppliedPhone);

        try
        {
            try
            {
                await tenantAuth.UpdateUserAsync(new FirebaseClientUserUpdate(
                    firebaseRegistration.FirebaseUid,
                    fullName,
                    firebasePhoneNumber,
                    Disabled: false), cancellationToken);
            }
            catch (FirebaseAuthException ex) when (firebasePhoneNumber is not null)
            {
                throw MapFirebasePhoneUpdateException(ex);
            }

            var identity = new AuthenticatedClientIdentity(
                firebaseRegistration.FirebaseUid,
                firebaseRegistration.Email,
                false,
                fullName,
                suppliedPhone,
                "password");

            var customToken = await tenantAuth.CreateCustomTokenAsync(
                firebaseRegistration.FirebaseUid,
                cancellationToken);

            var client = new Client
            {
                FirebaseUid = firebaseRegistration.FirebaseUid,
                Email = email,
                PhoneNumber = suppliedPhone ?? string.Empty,
                FullName = fullName,
                ClientType = ClientType.Individual,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = Guid.Empty,
                UpdatedAt = DateTimeOffset.UtcNow,
                UpdatedBy = Guid.Empty
            };

            await clientRepository.InsertAsync(client);
            await clientRepository.CommitAsync();

            return new RegisterResponse
            {
                FirebaseUid = firebaseRegistration.FirebaseUid,
                CustomToken = customToken,
                Client = ToDto(client),
                OnboardingStatus = "linked",
                VerificationStatus = GetVerificationStatus(identity)
            };
        }
        catch
        {
            using var cleanupCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await tenantAuth.DeleteUserAsync(firebaseRegistration.FirebaseUid, cleanupCancellation.Token);
            throw;
        }
    }

    public Task<RegisterResponse> ClaimCurrentClientAsync(ClaimsPrincipal principal, ClaimClientRequest request)
    {
        _ = request;
        _ = GetFirebaseIdentity(principal);
        throw ClaimTokenLinkingUnsupported();
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

    private static string? ToFirebasePhoneNumber(string? phoneNumber)
    {
        return phoneNumber?.StartsWith('+') == true ? phoneNumber : null;
    }

    private static ClientAuthException MapFirebasePhoneUpdateException(FirebaseAuthException exception)
    {
        var isConflict = exception.AuthErrorCode == AuthErrorCode.PhoneNumberAlreadyExists
            || exception.ErrorCode is ErrorCode.Conflict or ErrorCode.AlreadyExists;

        return isConflict
            ? new ClientAuthException(
                "The supplied phone number is already linked to another Firebase account.",
                StatusCodes.Status409Conflict,
                ClientAuthProblemCodes.ClientIdentityConflict)
            : new ClientAuthException(
                "The supplied phone number is not valid for Firebase registration.",
                StatusCodes.Status400BadRequest,
                ClientAuthProblemCodes.InvalidRegistration);
    }

    private static ClientDTO ToDto(Client client)
    {
        return new ClientDTO
        {
            Id = client.Id,
            UserId = client.FirebaseUid ?? string.Empty,
            FullName = client.FullName ?? string.Empty,
            BusinessName = client.BusinessName,
            Email = client.Email,
            PhoneNumber = client.PhoneNumber,
            PhoneCode = client.PhoneRegionCode?.DialCode ?? InferPhoneCode(client.PhoneNumber)
        };
    }

    private static string InferPhoneCode(string? phoneNumber)
    {
        return phoneNumber?.StartsWith("+52", StringComparison.Ordinal) == true ? "+52" : string.Empty;
    }

    private static string GetVerificationStatus(AuthenticatedClientIdentity identity)
    {
        return identity.EmailVerified ? "verified" : "pending";
    }

    private static ClientAuthException ClaimTokenLinkingUnsupported()
    {
        return new ClientAuthException(
            "Claim-token client linking is not supported in this auth-only registration flow.",
            StatusCodes.Status400BadRequest,
            ClientAuthProblemCodes.ClaimTokenInvalid);
    }
}
