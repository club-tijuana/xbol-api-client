using FluentAssertions;
using Odasoft.XBOL.Business;
using Odasoft.XBOL.Business.Handlers;
using Xunit;

namespace Odasoft.XBOL.ClientAPI.Tests.Handlers;

public sealed class CreateEventBookingHandlerTests
{
    [Fact]
    public void ApplyVerifiedClientIdentity_clears_unverified_request_client_id()
    {
        var contact = new ClientInfoRequest
        {
            Id = 123
        };

        CreateEventBookingHandler.ApplyVerifiedClientIdentity(contact, null);

        contact.Id.Should().BeNull();
    }

    [Fact]
    public void ApplyVerifiedClientIdentity_uses_server_verified_client_id()
    {
        var contact = new ClientInfoRequest
        {
            Id = 123
        };

        CreateEventBookingHandler.ApplyVerifiedClientIdentity(contact, 456);

        contact.Id.Should().Be(456);
    }
}
