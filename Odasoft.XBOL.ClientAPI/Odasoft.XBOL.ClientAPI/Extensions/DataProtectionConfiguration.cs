using Microsoft.AspNetCore.DataProtection;

namespace Odasoft.XBOL.ClientAPI.Extensions;

public static class DataProtectionConfiguration
{
    private const string ApplicationName = "Odasoft.XBOL.ClientAPI";

    public static IServiceCollection ConfigureDataProtection(this IServiceCollection services)
    {
        var keysPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".aspnet",
            "DataProtection-Keys");

        services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
            .SetApplicationName(ApplicationName);

        return services;
    }
}
