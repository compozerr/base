using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace Github.Services;

public interface IStateService
{
    public string Serialize<T>(T state);
    public T Deserialize<T>(string state);
}

public class StateService(IDataProtectionProvider dataProtectionProvider) : IStateService
{
    private readonly IDataProtector _dataProtector = dataProtectionProvider.CreateProtector(nameof(StateService));

    public string Serialize<T>(T state)
    {
        var json = JsonSerializer.Serialize(state);
        return _dataProtector.Protect(json);
    }

    public T Deserialize<T>(string state)
    {
        var json = _dataProtector.Unprotect(state);
        return JsonSerializer.Deserialize<T>(json) ?? throw new InvalidOperationException("Failed to deserialize state");
    }
}