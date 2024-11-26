using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Cli.Services.Hosting.Providers.Flyio;

public interface IFlyioNameGenerator
{
    public string GenerateName(string appName, Platform platform);
}

public class FlyioNameGenerator : IFlyioNameGenerator
{
    public string GenerateName(string appName, Platform platform)
    {
        var platformAsString = platform.ToString().ToLower();

        appName = Regex.Replace(appName, "\\s|-", "").ToLower();

        var hashedName = GetHashedName(appName);

        return $"{platformAsString}-{appName}-{hashedName[..6]}";
    }

    private static string GetHashedName(string appName)
    {
        using SHA256 sha256 = SHA256.Create();
        
        byte[] inputBytes = Encoding.UTF8.GetBytes(appName);
        byte[] hashBytes = sha256.ComputeHash(inputBytes);

        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < hashBytes.Length; i++)
        {
            builder.Append(hashBytes[i].ToString("x2")); // Convert each byte to a hexadecimal string
        }

        return builder.ToString();

    }
}