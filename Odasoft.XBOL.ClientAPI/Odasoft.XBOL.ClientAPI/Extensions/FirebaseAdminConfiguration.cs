using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Options;
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
            Credential = !string.IsNullOrWhiteSpace(options.ServiceAccountJson)
#pragma warning disable CS0618
                ? GoogleCredential.FromJson(options.ServiceAccountJson)
                : GoogleCredential.FromFile(options.ServiceAccountJsonPath!)
#pragma warning restore CS0618
        };

        if (!string.IsNullOrWhiteSpace(options.ProjectId))
        {
            appOptions.ProjectId = options.ProjectId;
        }

        return appOptions;
    }
}
