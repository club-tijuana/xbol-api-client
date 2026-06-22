using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Odasoft.XBOL.Business;
using Odasoft.XBOL.Business.Services;
using Odasoft.XBOL.ClientAPI.Controllers;
using Odasoft.XBOL.ClientAPI.Services;
using Odasoft.XBOL.DTO;
using Odasoft.XBOL.DTO.Results;
using System.Security.Claims;
using Xunit;

namespace Odasoft.XBOL.ClientAPI.Tests.Controllers;

public sealed class SeatManagementControllerTests
{
    [Fact]
    public async Task BookSeatsAsync_bundle_passes_ticket_and_bundle_pass_ids_through()
    {
        var ticketingClient = Substitute.For<ITicketingClient>();
        var identityService = Substitute.For<IClientIdentityService>();
        var expiresAt = DateTimeOffset.UtcNow.AddDays(1);
        ticketingClient.BookSeatsActionAsync(Arg.Any<BookSeatsActionRequest>())
            .Returns(Task.FromResult(new BookingResultResponse
            {
                OrderId = 900,
                Reference = "ORD-B-900",
                BookedSeatKeys = ["A-1"],
                TicketIds = [700],
                BundlePassIds = [800]
            }));
        identityService.TryResolveCurrentClientAsync(Arg.Any<ClaimsPrincipal>())
            .Returns(Task.FromResult<ClientDTO?>(new ClientDTO { Id = 456 }));
        var controller = CreateController(ticketingClient, identityService);

        var response = await controller.BookSeatsAsync(new BookSeatsBody
        {
            Seats =
            [
                new BookingSeatRequest
                {
                    SeatKey = "A-1",
                    SeatPrice = 140m,
                    PriceListItemId = 501
                }
            ],
            BundleId = 20,
            TicketType = ItemType.BundlePass,
            ClientContact = new ClientInfoRequest
            {
                Email = "buyer@example.com",
                PhoneNumber = "6641234567"
            },
            PaymentInfoRequest = new PaymentInfoRequest
            {
                CardAmount = 140m
            },
            ChangeInfoRequest = new ChangeInfoRequest(),
            IsPaymentLink = true,
            PaymentLinkRequest = new PaymentLinkRequest
            {
                ExpiresAt = expiresAt
            }
        });

        var ok = response.Result.Should().BeOfType<OkObjectResult>().Subject;
        var result = ok.Value.Should().BeOfType<BookingResult>().Subject;
        result.BookingId.Should().Be(900);
        result.Tickets.Should().BeEquivalentTo(["A-1"]);
        result.TicketIds.Should().BeEquivalentTo([700]);
        result.BundlePassIds.Should().BeEquivalentTo([800]);

        await ticketingClient.Received(1).BookSeatsActionAsync(Arg.Is<BookSeatsActionRequest?>(request =>
            request != null &&
            request.IsPaymentLink == true &&
            request.PaymentLinkRequest != null &&
            request.PaymentLinkRequest.ExpiresAt == expiresAt));
    }

    private static SeatManagementController CreateController(
        ITicketingClient ticketingClient,
        IClientIdentityService identityService)
    {
        return new SeatManagementController(null!, ticketingClient, identityService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity())
                }
            }
        };
    }
}
