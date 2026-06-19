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
            services.AddScoped<PhoneRegionCodeRepository>();
            services.AddScoped<SeasonPassEventTicketRepository>();
            services.AddScoped<ClientCreditTransactionRepository>();
            services.AddScoped<PaymentRepository>();
            services.AddScoped<MediaRepository>();
            services.AddScoped<ClientLoginIdentifierRepository>();
            services.AddScoped<PaymentLinkRepository>();
            services.AddScoped<BundleRepository>();
            services.AddScoped<BundlePassRepository>();
            services.AddScoped<BundlePassEventTicketRepository>();

            return services;
        }
    }
}
