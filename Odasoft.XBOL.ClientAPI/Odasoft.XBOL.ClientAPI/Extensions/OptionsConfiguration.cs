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

        services.AddOptions<AuthenticationOptions>()
            .BindConfiguration(AuthenticationOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}
