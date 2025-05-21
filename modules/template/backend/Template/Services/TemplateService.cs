namespace Template.Services;

public interface ITemplateService
{
    string GetObfuscatedName(ReadOnlySpan<char> nonObfuscatedName);
}

public class TemplateService : ITemplateService
{
    public string GetObfuscatedName(ReadOnlySpan<char> nonObfuscatedName)
    {
        Span<char> obfuscatedName = stackalloc char[nonObfuscatedName.Length];
        for (var index = 0; index < nonObfuscatedName.Length; index++)
        {
            var character = nonObfuscatedName[index];
            obfuscatedName[index] = GetRandomChar(character);
        }

        return obfuscatedName.ToString();
    }

    private static char GetRandomChar(char nonObfuscatedCharacter)
    {
        var isUpperCase = char.IsUpper(nonObfuscatedCharacter);
        ReadOnlySpan<char> obfuscatedCharacters = ['a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z'];

        var randomIndex = new Random().Next(0, obfuscatedCharacters.Length);
        var obfuscatedCharacter = obfuscatedCharacters[randomIndex];

        return isUpperCase ? char.ToUpper(obfuscatedCharacter) : obfuscatedCharacter;
    }
}