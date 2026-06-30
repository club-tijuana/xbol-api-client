using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Odasoft.XBOL.Business;
using Odasoft.XBOL.ClientAPI.Controllers;
using Odasoft.XBOL.ClientAPI.Services;
using Odasoft.XBOL.DTO;
using System.Security.Claims;
using Xunit;

namespace Odasoft.XBOL.ClientAPI.Tests.Controllers;

public sealed class PaymentControllerTests
{
    [Fact]
    public async Task InitiateCheckoutAsync_clears_unverified_client_contact_id()
    {
        var ticketingClient = Substitute.For<ITicketingClient>();
        var identityService = Substitute.For<IClientIdentityService>();
        InitiateCheckoutRequest? forwardedRequest = null;
        var request = CreateRequest(clientContactId: 123);

        ticketingClient
            .InitiateCheckoutAsync(Arg.Do<InitiateCheckoutRequest?>(x => forwardedRequest = x))
            .Returns(Task.FromResult(CreateResponse()));
        identityService
            .TryResolveCurrentClientAsync(Arg.Any<ClaimsPrincipal>())
            .Returns(Task.FromResult<ClientDTO?>(null));

        var controller = CreateController(ticketingClient, identityService);

        await controller.InitiateCheckoutAsync(request);

        forwardedRequest.Should().NotBeNull();
        forwardedRequest!.ClientContact.Id.Should().BeNull();
        forwardedRequest.ClientContact.Email.Should().Be("buyer@example.com");
    }

    [Fact]
    public async Task InitiateCheckoutAsync_uses_server_verified_client_id()
    {
        var ticketingClient = Substitute.For<ITicketingClient>();
        var identityService = Substitute.For<IClientIdentityService>();
        InitiateCheckoutRequest? forwardedRequest = null;
        var request = CreateRequest(clientContactId: 123);

        ticketingClient
            .InitiateCheckoutAsync(Arg.Do<InitiateCheckoutRequest?>(x => forwardedRequest = x))
            .Returns(Task.FromResult(CreateResponse()));
        identityService
            .TryResolveCurrentClientAsync(Arg.Any<ClaimsPrincipal>())
            .Returns(Task.FromResult<ClientDTO?>(new ClientDTO { Id = 456 }));

        var controller = CreateController(ticketingClient, identityService, isAuthenticated: true);

        await controller.InitiateCheckoutAsync(request);

        forwardedRequest.Should().NotBeNull();
        forwardedRequest!.ClientContact.Id.Should().Be(456);
        forwardedRequest.ClientContact.Email.Should().Be("buyer@example.com");
    }

    [Fact]
    public async Task InitiateCheckoutAsync_rejects_anonymous_renewal_checkout()
    {
        var ticketingClient = Substitute.For<ITicketingClient>();
        var identityService = Substitute.For<IClientIdentityService>();
        var request = CreateRequest(clientContactId: 123);
        request.RelatedOrderId = 987;

        identityService
            .TryResolveCurrentClientAsync(Arg.Any<ClaimsPrincipal>())
            .Returns(Task.FromResult<ClientDTO?>(null));

        var controller = CreateController(ticketingClient, identityService);

        var result = await controller.InitiateCheckoutAsync(request);

        result.Result.Should().BeOfType<UnauthorizedObjectResult>()
            .Which.Value.Should().Be("Renewal checkout requires an authenticated client.");
        await ticketingClient.DidNotReceiveWithAnyArgs().InitiateCheckoutAsync(default!);
    }

    private static PaymentController CreateController(
        ITicketingClient ticketingClient,
        IClientIdentityService identityService,
        bool isAuthenticated = false)
    {
        var user = isAuthenticated
            ? new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "firebase-uid")], "Firebase"))
            : new ClaimsPrincipal(new ClaimsIdentity());

        return new PaymentController(ticketingClient, identityService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = user
                }
            }
        };
    }

    private static InitiateCheckoutRequest CreateRequest(long clientContactId)
    {
        return new InitiateCheckoutRequest
        {
            EventScheduleId = 1,
            HoldToken = "hold-token",
            Seats =
            [
                new CheckoutSeatRequest
                {
                    SeatKey = "A-1",
                    PriceListItemId = 10
                }
            ],
            ClientContact = new ClientInfoRequest
            {
                Id = clientContactId,
                Email = "buyer@example.com",
                FullName = "Buyer Example",
                FirstName = "Buyer",
                LastName = "Example",
                PhoneRegionCodeId = 1,
                PhoneNumber = "6641234567"
            },
            ReturnUrl = "https://example.com/checkout-return",
            Currency = "MXN"
        };
    }

    private static InitiateCheckoutResponse CreateResponse()
    {
        return new InitiateCheckoutResponse
        {
            LocalOrderId = 1,
            SessionId = "session-id",
            SuccessIndicator = "success-indicator",
            OrderRefId = "order-ref",
            Amount = "100.00",
            Currency = "MXN",
            MerchantId = "merchant-id",
            ApiVersion = "100",
            GatewayBaseUrl = "https://gateway.example.com"
        };
    }
}
