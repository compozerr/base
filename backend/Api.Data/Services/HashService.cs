using System.Security.Cryptography;
using System.Text;

namespace Api.Data.Services;

public interface IHashService
{
    string Hash(string plainText);
}

public class HashService : IHashService
{
    public string Hash(string plainText)
    {
        var bytes = Encoding.UTF8.GetBytes(plainText);
        var hash = SHA256.HashData(bytes);

        return Convert.ToBase64String(hash);
    }
}
