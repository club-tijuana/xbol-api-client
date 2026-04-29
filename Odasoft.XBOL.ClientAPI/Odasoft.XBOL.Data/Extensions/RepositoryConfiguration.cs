using Microsoft.Extensions.DependencyInjection;
using Odasoft.XBOL.Data.Repositories;

namespace Odasoft.XBOL.Data.Extensions
{
    public static class RepositoryConfiguration
    {
        public static IServiceCollection ConfigureRepositories(this IServiceCollection services)
        {
            services.AddScoped<EventRepository>();
            services.AddScoped<ClientRepository>();
            services.AddScoped<OrderRepository>();
            services.AddScoped<EventViewRepository>();
            services.AddScoped<EventCategoryRepository>();
            services.AddScoped<EventScheduleRepository>();
            services.AddScoped<EventSectionRepository>();
            services.AddScoped<TicketRepository>();
            services.AddScoped<EventSeatRepository>();
            services.AddScoped<SeasonPassRepository>();
            services.AddScoped<SeasonPassEventTicketRepository>();
            services.AddScoped<SeasonRepository>();
            services.AddScoped<ClientFavoriteEventRepository>();
            services.AddScoped<SeasonSeatRepository>();
            services.AddScoped<SequenceTrackerRepository>();

            return services;
        }
    }
}
