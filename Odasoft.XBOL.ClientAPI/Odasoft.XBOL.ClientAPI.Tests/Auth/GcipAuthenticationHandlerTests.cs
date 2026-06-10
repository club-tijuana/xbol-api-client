using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Odasoft.XBOL.ClientAPI.Auth;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Xunit;

namespace Odasoft.XBOL.ClientAPI.Tests.Auth;

public sealed class GcipAuthenticationHandlerTests
{
    [Fact]
    public async Task AuthenticateAsync_accepts_root_firebase_token()
    {
        var verifier = Substitute.For<IFirebaseTokenVerifier>();
        verifier.VerifyIdTokenAsync("root-token", Arg.Any<CancellationToken>())
            .Returns(new VerifiedFirebaseToken(
                "root-uid",
                new Dictionary<string, object>
                {
                    ["email"] = "buyer@example.com",
                    ["email_verified"] = true,
                    ["phone_number"] = "+526641234567",
                    ["firebase"] = new Dictionary<string, object>
                    {
                        ["sign_in_provider"] = "phone"
                    }
                }));
        var handler = CreateHandler(verifier, "Bearer root-token");

        var result = await handler.AuthenticateAsync();

        result.Succeeded.Should().BeTrue();
        result.Principal!.FindFirst(GcipAuthenticationHandler.FirebaseUidClaimType)!.Value
            .Should().Be("root-uid");
        result.Principal.FindFirst(ClaimTypes.MobilePhone)!.Value
            .Should().Be("+526641234567");
        result.Principal.FindFirst(GcipAuthenticationHandler.SignInProviderClaimType)!.Value
            .Should().Be("phone");
    }

    [Fact]
    public async Task AuthenticateAsync_reads_sign_in_provider_from_json_firebase_claim()
    {
        using var firebaseClaimJson = JsonDocument.Parse(
            """
            {
              "identities": {
                "phone": ["+526641234567"]
              },
              "sign_in_provider": "phone"
            }
            """);
        var firebaseClaim = firebaseClaimJson.RootElement.Clone();
        var verifier = Substitute.For<IFirebaseTokenVerifier>();
        verifier.VerifyIdTokenAsync("root-token", Arg.Any<CancellationToken>())
            .Returns(new VerifiedFirebaseToken(
                "root-uid",
                new Dictionary<string, object>
                {
                    ["phone_number"] = "+526641234567",
                    ["firebase"] = firebaseClaim
                }));
        var handler = CreateHandler(verifier, "Bearer root-token");

        var result = await handler.AuthenticateAsync();

        result.Succeeded.Should().BeTrue();
        result.Principal!.FindFirst(GcipAuthenticationHandler.SignInProviderClaimType)!.Value
            .Should().Be("phone");
    }

    private static GcipAuthenticationHandler CreateHandler(
        IFirebaseTokenVerifier verifier,
        string authorizationHeader)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = authorizationHeader;

        var handler = new GcipAuthenticationHandler(
            new StaticOptionsMonitor<GcipAuthenticationOptions>(new GcipAuthenticationOptions()),
            NullLoggerFactory.Instance,
            UrlEncoder.Default,
            verifier);

        var scheme = new AuthenticationScheme(
            GcipAuthenticationHandler.SchemeName,
            null,
            typeof(GcipAuthenticationHandler));
        handler.InitializeAsync(scheme, context).GetAwaiter().GetResult();
        return handler;
    }

    private sealed class StaticOptionsMonitor<TOptions>(TOptions value) : IOptionsMonitor<TOptions>
        where TOptions : class
    {
        public TOptions CurrentValue => value;

        public TOptions Get(string? name)
        {
            return value;
        }

        public IDisposable? OnChange(Action<TOptions, string?> listener)
        {
            return null;
        }
    }
}
