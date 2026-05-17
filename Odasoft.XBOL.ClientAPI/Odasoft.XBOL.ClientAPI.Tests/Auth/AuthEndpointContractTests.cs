using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Odasoft.XBOL.ClientAPI.Auth;
using Odasoft.XBOL.ClientAPI.Services;
using Odasoft.XBOL.DTO;
using Odasoft.XBOL.DTO.Requests;
using Odasoft.XBOL.DTO.Responses;

namespace Odasoft.XBOL.ClientAPI.Tests.Auth;

public sealed class AuthEndpointContractTests
{
    [Fact]
    public async Task Register_uses_api_route_and_returns_client_web_contract_shape()
    {
        await using var factory = new AuthEndpointFactory(new ContractIdentityService());
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "client@example.com",
            password = "Password123!",
            fullName = "Client Name",
            phoneNumber = "+526641234567"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Equal("firebase-route", body.RootElement.GetProperty("firebaseUid").GetString());
        Assert.Equal("custom-route-token", body.RootElement.GetProperty("customToken").GetString());
        Assert.False(body.RootElement.TryGetProperty("idToken", out _));
        Assert.Equal("linked", body.RootElement.GetProperty("onboardingStatus").GetString());
        Assert.Equal("pending", body.RootElement.GetProperty("verificationStatus").GetString());
        Assert.Equal("firebase-route", body.RootElement.GetProperty("client").GetProperty("userId").GetString());
    }

    [Fact]
    public async Task Client_auth_exception_returns_problem_json_with_code()
    {
        await using var factory = new AuthEndpointFactory(
            new ThrowingIdentityService(new ClientAuthException(
                "Authenticated Firebase identity is not linked to a client profile.",
                StatusCodes.Status404NotFound,
                ClientAuthProblemCodes.UnlinkedClientProfile)));
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "client@example.com",
            password = "Password123!",
            fullName = "Client Name",
            phoneNumber = "+526641234567"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Equal("unlinked_client_profile", body.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Protected_account_endpoint_returns_problem_code_after_bearer_auth()
    {
        await using var factory = new AuthEndpointFactory(
            new ThrowingIdentityService(new ClientAuthException(
                "Authenticated Firebase identity is not linked to a client profile.",
                StatusCodes.Status404NotFound,
                ClientAuthProblemCodes.UnlinkedClientProfile)));
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test-token");

        using var response = await client.GetAsync("/api/favorites/get-client-favorites-ids");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Equal("unlinked_client_profile", body.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Share_ticket_returns_auth_problem_code_when_client_is_unlinked()
    {
        await using var factory = new AuthEndpointFactory(
            new ThrowingIdentityService(new ClientAuthException(
                "Authenticated Firebase identity is not linked to a client profile.",
                StatusCodes.Status404NotFound,
                ClientAuthProblemCodes.UnlinkedClientProfile)));
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test-token");

        using var response = await client.PostAsJsonAsync("/api/tickets/share", new
        {
            ticketId = 123,
            email = "recipient@example.com",
            phone = "526641234567",
            phoneCode = "+52",
            phoneIsoCode = "MX",
            applyToEntireSeason = false
        });

        await AssertProblemCodeAsync(response, HttpStatusCode.NotFound, "unlinked_client_profile");
    }

    [Fact]
    public async Task Unshare_ticket_returns_auth_problem_code_when_client_is_unlinked()
    {
        await using var factory = new AuthEndpointFactory(
            new ThrowingIdentityService(new ClientAuthException(
                "Authenticated Firebase identity is not linked to a client profile.",
                StatusCodes.Status404NotFound,
                ClientAuthProblemCodes.UnlinkedClientProfile)));
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test-token");

        using var response = await client.PostAsJsonAsync("/api/tickets/unshare", new
        {
            ticketId = 123,
            applyToEntireSeason = false
        });

        await AssertProblemCodeAsync(response, HttpStatusCode.NotFound, "unlinked_client_profile");
    }

    [Fact]
    public async Task Renovate_order_returns_auth_problem_code_when_verification_is_required()
    {
        await using var factory = new AuthEndpointFactory(
            new ThrowingIdentityService(new ClientAuthException(
                "Client profile requires a verified Firebase email before account data can be accessed.",
                StatusCodes.Status403Forbidden,
                ClientAuthProblemCodes.VerificationRequired)));
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test-token");

        using var response = await client.GetAsync("/api/orders/renovate/123");

        await AssertProblemCodeAsync(response, HttpStatusCode.Forbidden, "verification_required");
    }

    [Fact]
    public async Task Claim_client_returns_claim_token_invalid_problem_code()
    {
        await using var factory = new AuthEndpointFactory(
            new ThrowingIdentityService(new ClientAuthException(
                "Claim-token client linking is not supported in this auth-only registration flow.",
                StatusCodes.Status400BadRequest,
                ClientAuthProblemCodes.ClaimTokenInvalid)));
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test-token");

        using var response = await client.PostAsJsonAsync("/api/auth/claim-client", new
        {
            claimToken = "claim-token"
        });

        await AssertProblemCodeAsync(response, HttpStatusCode.BadRequest, "claim_token_invalid");
    }

    private static async Task AssertProblemCodeAsync(
        HttpResponseMessage response,
        HttpStatusCode statusCode,
        string code)
    {
        Assert.Equal(statusCode, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Equal(code, body.RootElement.GetProperty("code").GetString());
    }

    private sealed class AuthEndpointFactory(IClientIdentityService identityService) : WebApplicationFactory<Program>
    {
        private readonly string firebaseAppName = $"client-api-tests-{Guid.NewGuid():N}";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Cors:PolicyName"] = "TestCors",
                    ["Cors:AcceptedOrigins:0"] = "https://localhost",
                    ["Database:Database"] = "Host=localhost;Port=5432;Database=XBOL;Username=postgres;Password=12345",
                    ["GcipAuth:TenantId"] = "test-tenant",
                    ["GcipAuth:ProjectId"] = "test-project",
                    ["GcipAuth:ApiKey"] = "test-api-key",
                    ["GcipAuth:ServiceAccountJson"] = "{}",
                    ["TicketingClient:BaseAddress"] = "https://localhost"
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<FirebaseApp>();
                services.AddSingleton(_ => FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromAccessToken("test-token"),
                    ProjectId = "test-project"
                }, firebaseAppName));

                services.RemoveAll<IClientIdentityService>();
                services.AddSingleton(identityService);
                services.RemoveAll<IClientFirebaseTokenVerifier>();
                services.AddSingleton<IClientFirebaseTokenVerifier, FakeTokenVerifier>();
            });
        }

        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
            FirebaseApp.GetInstance(firebaseAppName)?.Delete();
        }
    }

    private sealed class FakeTokenVerifier : IClientFirebaseTokenVerifier
    {
        public Task<VerifiedFirebaseClientToken> VerifyIdTokenAsync(
            string token,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new VerifiedFirebaseClientToken(
                "firebase-route",
                "test-tenant",
                new Dictionary<string, object>
                {
                    ["email"] = "client@example.com",
                    ["email_verified"] = true,
                    ["firebase"] = new Dictionary<string, object>
                    {
                        ["sign_in_provider"] = "password"
                    }
                }));
        }
    }

    private sealed class ContractIdentityService : IClientIdentityService
    {
        public Task<ClientDTO?> TryResolveCurrentClientAsync(ClaimsPrincipal principal)
        {
            return Task.FromResult<ClientDTO?>(null);
        }

        public Task<ClientDTO> RequireCurrentClientAsync(ClaimsPrincipal principal)
        {
            return Task.FromResult(Client);
        }

        public Task<AuthMeResponse> GetMeAsync(ClaimsPrincipal principal)
        {
            var response = new AuthMeResponse
            {
                FirebaseUid = "firebase-route",
                Email = "client@example.com",
                EmailVerified = false,
                PhoneNumber = "+526641234567",
                SignInProvider = "password",
                Client = Client,
                OnboardingStatus = "linked"
            };
            SetOptionalProperty(response, "VerificationStatus", "pending");
            return Task.FromResult(response);
        }

        public Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
        {
            var response = new RegisterResponse
            {
                FirebaseUid = "firebase-route",
                Client = Client,
                OnboardingStatus = "linked"
            };
            SetOptionalProperty(response, "CustomToken", "custom-route-token");
            SetOptionalProperty(response, "IdToken", "custom-route-token");
            SetOptionalProperty(response, "VerificationStatus", "pending");
            return Task.FromResult(response);
        }

        public Task<RegisterResponse> ClaimCurrentClientAsync(ClaimsPrincipal principal, ClaimClientRequest request)
        {
            var response = new RegisterResponse
            {
                FirebaseUid = "firebase-route",
                Client = Client,
                OnboardingStatus = "linked"
            };
            SetOptionalProperty(response, "VerificationStatus", "verified");
            return Task.FromResult(response);
        }

        private static ClientDTO Client => new()
        {
            Id = 123,
            UserId = "firebase-route",
            FullName = "Client Name",
            Email = "client@example.com",
            PhoneNumber = "+526641234567",
            PhoneCode = "+52"
        };

        private static void SetOptionalProperty(object target, string propertyName, object? value)
        {
            target.GetType()
                .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)
                ?.SetValue(target, value);
        }
    }

    private sealed class ThrowingIdentityService(ClientAuthException exception) : IClientIdentityService
    {
        public Task<ClientDTO?> TryResolveCurrentClientAsync(ClaimsPrincipal principal)
        {
            throw exception;
        }

        public Task<ClientDTO> RequireCurrentClientAsync(ClaimsPrincipal principal)
        {
            throw exception;
        }

        public Task<AuthMeResponse> GetMeAsync(ClaimsPrincipal principal)
        {
            throw exception;
        }

        public Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
        {
            throw exception;
        }

        public Task<RegisterResponse> ClaimCurrentClientAsync(ClaimsPrincipal principal, ClaimClientRequest request)
        {
            throw exception;
        }
    }
}
