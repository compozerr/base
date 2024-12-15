using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using JWT.Algorithms;
using JWT.Builder;
using JWT.Exceptions;

namespace Core.Services;

public interface IJsonWebTokenService
{
    string CreateToken(string privateKeyCertificateBase64, string issuer, DateTime expireAtUtc, IEnumerable<Claim>? claims = null);
    IEnumerable<Claim> GetClaims(string token, string privateKeyCertificateBase64);
    bool ValidateToken(string token, string privateKeyCertificateBase64);
}

public class JsonWebTokenService(IDateTimeProvider dateTimeProvider) : IJsonWebTokenService
{
    private static X509Certificate2 GetCertificateFromBase64(string privateKeyCertificateBase64)
    {
        var certBytes = Convert.FromBase64String(privateKeyCertificateBase64);
        var pemString = System.Text.Encoding.UTF8.GetString(certBytes);
        return X509Certificate2.CreateFromPem(pemString);
    }

    public string CreateToken(string privateKeyCertificateBase64, string issuer, DateTime expireAtUtc, IEnumerable<Claim>? claims = null)
    {
        var token = JwtBuilder.Create()
                              .WithAlgorithm(new RS256Algorithm(GetCertificateFromBase64(privateKeyCertificateBase64)))
                              .AddClaim("iat", new DateTimeOffset(dateTimeProvider.UtcNow).ToUnixTimeSeconds())
                              .AddClaim("exp", new DateTimeOffset(expireAtUtc).ToUnixTimeSeconds())
                              .AddClaim("iss", issuer)
                              .AddClaims(claims?.Select(c => new KeyValuePair<string, object>(c.Type, c.Value)) ?? [])
                              .Encode();

        return token;
    }
    public IEnumerable<Claim> GetClaims(string token, string privateKeyCertificateBase64)
    {
        var cert = GetCertificateFromBase64(privateKeyCertificateBase64);
        var json = new JwtBuilder()
            .WithAlgorithm(new RS256Algorithm(cert))
            .MustVerifySignature()
            .Decode(token);

        var claims = new JwtBuilder()
            .WithAlgorithm(new RS256Algorithm(cert))
            .MustVerifySignature()
            .Decode<IDictionary<string, object>>(token);

        return claims.Select(c => new Claim(c.Key, c.Value.ToString()!));
    }

    public bool ValidateToken(string token, string privateKeyCertificateBase64)
    {
        var cert = GetCertificateFromBase64(privateKeyCertificateBase64);
        try
        {
            new JwtBuilder()
                .WithAlgorithm(new RS256Algorithm(cert))
                .MustVerifySignature()
                .Decode(token);
            return true;
        }
        catch (TokenExpiredException)
        {
            Console.WriteLine("Token has expired");
            return false;
        }
        catch (SignatureVerificationException)
        {
            Console.WriteLine("Token has invalid signature");
            return false;
        }
    }
}

