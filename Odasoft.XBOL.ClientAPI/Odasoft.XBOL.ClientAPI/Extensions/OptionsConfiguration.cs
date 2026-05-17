using Odasoft.XBOL.Commons.Options;

namespace Odasoft.XBOL.ClientAPI.Extensions;

public static class OptionsConfiguration
{
    public static IServiceCollection ConfigureOptions(this IServiceCollection services)
    {
        services.AddOptions<CorsOptions>()
            .BindConfiguration(CorsOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<DatabaseOptions>()
            .BindConfiguration(DatabaseOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<GcipAuthOptions>()
            .BindConfiguration(GcipAuthOptions.SectionName)
            .ValidateDataAnnotations()
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ServiceAccountJson)
                    || !string.IsNullOrWhiteSpace(options.ServiceAccountJsonPath),
                "GcipAuth:ServiceAccountJson or GcipAuth:ServiceAccountJsonPath is required.")
            .ValidateOnStart();

        services.AddOptions<TicketingClientOptions>()
            .BindConfiguration("TicketingClient")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}
