using System.Diagnostics;
using Carter;
using Cli.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;

namespace Cli.Features.Docker;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Text;
using Google.Apis.Auth.OAuth2;
using Serilog;

public class DockerPush : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/docker/push", async (
            HttpContext context,
            [FromHeader(Name = "x-api-key")] string apiKey,
            [FromHeader(Name = "x-app-name")] string appName,
            IApiKeyService apiKeyService,
            IConfiguration configuration,
            GoogleAuthService googleAuthService) =>
        {
            try
            {
                googleAuthService.SetupGoogleCredentials();
                Log.Logger.Information("Activating service account");

                // Activate service account
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "gcloud",
                        Arguments = $"auth activate-service-account --key-file {Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS")}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false
                    }
                };

                process.Start();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    Log.Logger.Error($"Failed to activate service account {process.StandardError.ReadToEnd()}");
                    return Results.Problem("Failed to activate service account");
                }
                Log.Logger.Information("Service account activated");

                var config = GetGoogleCloudConfiguration(configuration);
                var registryPath = $"europe-west3-docker.pkg.dev/{config.ProjectId}/{config.RepositoryName}/{appName}";


                // Authenticate Docker registry
                await AuthenticateDockerRegistryAsync(config.Region);

                // Load the image from the stream
                var loadProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "docker",
                        Arguments = "load",
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false
                    }
                };
                Log.Logger.Information("Loading docker image");

                foreach (var header in context.Request.Headers)
                {
                    Log.Logger.Information($"Header: {header.Key} = {header.Value}");
                }

                loadProcess.Start();
                if (!context.Request.Headers.TryGetValue("ContentSize", out var totalBytesString) || !long.TryParse(totalBytesString, out var totalBytes))
                    throw new InvalidOperationException("ContentSize not found");

                var buffer = new byte[81920 * 64];
                int bytesRead;
                long totalRead = 0;

                while ((bytesRead = await context.Request.Body.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await loadProcess.StandardInput.BaseStream.WriteAsync(buffer, 0, bytesRead);
                    totalRead += bytesRead;
                    var percentage = Math.Min((double)totalRead / totalBytes * 100, 100);
                    Log.Logger.Information($"Loading Docker image: {percentage:F2}%");
                }

                loadProcess.StandardInput.Close();
                await loadProcess.WaitForExitAsync();

                if (loadProcess.ExitCode != 0)
                {
                    Log.Logger.Error("Failed to load Docker image");
                    return Results.Problem("Failed to load Docker image");
                }

                Log.Logger.Information("Docker image loaded");

                // Tag the image
                var tagProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "docker",
                        Arguments = $"tag {appName} {registryPath}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false
                    }
                };

                Log.Logger.Information("Tagging Docker image");

                tagProcess.Start();
                await tagProcess.WaitForExitAsync();

                if (tagProcess.ExitCode != 0)
                {
                    Log.Logger.Error("Failed to tag Docker image");
                    return Results.Problem("Failed to tag Docker image");
                }

                Log.Logger.Information("Docker image tagged");

                // Push the tagged image
                var pushProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "docker",
                        Arguments = $"push {registryPath}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false
                    }
                };

                Log.Logger.Information("Pushing Docker image");
                pushProcess.Start();
                await pushProcess.WaitForExitAsync();

                if (pushProcess.ExitCode != 0)
                {
                    Log.Logger.Error($"Failed to push Docker image {pushProcess.StandardError.ReadToEnd()}");
                    return Results.Problem("Failed to push Docker image");
                }

                Log.Logger.Information("Docker image pushed");

                return Results.Ok("Successfully pushed image");
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Push error");
                return Results.Problem($"Push error: {ex.Message}");
            }
        });
    }
    public record GoogleCloudConfiguration(string Region, string ProjectId, string RepositoryName);

    public static GoogleCloudConfiguration GetGoogleCloudConfiguration(IConfiguration configuration)
    {
        return new GoogleCloudConfiguration(
            configuration["GoogleCloud:Region"] ?? throw new InvalidOperationException("GoogleCloud:Region not found"),
            configuration["GoogleCloud:ProjectId"] ?? throw new InvalidOperationException("GoogleCloud:ProjectId not found"),
            configuration["GoogleCloud:RepositoryName"] ?? throw new InvalidOperationException("GoogleCloud:RepositoryName not found")
        );
    }

    private async Task AuthenticateDockerRegistryAsync(string region)
    {
        var process = new Process
        {
            StartInfo = {
                FileName = "gcloud",
                Arguments = $"auth configure-docker {region}-docker.pkg.dev",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };
        Log.Logger.Information("Authenticating Docker registry");
        process.Start();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new AuthenticationException("Docker authentication failed");
        }

        Log.Logger.Information("Docker registry authenticated");
    }

    private async Task<bool> RunDockerTagAsync(string localImage, string registryPath)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"tag {localImage}:latest {registryPath}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processInfo };
        process.Start();
        await process.WaitForExitAsync();

        return process.ExitCode == 0;
    }

    private async Task<bool> RunDockerPushAsync(string registryPath)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"push {registryPath}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processInfo };

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                outputBuilder.AppendLine(e.Data);
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                errorBuilder.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            Console.WriteLine($"Push error: {errorBuilder}");
            return false;
        }

        return true;
    }
}