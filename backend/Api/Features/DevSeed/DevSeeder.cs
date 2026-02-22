using Api.Abstractions;
using Api.Data;
using Auth.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Api.Features.DevSeed;

public static class DevSeeder
{
    /// <summary>
    /// Ensures the dev location and server exist in the database.
    /// Called at startup so CLI commands (compozerr new, migrate) pass validation.
    /// </summary>
    public static async Task EnsureInfrastructureAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var apiDb = scope.ServiceProvider.GetRequiredService<ApiDbContext>();
        await EnsureInfrastructureAsync(apiDb);
    }

    private static async Task<(Location location, Server server)> EnsureInfrastructureAsync(ApiDbContext apiDb)
    {
        var location = await apiDb.Locations.FirstOrDefaultAsync(l => l.IsoCountryCode == "deu");
        if (location is null)
        {
            location = new Location
            {
                IsoCountryCode = "deu",
                CreatedAtUtc = DateTime.UtcNow,
            };
            apiDb.Locations.Add(location);
            await apiDb.SaveChangesAsync();
            Log.Information("[DevSeed] Created dev location (deu)");
        }

        var server = await apiDb.Servers.FirstOrDefaultAsync(
            s => s.LocationId == location.Id && s.ServerVisibility == ServerVisibility.Public && s.DeletedAtUtc == null);
        if (server is null)
        {
            var secretId = SecretId.Create(Guid.NewGuid());
            server = new Server
            {
                LocationId = location.Id,
                SecretId = secretId,
                ServerVisibility = ServerVisibility.Public,
                MachineId = "dev-machine-001",
                Ram = "16 GB",
                VCpu = "4c/8t",
                Ip = "192.168.1.100",
                HostName = "dev-host-01",
                ApiDomain = "api.dev-host-01.compozerr.com",
                Usage = new ServerUsage
                {
                    AvgCpuPercentage = 42.5m,
                    AvgRamPercentage = 61.2m,
                    AvgDiskPercentage = 28.7m,
                },
                CreatedAtUtc = DateTime.UtcNow,
            };
            apiDb.Servers.Add(server);

            var secret = new Secret
            {
                Id = secretId,
                ServerId = server.Id,
                Value = "dev-secret-placeholder",
                CreatedAtUtc = DateTime.UtcNow,
            };
            apiDb.Secrets.Add(secret);
            await apiDb.SaveChangesAsync();
            Log.Information("[DevSeed] Created dev server + secret");
        }

        return (location, server);
    }

    public static async Task<object> SeedForUser(ApiDbContext apiDb, UserId userId)
    {
        Log.Information("[DevSeed] Seeding development data for user {UserId}...", userId);

        var (location, server) = await EnsureInfrastructureAsync(apiDb);

        // --- Seed projects ---
        var projects = CreateProjects(userId, location.Id, server.Id);
        apiDb.Projects.AddRange(projects);
        await apiDb.SaveChangesAsync();

        // --- Seed related data ---
        apiDb.Domains.AddRange(CreateDomains(projects));
        apiDb.ProjectServices.AddRange(CreateProjectServices(projects));

        var deployments = CreateDeployments(projects, userId);
        apiDb.Deployments.AddRange(deployments);

        var environments = CreateProjectEnvironments(projects);
        apiDb.ProjectEnvironments.AddRange(environments);
        await apiDb.SaveChangesAsync();

        apiDb.ProjectEnvironmentVariables.AddRange(CreateEnvironmentVariables(environments));

        var usages = CreateProjectUsages(projects);
        apiDb.ProjectUsages.AddRange(usages);
        await apiDb.SaveChangesAsync();

        Log.Information("[DevSeed] Seeded {ProjectCount} projects, {DeploymentCount} deployments, {UsageCount} usage records for user {UserId}",
            projects.Count, deployments.Count, usages.Count, userId);

        return new { message = "Seeded", seeded = true, projects = projects.Count, deployments = deployments.Count, usageRecords = usages.Count };
    }

    private static List<Project> CreateProjects(UserId userId, LocationId locationId, ServerId serverId)
    {
        var now = DateTime.UtcNow;
        return
        [
            new()
            {
                Name = "my-saas-app",
                RepoUri = new Uri("https://github.com/acme/my-saas-app"),
                UserId = userId, LocationId = locationId, ServerId = serverId,
                State = ProjectState.Running, ServerTierId = new ServerTierId("T2"),
                Type = ProjectType.Compozerr, GithubInstallationId = 12345678,
                CreatedAtUtc = now.AddDays(-45),
            },
            new()
            {
                Name = "landing-page",
                RepoUri = new Uri("https://github.com/acme/landing-page"),
                UserId = userId, LocationId = locationId, ServerId = serverId,
                State = ProjectState.Running, ServerTierId = new ServerTierId("T0"),
                Type = ProjectType.Dockerfile, GithubInstallationId = 12345679,
                CreatedAtUtc = now.AddDays(-30),
            },
            new()
            {
                Name = "workflow-automation",
                RepoUri = new Uri("https://github.com/acme/workflow-automation"),
                UserId = userId, LocationId = locationId, ServerId = serverId,
                State = ProjectState.Running, ServerTierId = new ServerTierId("T1"),
                Type = ProjectType.N8n,
                CreatedAtUtc = now.AddDays(-20),
            },
            new()
            {
                Name = "microservices-stack",
                RepoUri = new Uri("https://github.com/acme/microservices-stack"),
                UserId = userId, LocationId = locationId, ServerId = serverId,
                State = ProjectState.Stopped, ServerTierId = new ServerTierId("T3"),
                Type = ProjectType.DockerCompose, GithubInstallationId = 12345680,
                CreatedAtUtc = now.AddDays(-60),
            },
            new()
            {
                Name = "api-gateway",
                RepoUri = new Uri("https://github.com/acme/api-gateway"),
                UserId = userId, LocationId = locationId, ServerId = serverId,
                State = ProjectState.Starting, ServerTierId = new ServerTierId("T1"),
                Type = ProjectType.Dockerfile, GithubInstallationId = 12345681,
                CreatedAtUtc = now.AddDays(-10),
            },
            new()
            {
                Name = "docs-site",
                RepoUri = new Uri("https://github.com/acme/docs-site"),
                UserId = userId, LocationId = locationId, ServerId = serverId,
                State = ProjectState.Running, ServerTierId = new ServerTierId("T0"),
                Type = ProjectType.Dockerfile,
                CreatedAtUtc = now.AddDays(-15),
            },
        ];
    }

    private static List<Domain> CreateDomains(List<Project> projects)
    {
        var now = DateTime.UtcNow;
        return
        [
            // my-saas-app: internal + external primary
            new InternalDomain { ProjectId = projects[0].Id, Subdomain = "my-saas-app", ServiceName = "frontend", Port = "3000", Protocol = Protocol.Http, IsPrimary = false, CreatedAtUtc = now.AddDays(-44) },
            new ExternalDomain { ProjectId = projects[0].Id, Value = "app.acme.dev", ServiceName = "frontend", Port = "3000", Protocol = Protocol.Http, IsPrimary = true, IsVerified = true, HealthCheckPassed = true, VerificationCheckCount = 3, LastVerificationCheckUtc = now.AddHours(-2), CreatedAtUtc = now.AddDays(-43) },
            new ExternalDomain { ProjectId = projects[0].Id, Value = "api.acme.dev", ServiceName = "backend", Port = "5000", Protocol = Protocol.Http, IsPrimary = false, IsVerified = true, HealthCheckPassed = true, VerificationCheckCount = 5, LastVerificationCheckUtc = now.AddHours(-2), CreatedAtUtc = now.AddDays(-43) },

            // landing-page: internal + external
            new InternalDomain { ProjectId = projects[1].Id, Subdomain = "landing-page", ServiceName = "web", Port = "80", Protocol = Protocol.Http, IsPrimary = false, CreatedAtUtc = now.AddDays(-29) },
            new ExternalDomain { ProjectId = projects[1].Id, Value = "acme.dev", ServiceName = "web", Port = "80", Protocol = Protocol.Http, IsPrimary = true, IsVerified = true, HealthCheckPassed = true, VerificationCheckCount = 10, LastVerificationCheckUtc = now.AddHours(-1), CreatedAtUtc = now.AddDays(-28) },

            // workflow-automation: internal only
            new InternalDomain { ProjectId = projects[2].Id, Subdomain = "n8n-workflows", ServiceName = "n8n", Port = "5678", Protocol = Protocol.Http, IsPrimary = true, CreatedAtUtc = now.AddDays(-19) },

            // microservices-stack: internal + unverified external
            new InternalDomain { ProjectId = projects[3].Id, Subdomain = "microservices", ServiceName = "gateway", Port = "8080", Protocol = Protocol.Http, IsPrimary = false, CreatedAtUtc = now.AddDays(-59) },
            new ExternalDomain { ProjectId = projects[3].Id, Value = "services.acme.dev", ServiceName = "gateway", Port = "8080", Protocol = Protocol.Http, IsPrimary = true, IsVerified = false, HealthCheckPassed = false, VerificationCheckCount = 1, LastVerificationCheckUtc = now.AddDays(-2), NextVerificationCheckUtc = now.AddHours(4), CreatedAtUtc = now.AddDays(-5) },

            // api-gateway: internal
            new InternalDomain { ProjectId = projects[4].Id, Subdomain = "api-gw", ServiceName = "gateway", Port = "8080", Protocol = Protocol.Http, IsPrimary = true, CreatedAtUtc = now.AddDays(-9) },

            // docs-site: internal + external
            new InternalDomain { ProjectId = projects[5].Id, Subdomain = "docs", ServiceName = "web", Port = "3000", Protocol = Protocol.Http, IsPrimary = false, CreatedAtUtc = now.AddDays(-14) },
            new ExternalDomain { ProjectId = projects[5].Id, Value = "docs.acme.dev", ServiceName = "web", Port = "3000", Protocol = Protocol.Http, IsPrimary = true, IsVerified = true, HealthCheckPassed = true, VerificationCheckCount = 7, LastVerificationCheckUtc = now.AddMinutes(-30), CreatedAtUtc = now.AddDays(-13) },
        ];
    }

    private static List<ProjectService> CreateProjectServices(List<Project> projects)
    {
        var now = DateTime.UtcNow;
        return
        [
            new() { ProjectId = projects[0].Id, Name = "frontend", Port = "3000", Protocol = Protocol.Http, CreatedAtUtc = now },
            new() { ProjectId = projects[0].Id, Name = "backend", Port = "5000", Protocol = Protocol.Http, CreatedAtUtc = now },
            new() { ProjectId = projects[1].Id, Name = "web", Port = "80", Protocol = Protocol.Http, CreatedAtUtc = now },
            new() { ProjectId = projects[2].Id, Name = "n8n", Port = "5678", Protocol = Protocol.Http, CreatedAtUtc = now },
            new() { ProjectId = projects[3].Id, Name = "gateway", Port = "8080", Protocol = Protocol.Http, CreatedAtUtc = now },
            new() { ProjectId = projects[3].Id, Name = "api", Port = "3001", Protocol = Protocol.Http, CreatedAtUtc = now },
            new() { ProjectId = projects[3].Id, Name = "worker", Port = "3002", Protocol = Protocol.Http, CreatedAtUtc = now },
            new() { ProjectId = projects[3].Id, Name = "redis", Port = "6379", Protocol = Protocol.Tcp, IsSystem = true, CreatedAtUtc = now },
            new() { ProjectId = projects[4].Id, Name = "gateway", Port = "8080", Protocol = Protocol.Http, CreatedAtUtc = now },
            new() { ProjectId = projects[5].Id, Name = "web", Port = "3000", Protocol = Protocol.Http, CreatedAtUtc = now },
        ];
    }

    private static List<Deployment> CreateDeployments(List<Project> projects, UserId userId)
    {
        var now = DateTime.UtcNow;
        var rng = new Random(42);
        var deployments = new List<Deployment>();

        var commitMessages = new[]
        {
            "feat: add user authentication flow",
            "fix: resolve memory leak in websocket handler",
            "chore: update dependencies to latest versions",
            "feat: implement dark mode toggle",
            "fix: correct timezone handling in scheduler",
            "refactor: extract payment logic into service",
            "feat: add CSV export for reports",
            "fix: handle edge case in pagination",
            "docs: update API documentation",
            "feat: add webhook retry mechanism",
            "fix: resolve race condition in queue consumer",
            "chore: optimize Docker image size",
            "feat: add health check endpoint",
            "fix: sanitize user input in search",
            "feat: implement rate limiting middleware",
        };

        foreach (var project in projects)
        {
            var deploymentCount = rng.Next(3, 8);
            for (var i = 0; i < deploymentCount; i++)
            {
                var daysAgo = rng.Next(0, 30);
                var hoursAgo = rng.Next(0, 24);
                var createdAt = now.AddDays(-daysAgo).AddHours(-hoursAgo);

                var status = i == 0 && project.State == ProjectState.Starting
                    ? DeploymentStatus.Deploying
                    : i == 0
                        ? DeploymentStatus.Completed
                        : (DeploymentStatus)(new[] { 2, 2, 2, 2, 4, 5 }[rng.Next(6)]);

                var hash = $"{rng.Next(0x1000000):x6}{rng.Next(0x10000000):x7}";

                deployments.Add(new Deployment
                {
                    ProjectId = project.Id,
                    UserId = userId,
                    CommitHash = hash[..12],
                    CommitMessage = commitMessages[rng.Next(commitMessages.Length)],
                    CommitAuthor = "Dev User",
                    CommitBranch = i == 0 ? "main" : (new[] { "main", "main", "develop", "feature/update" })[rng.Next(4)],
                    CommitEmail = "dev@compozerr.local",
                    Status = status,
                    BuildDuration = status == DeploymentStatus.Deploying ? null : TimeSpan.FromSeconds(rng.Next(30, 300)),
                    CreatedAtUtc = createdAt,
                });
            }
        }

        return deployments;
    }

    private static List<ProjectEnvironment> CreateProjectEnvironments(List<Project> projects)
    {
        return [.. projects.Select(p => new ProjectEnvironment
        {
            ProjectId = p.Id,
            Branches = ["main"],
            AutoDeploy = true,
            CreatedAtUtc = DateTime.UtcNow,
        })];
    }

    private static List<ProjectEnvironmentVariable> CreateEnvironmentVariables(List<ProjectEnvironment> environments)
    {
        var vars = new List<ProjectEnvironmentVariable>();
        foreach (var env in environments)
        {
            vars.Add(new() { ProjectEnvironmentId = env.Id, SystemType = SystemType.General, Key = "NODE_ENV", Value = "production", CreatedAtUtc = DateTime.UtcNow });
            vars.Add(new() { ProjectEnvironmentId = env.Id, SystemType = SystemType.General, Key = "LOG_LEVEL", Value = "info", CreatedAtUtc = DateTime.UtcNow });
            vars.Add(new() { ProjectEnvironmentId = env.Id, SystemType = SystemType.Backend, Key = "DATABASE_URL", Value = "postgresql://user:pass@db:5432/app", CreatedAtUtc = DateTime.UtcNow });
            vars.Add(new() { ProjectEnvironmentId = env.Id, SystemType = SystemType.Frontend, Key = "VITE_API_URL", Value = "https://api.acme.dev", CreatedAtUtc = DateTime.UtcNow });
        }
        return vars;
    }

    private static List<ProjectUsage> CreateProjectUsages(List<Project> projects)
    {
        var now = DateTime.UtcNow;
        var rng = new Random(123);
        var usages = new List<ProjectUsage>();

        foreach (var project in projects)
        {
            var isRunning = project.State == ProjectState.Running || project.State == ProjectState.Starting;

            for (var i = 0; i < 96; i++)
            {
                var timestamp = now.AddMinutes(-15 * (96 - i));
                var hour = timestamp.Hour;
                var isBusinessHours = hour >= 8 && hour <= 18;
                var baseCpu = isBusinessHours ? 35.0 : 12.0;
                var baseMemory = isBusinessHours ? 1.2 : 0.8;

                var cpuUsage = isRunning ? (decimal)(baseCpu + rng.NextDouble() * 25 + Math.Sin(i * 0.1) * 10) : 0m;
                cpuUsage = Math.Clamp(cpuUsage, 0, 100);

                var memUsage = isRunning ? (decimal)(baseMemory + rng.NextDouble() * 0.5) : 0m;

                var totalMemory = project.ServerTierId.Value switch { "T0" => 0.5m, "T1" => 1m, "T2" => 2m, "T3" => 4m, "T4" => 8m, _ => 1m };
                var cpuCount = project.ServerTierId.Value switch { "T0" => 1, "T1" => 1, "T2" => 2, "T3" => 2, "T4" => 3, _ => 1 };

                usages.Add(new ProjectUsage
                {
                    ProjectId = project.Id,
                    VmId = 100 + projects.IndexOf(project),
                    Name = project.Name,
                    Status = isRunning ? ProjectStatus.Running : ProjectStatus.Stopped,
                    CpuUsagePercentage = Math.Round(cpuUsage, 2),
                    CpuCount = cpuCount,
                    MemoryUsageGb = Math.Round(Math.Min(memUsage, totalMemory), 3),
                    TotalMemoryGb = totalMemory,
                    DiskUsageGb = Math.Round((decimal)(5 + rng.NextDouble() * 15), 2),
                    TotalDiskGb = 50m,
                    NetworkInBytesPerSec = isRunning ? Math.Round((decimal)(rng.NextDouble() * 500000), 0) : 0m,
                    NetworkOutBytesPerSec = isRunning ? Math.Round((decimal)(rng.NextDouble() * 300000), 0) : 0m,
                    DiskReadBytesPerSec = isRunning ? Math.Round((decimal)(rng.NextDouble() * 100000), 0) : 0m,
                    DiskWriteBytesPerSec = isRunning ? Math.Round((decimal)(rng.NextDouble() * 80000), 0) : 0m,
                    CreatedAtUtc = timestamp,
                });
            }
        }

        return usages;
    }
}
