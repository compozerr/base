using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Core.Options;
using JWT.Algorithms;
using JWT.Builder;
using JWT.Exceptions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Core.Services;

public interface IJsonWebTokenService
{
    string CreateToken(
        string privateKeyCertificateBase64,
        string issuer,
        DateTime expireAtUtc,
        IEnumerable<Claim>? claims = null);

    string CreateToken(
        ClaimsPrincipal claimsPrincipal,
        DateTime? ExpiresAtUtc = null);

    IEnumerable<Claim> GetClaims(
        string token,
        string privateKeyCertificateBase64);

    bool ValidateToken(
        string token,
        string privateKeyCertificateBase64);
}

public class JsonWebTokenService(
    IDateTimeProvider dateTimeProvider,
    IOptions<JwtOptions> jwtOptions) : IJsonWebTokenService
{
    private static (RSA Private, RSA Public) GetRsaKeysFromBase64(string privateKeyCertificateBase64)
    {
        var keyBytes = Convert.FromBase64String(privateKeyCertificateBase64);
        var pemString = Encoding.UTF8.GetString(keyBytes);

        var privateRsa = RSA.Create();
        privateRsa.ImportFromPem(pemString);

        var publicRsa = RSA.Create();
        publicRsa.ImportRSAPublicKey(privateRsa.ExportRSAPublicKey(), out _);

        return (privateRsa, publicRsa);
    }

    public string CreateToken(string privateKeyCertificateBase64, string issuer, DateTime expireAtUtc, IEnumerable<Claim>? claims = null)
    {
        var (privateKey, publicKey) = GetRsaKeysFromBase64(privateKeyCertificateBase64);
        var token = JwtBuilder.Create()
                              .WithAlgorithm(new RS256Algorithm(publicKey, privateKey))
                              .AddClaim("iat", new DateTimeOffset(dateTimeProvider.UtcNow).ToUnixTimeSeconds())
                              .AddClaim("exp", new DateTimeOffset(expireAtUtc).ToUnixTimeSeconds())
                              .AddClaim("iss", issuer)
                              .AddClaims(claims?.Select(c => new KeyValuePair<string, object>(c.Type, c.Value)) ?? [])
                              .Encode();

        return token;
    }

    public string CreateToken(ClaimsPrincipal claimsPrincipal, DateTime? ExpiresAtUtc = null)
    {
        var claims = claimsPrincipal.Claims.ToList();

        claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Value.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtOptions.Value.Issuer,
            audience: jwtOptions.Value.Audience,
            claims: claims,
            expires: ExpiresAtUtc ?? dateTimeProvider.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public IEnumerable<Claim> GetClaims(string token, string privateKeyCertificateBase64)
    {
        var (privateKey, publicKey) = GetRsaKeysFromBase64(privateKeyCertificateBase64);
        var json = new JwtBuilder()
            .WithAlgorithm(new RS256Algorithm(publicKey, privateKey))
            .MustVerifySignature()
            .Decode(token);

        var claims = new JwtBuilder()
            .WithAlgorithm(new RS256Algorithm(publicKey, privateKey))
            .MustVerifySignature()
            .Decode<IDictionary<string, object>>(token);

        return claims.Select(c => new Claim(c.Key, c.Value.ToString()!));
    }

    public bool ValidateToken(string token, string privateKeyCertificateBase64)
    {
        var (privateKey, publicKey) = GetRsaKeysFromBase64(privateKeyCertificateBase64);
        try
        {
            new JwtBuilder()
                .WithAlgorithm(new RS256Algorithm(publicKey, privateKey))
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

