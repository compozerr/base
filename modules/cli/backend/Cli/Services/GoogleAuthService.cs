using Microsoft.Extensions.Configuration;

namespace Cli.Services;

public class GoogleAuthService
{
    private readonly IConfiguration _configuration;

    public GoogleAuthService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void SetupGoogleCredentials()
    {
        var serviceAccountJson = _configuration["GoogleCloud:ServiceAccountCredentials"];
        File.WriteAllText("/tmp/google-credentials.json", serviceAccountJson);
        Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", "/tmp/google-credentials.json");
    }
}