using Microsoft.Extensions.DependencyInjection;
using Odasoft.XBOL.Business.Services;

namespace Odasoft.XBOL.Business.Extensions
{
    public static class ServiceConfiguration
    {
        public static IServiceCollection ConfigureServices(this IServiceCollection services)
        {
            services.AddScoped<AccountServices>();
            services.AddScoped<EventService>();

            return services;
        }
    }
}
