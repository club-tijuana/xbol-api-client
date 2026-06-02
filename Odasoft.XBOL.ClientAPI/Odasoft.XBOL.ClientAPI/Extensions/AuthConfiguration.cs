using FirebaseAdmin.Auth;
using Odasoft.XBOL.ClientAPI.Auth;
using Odasoft.XBOL.ClientAPI.Services;

namespace Odasoft.XBOL.ClientAPI.Extensions;

public static class AuthConfiguration
{
    public static IServiceCollection ConfigureAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(sp => FirebaseAuth.GetAuth(sp.GetRequiredService<FirebaseAdmin.FirebaseApp>()));
        services.AddSingleton<IFirebaseTokenVerifier, FirebaseTokenVerifier>();
        services.AddSingleton<IFirebaseClientAuthClient, FirebaseClientAuthClient>();
        services.AddScoped<IClientIdentityService, ClientIdentityService>();

        services.AddAuthentication(GcipAuthenticationHandler.SchemeName)
            .AddScheme<GcipAuthenticationOptions, GcipAuthenticationHandler>(
                GcipAuthenticationHandler.SchemeName,
                _ => { });

        services.AddAuthorization();

        return services;
    }
}
