using System.Security.Cryptography;
using System.Text;

namespace Api.Hosting.Services;

public interface ICryptoService
{
    /// <summary>
    /// Creates a new RSA private key and stores it in the specified location.
    /// </summary>
    /// <param name="location">The file path where the private key will be stored.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the generated public key as a string.</returns>
    Task<string> CreateAndStorePrivateKeyAsync(string location);

    string SignStringWithKey(string content, string location);
}

public sealed class CryptoService : ICryptoService
{
    private const string PRIVATE_KEYS_LOCATION = "privateKeys";

    public async Task<string> CreateAndStorePrivateKeyAsync(string location)
    {
        using var rsa = RSA.Create(2048);
        var privateKey = rsa.ExportRSAPrivateKey();

        var path = Path.Combine(PRIVATE_KEYS_LOCATION, location);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllBytesAsync(path, privateKey);

        var publicKey = Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo());

        return publicKey;
    }

    private static byte[] GetPrivateKey(string location)
    {
        var keyPath = Path.Combine(PRIVATE_KEYS_LOCATION, location);
        if (!File.Exists(keyPath))
        {
            throw new FileNotFoundException($"Private key not found for location: {location}");
        }
        return File.ReadAllBytes(keyPath);
    }

    public string SignStringWithKey(string content, string location)
    {
        var privateKey = GetPrivateKey(location);

        using var rsa = RSA.Create();
        rsa.ImportRSAPrivateKey(privateKey, out _);

        byte[] dataToSign = Encoding.UTF8.GetBytes(content);
        byte[] signature = rsa.SignData(dataToSign, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        return Convert.ToBase64String(signature);
    }
}
