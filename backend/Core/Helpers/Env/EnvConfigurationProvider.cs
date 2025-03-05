using Microsoft.Extensions.Configuration;

namespace Core.Helpers.Env;
internal class EnvConfigurationProvider(FileConfigurationSource source) : FileConfigurationProvider(source)
{
    public override void Load(Stream stream)
    {
        foreach (var item in EnvReader.Load(stream))
        {
            var key = ConvertToObjectAndPascalCase(item.Key);
            Data[key] = item.Value.Trim('"');
        }
    }

    private static string ConvertToObjectAndPascalCase(string key)
    {
        var sections = key.Split("__");

        for (int i = 0; i < sections.Length; i++)
        {
            var words = sections[i].Split('_')
                                 .Select(word => word.ToLower())
                                 .Select(word => char.ToUpperInvariant(word[0]) + word[1..])
                                 .ToArray();

            sections[i] = string.Join("", words);
        }

        return string.Join(ConfigurationPath.KeyDelimiter, sections);
    }
}