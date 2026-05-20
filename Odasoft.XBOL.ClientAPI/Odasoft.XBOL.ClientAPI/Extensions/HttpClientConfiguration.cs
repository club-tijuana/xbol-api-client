using Microsoft.Extensions.Options;
using Odasoft.XBOL.Business;
using Odasoft.XBOL.Commons.Options;

namespace Odasoft.XBOL.ClientAPI.Extensions;

public static class HttpClientConfiguration
{
    public static IServiceCollection ConfigureHttpClients(this IServiceCollection services)
    {
        services.AddHttpClient<ITicketingClient, TicketingClient>((sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<TicketingClientOptions>>();
            client.BaseAddress = new Uri(opts.Value.BaseAddress);
        });

        return services;
    }
}
