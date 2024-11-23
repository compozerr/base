using Microsoft.Extensions.Configuration;

namespace Cli.Services;

public interface IApiKeyService
{
    Task<bool> IsValidApiKeyAsync(string apiKey, string appName);
}

public class ApiKeyService(IConfiguration configuration) : IApiKeyService
{
    public async Task<bool> IsValidApiKeyAsync(string apiKey, string appName)
    {
        await Task.CompletedTask;
        
        var globalAccessToken = configuration["GLOBAL_ACCESS_TOKEN"];
        if (string.IsNullOrEmpty(globalAccessToken))
        {
            Log.Error("GLOBAL_ACCESS_TOKEN is not set");
            return false;
        }

        if (string.IsNullOrEmpty(apiKey))
        {
            return false;
        }

        if (apiKey != globalAccessToken)
        {
            return false;
        }

        return true;
    }
}