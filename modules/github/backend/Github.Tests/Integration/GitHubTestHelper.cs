using System.Text;
using System.Text.Json;
using Octokit;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;

namespace Github.Tests.Integration;

public static class GitHubTestHelper
{
    public static async Task<IGitHubClient> CreateInstallationClientAsync(
        string appId, string privateKeyBase64, string installationId)
    {
        var privateKeyBytes = Convert.FromBase64String(privateKeyBase64);
        var pemString = Encoding.UTF8.GetString(privateKeyBytes);

        // Use BouncyCastle for RSA signing to avoid macOS Apple crypto issues
        // (SecKeyCreateSignature fails with OSStatus -50 for GitHub App PKCS#1 keys)
        var pemReader = new PemReader(new StringReader(pemString));
        var keyPair = (AsymmetricCipherKeyPair)pemReader.ReadObject();
        var privateKey = (RsaPrivateCrtKeyParameters)keyPair.Private;

        var now = DateTimeOffset.UtcNow;
        var jwt = CreateJwtWithBouncyCastle(privateKey, appId, now);

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
    /// <param name="parentSha">Optional explicit parent SHA. If null, reads current HEAD of main.</param>
    public static async Task<(string BeforeSha, string AfterSha)> CreateTestCommitAsync(
        IGitHubClient client,
        string owner,
        string repo,
        Dictionary<string, string> filesToAdd,
        IReadOnlyList<string>? filesToDelete = null,
        string? parentSha = null)
    {
        var beforeSha = parentSha ?? await GetMainBranchShaAsync(client, owner, repo);

        var deleteSet = filesToDelete?.ToHashSet() ?? [];

        if (deleteSet.Count > 0)
        {
            // For deletions, build the full tree without BaseTree:
            // get the current recursive tree, filter out deleted paths, add new files
            var currentTree = await client.Git.Tree.GetRecursive(owner, repo, beforeSha);
            var newTree = new NewTree();

            foreach (var item in currentTree.Tree)
            {
                if (item.Type == TreeType.Blob && !deleteSet.Contains(item.Path))
                {
                    newTree.Tree.Add(new NewTreeItem
                    {
                        Path = item.Path,
                        Mode = item.Mode,
                        Type = TreeType.Blob,
                        Sha = item.Sha
                    });
                }
            }

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

            var createdDeleteTree = await client.Git.Tree.Create(owner, repo, newTree);
            var deleteCommit = new NewCommit(
                $"test: integration test commit {Guid.NewGuid():N}",
                createdDeleteTree.Sha,
                beforeSha);

            var createdDeleteCommit = await client.Git.Commit.Create(owner, repo, deleteCommit);
            await client.Git.Reference.Update(owner, repo, "heads/main",
                new ReferenceUpdate(createdDeleteCommit.Sha, true));

            return (beforeSha, createdDeleteCommit.Sha);
        }

        // Add-only path: use BaseTree for efficiency
        var addTree = new NewTree { BaseTree = beforeSha };

        foreach (var (path, content) in filesToAdd)
        {
            var blob = await client.Git.Blob.Create(owner, repo, new NewBlob
            {
                Content = content,
                Encoding = EncodingType.Utf8
            });

            addTree.Tree.Add(new NewTreeItem
            {
                Path = path,
                Mode = "100644",
                Type = TreeType.Blob,
                Sha = blob.Sha
            });
        }

        var createdTree = await client.Git.Tree.Create(owner, repo, addTree);

        var newCommit = new NewCommit(
            $"test: integration test commit {Guid.NewGuid():N}",
            createdTree.Sha,
            beforeSha);

        var createdCommit = await client.Git.Commit.Create(owner, repo, newCommit);

        await client.Git.Reference.Update(owner, repo, "heads/main",
            new ReferenceUpdate(createdCommit.Sha, true));

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

    private static string CreateJwtWithBouncyCastle(
        RsaPrivateCrtKeyParameters privateKey, string issuer, DateTimeOffset now)
    {
        var header = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(
            new { alg = "RS256", typ = "JWT" }));

        var payload = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(new
        {
            iat = now.AddSeconds(-60).ToUnixTimeSeconds(),
            exp = now.AddMinutes(10).ToUnixTimeSeconds(),
            iss = issuer
        }));

        var dataToSign = Encoding.ASCII.GetBytes($"{header}.{payload}");

        var signer = SignerUtilities.GetSigner("SHA-256withRSA");
        signer.Init(true, privateKey);
        signer.BlockUpdate(dataToSign, 0, dataToSign.Length);
        var signature = signer.GenerateSignature();

        return $"{header}.{payload}.{Base64UrlEncode(signature)}";
    }

    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
