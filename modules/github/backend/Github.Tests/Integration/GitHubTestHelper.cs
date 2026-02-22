using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using Octokit;

namespace Github.Tests.Integration;

public static class GitHubTestHelper
{
    public static async Task<IGitHubClient> CreateInstallationClientAsync(
        string appId, string privateKeyBase64, string installationId)
    {
        var privateKeyBytes = Convert.FromBase64String(privateKeyBase64);
        var privateKeyPem = System.Text.Encoding.UTF8.GetString(privateKeyBytes);

        var rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem);

        var signingCredentials = new SigningCredentials(
            new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256);

        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: appId,
            expires: now.AddMinutes(10),
            notBefore: now.AddSeconds(-60),
            signingCredentials: signingCredentials);

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        var appClient = new GitHubClient(new ProductHeaderValue("compozerr-integration-tests"))
        {
            Credentials = new Credentials(jwt, AuthenticationType.Bearer)
        };

        var installationToken = await appClient.GitHubApps.CreateInstallationToken(
            long.Parse(installationId));

        return new GitHubClient(new ProductHeaderValue("compozerr-integration-tests"))
        {
            Credentials = new Credentials(installationToken.Token, AuthenticationType.Bearer)
        };
    }

    public static async Task<string> GetMainBranchShaAsync(
        IGitHubClient client, string owner, string repo)
    {
        var reference = await client.Git.Reference.Get(owner, repo, "heads/main");
        return reference.Object.Sha;
    }

    /// <summary>
    /// Creates a test commit programmatically using the Git Data API.
    /// Returns (beforeSha, afterSha).
    /// </summary>
    public static async Task<(string BeforeSha, string AfterSha)> CreateTestCommitAsync(
        IGitHubClient client,
        string owner,
        string repo,
        Dictionary<string, string> filesToAdd,
        IReadOnlyList<string>? filesToDelete = null)
    {
        var beforeSha = await GetMainBranchShaAsync(client, owner, repo);

        var newTree = new NewTree { BaseTree = beforeSha };

        // Add files
        foreach (var (path, content) in filesToAdd)
        {
            var blob = await client.Git.Blob.Create(owner, repo, new NewBlob
            {
                Content = content,
                Encoding = EncodingType.Utf8
            });

            newTree.Tree.Add(new NewTreeItem
            {
                Path = path,
                Mode = "100644",
                Type = TreeType.Blob,
                Sha = blob.Sha
            });
        }

        // Delete files
        if (filesToDelete is not null)
        {
            foreach (var path in filesToDelete)
            {
                newTree.Tree.Add(new NewTreeItem
                {
                    Path = path,
                    Mode = "100644",
                    Type = TreeType.Blob,
                    Sha = null
                });
            }
        }

        var createdTree = await client.Git.Tree.Create(owner, repo, newTree);

        var newCommit = new NewCommit(
            $"test: integration test commit {Guid.NewGuid():N}",
            createdTree.Sha,
            beforeSha);

        var createdCommit = await client.Git.Commit.Create(owner, repo, newCommit);

        await client.Git.Reference.Update(owner, repo, "heads/main",
            new ReferenceUpdate(createdCommit.Sha));

        return (beforeSha, createdCommit.Sha);
    }

    public static async Task ResetMainBranchAsync(
        IGitHubClient client, string owner, string repo, string targetSha)
    {
        await client.Git.Reference.Update(owner, repo, "heads/main",
            new ReferenceUpdate(targetSha, true));
    }

    public static async Task<string?> ReadFileContentAsync(
        IGitHubClient client, string owner, string repo, string path, string? @ref = null)
    {
        try
        {
            var contents = @ref is not null
                ? await client.Repository.Content.GetAllContentsByRef(owner, repo, path, @ref)
                : await client.Repository.Content.GetAllContents(owner, repo, path);

            return contents.FirstOrDefault()?.Content;
        }
        catch (NotFoundException)
        {
            return null;
        }
    }
}
