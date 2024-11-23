using Microsoft.Extensions.Configuration;

namespace Core.Helpers.Env;
internal class EnvConfigurationProvider(FileConfigurationSource source) : FileConfigurationProvider(source)
{
    public override void Load(Stream stream)
    {
        foreach (var item in EnvReader.Load(stream))
        {
            Data[item.Key] = item.Value;
        }
    }
}