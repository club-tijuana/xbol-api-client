using FirebaseAdmin;
using Microsoft.Extensions.Options;
using Odasoft.XBOL.ClientAPI.Services;
using Odasoft.XBOL.Commons.Options;

namespace Odasoft.XBOL.ClientAPI.Extensions;

public static class FirebaseAdminConfiguration
{
    public static IServiceCollection ConfigureFirebaseAdmin(this IServiceCollection services)
    {
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<GcipAuthOptions>>().Value;
            return FirebaseApp.DefaultInstance ?? FirebaseApp.Create(BuildAppOptions(options));
        });

        return services;
    }

    private static AppOptions BuildAppOptions(GcipAuthOptions options)
    {
        var appOptions = new AppOptions
        {
            Credential = GoogleServiceAccountCredentialFactory.Create(
                options.ServiceAccountJson,
                options.ServiceAccountJsonPath)
        };

        if (!string.IsNullOrWhiteSpace(options.ProjectId))
        {
            appOptions.ProjectId = options.ProjectId;
        }

        return appOptions;
    }
}
