namespace Cli.Services;

public interface IApiKeyService
{
    Task<bool> ValidateApiKeyAsync();
}

public class ApiKeyService : IApiKeyService
{
    public async Task<bool> ValidateApiKeyAsync()
    {
        await Task.Delay(1000);
        return true;
    }
}