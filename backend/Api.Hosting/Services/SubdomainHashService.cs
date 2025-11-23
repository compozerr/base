using Api.Abstractions;

namespace Api.Hosting.Services;

public interface ISubdomainHashService
{
    /// <summary>
    /// Generates a deterministic hash for subdomain generation based on the project ID.
    /// </summary>
    /// <param name="projectId">The project ID to hash.</param>
    /// <returns>An 8-character lowercase hexadecimal hash.</returns>
    string GetHash(ProjectId projectId);
}

public sealed class SubdomainHashService : ISubdomainHashService
{
    public string GetHash(ProjectId projectId)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(projectId.Value.ToString());
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLower()[..8];
    }
}
