using Microsoft.Extensions.DependencyInjection;
using Odasoft.XBOL.Business.Services;

namespace Odasoft.XBOL.Business.Extensions
{
    public static class ServiceConfiguration
    {
        public static IServiceCollection ConfigureServices(this IServiceCollection services)
        {
            services.AddScoped<AccountService>();
            services.AddScoped<EventService>();
            services.AddScoped<ClientService>();
            services.AddScoped<BookingService>();
            services.AddScoped<OrderService>();
            services.AddScoped<TicketService>();
            services.AddScoped<SeasonService>();
            services.AddScoped<ClientFavoriteEventService>();

            return services;
        }
    }
}
