namespace Cli.Services;

public interface IApiKeyService
{
    Task<bool> ValidateApiKeyAsync(string apiKey, string appName);
}

public class ApiKeyService : IApiKeyService
{
    public async Task<bool> ValidateApiKeyAsync(string apiKey, string appName)
    {
        
        await Task.Delay(1000);
        return true;
    }
}