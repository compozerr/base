using Api.Options;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace Api.Services;

public interface IEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string encryptedText);
}

public class EncryptionService(IOptions<EncryptionOptions> options) : IEncryptionService
{
    public string Encrypt(string plainText)
    {
        var secret = options.Value.Secret;

        using var aes = Aes.Create();
        var key = Encoding.UTF8.GetBytes(secret);
        Array.Resize(ref key, 16);
        aes.Key = key;

        aes.GenerateIV();
        var iv = aes.IV;

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        var result = new byte[iv.Length + cipherBytes.Length];
        iv.CopyTo(result, 0);
        cipherBytes.CopyTo(result, iv.Length);
        
        return Convert.ToBase64String(result);
    }

    public string Decrypt(string encryptedText)
    {
        var fullBytes = Convert.FromBase64String(encryptedText);

        using var aes = Aes.Create();
        var key = Encoding.UTF8.GetBytes(options.Value.Secret);
        Array.Resize(ref key, 16);
        aes.Key = key;

        var iv = new byte[16];
        Array.Copy(fullBytes, 0, iv, 0, 16);
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        var cipherBytes = new byte[fullBytes.Length - 16];
        Array.Copy(fullBytes, 16, cipherBytes, 0, cipherBytes.Length);
        var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

        return Encoding.UTF8.GetString(plainBytes);
    }
}