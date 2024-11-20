using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Cli.Services;

public class GoogleAuthService(IConfiguration configuration)
{
    public void SetupGoogleCredentials()
    {
        const string GOOGLE_APPLICATION_CREDENTIALS_PATH = "/tmp/google-credentials.json";
        
        var serviceAccountRaw = configuration["GOOGLECLOUD_SERVICEACCOUNTCREDENTIALS"];
        if (string.IsNullOrEmpty(serviceAccountRaw))
        {
            Log.Logger.Error("GOOGLECLOUD_SERVICEACCOUNTCREDENTIALS is not set");
            return;
        }

        var serviceAccountJson = JsonDocument.Parse(serviceAccountRaw);

        File.WriteAllText(GOOGLE_APPLICATION_CREDENTIALS_PATH, serviceAccountJson.RootElement.ToString());

        Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", GOOGLE_APPLICATION_CREDENTIALS_PATH);
    }
}