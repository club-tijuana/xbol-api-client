using FirebaseAdmin.Auth;
using FirebaseAdmin.Auth.Multitenancy;
using Microsoft.Extensions.Options;
using Odasoft.XBOL.ClientAPI.Auth;
using Odasoft.XBOL.ClientAPI.Services;
using Odasoft.XBOL.Commons.Options;

namespace Odasoft.XBOL.ClientAPI.Extensions;

public static class AuthConfiguration
{
    public static IServiceCollection ConfigureAuth(this IServiceCollection services, IConfiguration configuration)
    {
        var options = new GcipAuthOptions();
        configuration.GetSection(GcipAuthOptions.SectionName).Bind(options);

        services.AddSingleton(sp => FirebaseAuth.GetAuth(sp.GetRequiredService<FirebaseAdmin.FirebaseApp>()));
        services.AddSingleton(sp =>
        {
            var authOptions = sp.GetRequiredService<IOptions<GcipAuthOptions>>().Value;
            return sp.GetRequiredService<FirebaseAuth>().TenantManager.AuthForTenant(authOptions.TenantId);
        });
        services.AddSingleton<IClientFirebaseTokenVerifier, FirebaseAdminClientTokenVerifier>();

        services.AddSingleton<IFirebaseTenantAuthClient, FirebaseTenantAuthClient>();
        services.AddScoped<IClientIdentityService, ClientIdentityService>();

        services.AddAuthentication(GcipAuthenticationHandler.SchemeName)
            .AddScheme<GcipAuthenticationOptions, GcipAuthenticationHandler>(
                GcipAuthenticationHandler.SchemeName,
                schemeOptions =>
                {
                    schemeOptions.TenantId = options.TenantId;
                });

        services.AddAuthorization();

        return services;
    }
}
