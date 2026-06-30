using FluentAssertions;
using Odasoft.XBOL.Commons.Enums;
using Xunit;

namespace Odasoft.XBOL.ClientAPI.Tests.Services;

public sealed class ClientSaleTypeContractTests
{
    [Fact]
    public void ClientSaleType_UsesPersistedMediaReferenceValues()
    {
        ((int)ClientSaleType.Event).Should().Be(0);
        ((int)ClientSaleType.SeasonPass).Should().Be(1);
        ((int)ClientSaleType.Bundle).Should().Be(2);
    }
}
