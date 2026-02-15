# Memory Optimization & Storage Refactor Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Reduce server RAM usage from ~18 GiB to ~4-5 GiB by optimizing Docker configurations, replacing MinIO with filesystem storage, and removing unnecessary services.

**Architecture:** Replace the MinIO-backed `StorageService` with a `FileSystemStorageService` (active) while also providing a `GarageStorageService` as a lightweight S3-compatible alternative. Disable the OpenTelemetry/Aspire dashboard in production. Optimize PostgreSQL and .NET GC memory settings.

**Tech Stack:** .NET 9, Docker Compose, Garage (S3-compatible), local filesystem storage

---

### Task 1: Optimize Dockerfile.backend GC settings

**Files:**
- Modify: `Dockerfile.backend`

**Step 1: Update GC environment variables**

Replace the 3 GC env vars in `Dockerfile.backend` (lines 26-28):

```dockerfile
# Configure .NET to respect container memory limits and optimize GC for containers
ENV DOTNET_GCHeapHardLimit=0x20000000
ENV DOTNET_gcServer=0
ENV DOTNET_GCConserveMemory=9
```

Changes:
- `DOTNET_GCHeapHardLimit`: `0x80000000` (2 GiB) -> `0x20000000` (512 MB)
- `DOTNET_gcServer`: `1` (Server GC) -> `0` (Workstation GC)
- `DOTNET_GCConserveMemory`: unchanged at `9`

**Step 2: Build to verify no syntax errors**

Run: `cd /Users/milo/milodev/gits/web/backend && dotnet build`
Expected: Build succeeds (Dockerfile changes don't affect local build, but verify backend still compiles)

**Step 3: Commit**

```bash
git add Dockerfile.backend
git commit -m "perf: reduce .NET GC heap to 512MB and switch to workstation GC"
```

---

### Task 2: Optimize PostgreSQL docker-compose

**Files:**
- Modify: `modules/database/docker-compose.yml`

**Step 1: Add memory limit and reduce shm_size**

Replace the full file content with:

```yaml
services:
  db:
    image: postgres:15
    shm_size: 256m
    ports:
      - "5432:5432"
    environment:
      - POSTGRES_DB=myapp
      - POSTGRES_USER=postgres
      - POSTGRES_PASSWORD=019661c0-7b26-76de-9dfc-c4ce956cdca0
    command:
      - "postgres"
      - "-c"
      - "shared_buffers=256MB"
      - "-c"
      - "effective_cache_size=1GB"
      - "-c"
      - "work_mem=4MB"
      - "-c"
      - "maintenance_work_mem=128MB"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    restart: always
    deploy:
      resources:
        limits:
          memory: 4G
        reservations:
          memory: 512M

volumes:
  postgres_data:
```

Changes:
- `shm_size`: `2gb` -> `256m`
- Added `command` with explicit PostgreSQL tuning params
- Added `deploy.resources.limits.memory: 4G`

**Step 2: Commit**

```bash
git add modules/database/docker-compose.yml
git commit -m "perf: add 4GB memory limit to PostgreSQL and tune shared_buffers"
```

---

### Task 3: Disable OpenTelemetry module in production

The OpenTelemetry feature reads a required `OtelExporterOtlpEndpoint` config value. If it's missing, the feature will crash. The safest approach: disable the feature via `IsEnabled` checking for the config value, and remove the Aspire dashboard container from production compose.

**Files:**
- Modify: `modules/openTelemetry/backend/OpenTelemetry/OpenTelemetryFeature.cs`
- Modify: `modules/openTelemetry/docker-compose.yml`

**Step 1: Make OpenTelemetryFeature check for configuration before enabling**

Replace the full content of `OpenTelemetryFeature.cs`:

```csharp
using Core.Extensions;
using Core.Feature;
using Core.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace OpenTelemetryModule;

public class OpenTelemetryFeature : IFeature
{
    private const string serviceName = "base";

    public bool IsEnabled => Environment.GetEnvironmentVariable("OTEL_ENABLED") == "true";

    void IFeature.ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation()
                .AddOtlpExporter(
                    options => options.Endpoint = new Uri(configuration["OtelExporterOtlpEndpoint"].ThrowIfNullOrWhiteSpace("OTEL_EXPORTER_OTLP_ENDPOINT configuration key is required"))
                ))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter(
                    options => options.Endpoint = new Uri(configuration["OtelExporterOtlpEndpoint"].ThrowIfNullOrWhiteSpace("OTEL_EXPORTER_OTLP_ENDPOINT configuration key is required"))
                ));
    }

    void IFeature.ConfigureBuilder(WebApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetry(options =>
        {
            options
                .SetResourceBuilder(ResourceBuilder.CreateDefault()
                                                   .AddService(serviceName))
                .AddConsoleExporter();
        });
    }
}
```

The only change: added `public bool IsEnabled => Environment.GetEnvironmentVariable("OTEL_ENABLED") == "true";`. In production, don't set `OTEL_ENABLED=true`, so the feature and its dashboard container are both skipped.

**Step 2: Add memory limit to Aspire dashboard (for when it IS enabled)**

Replace `modules/openTelemetry/docker-compose.yml`:

```yaml
services:
  openTelemetry:
    image: mcr.microsoft.com/dotnet/aspire-dashboard:latest
    ports:
      - "18888:18888"
      - "18889:18889"
    deploy:
      resources:
        limits:
          memory: 256M
```

**Step 3: Update docker-compose.watch.yml - remove openTelemetry dependency**

In `docker-compose.watch.yml`, the backend `depends_on` list includes `openTelemetry`. Remove it since the feature is now opt-in. The `depends_on` should only contain `db` and `minio` (minio will be removed later too).

**Step 4: Build to verify**

Run: `cd /Users/milo/milodev/gits/web/backend && dotnet build`
Expected: Build succeeds

**Step 5: Commit**

```bash
git add modules/openTelemetry/backend/OpenTelemetry/OpenTelemetryFeature.cs modules/openTelemetry/docker-compose.yml docker-compose.watch.yml
git commit -m "perf: make OpenTelemetry opt-in via OTEL_ENABLED env var"
```

---

### Task 4: Implement FileSystemStorageService

**Files:**
- Create: `modules/storage/backend/Storage/FileSystemStorageService.cs`
- Create: `modules/storage/backend/Storage/Options/FileSystemStorageOptions.cs`

**Step 1: Create FileSystemStorageOptions**

Create `modules/storage/backend/Storage/Options/FileSystemStorageOptions.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace Storage.Options;

public sealed class FileSystemStorageOptions
{
    [Required]
    public required string BasePath { get; init; }
}
```

**Step 2: Create FileSystemStorageService**

Create `modules/storage/backend/Storage/FileSystemStorageService.cs`:

```csharp
using Microsoft.Extensions.Options;
using Serilog;
using Storage.Options;

namespace Storage;

public class FileSystemStorageService : IStorageService
{
    private readonly string _basePath;

    public FileSystemStorageService(IOptions<FileSystemStorageOptions> options)
    {
        _basePath = options.Value.BasePath;
        Directory.CreateDirectory(_basePath);

        Log.Information("FileSystemStorageService initialized with base path: {BasePath}", _basePath);
    }

    private string GetFullPath(string fileName)
    {
        var sanitized = fileName.Replace("..", "").TrimStart('/');
        return Path.Combine(_basePath, sanitized);
    }

    public async Task UploadAsync(string fileName, Stream content, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(fileName);
        var directory = Path.GetDirectoryName(fullPath);
        if (directory is not null)
            Directory.CreateDirectory(directory);

        await using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
        content.Position = 0;
        await content.CopyToAsync(fileStream, cancellationToken);
    }

    public async Task<Stream?> DownloadAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(fileName);
        if (!File.Exists(fullPath))
            return null;

        var memoryStream = new MemoryStream();
        await using var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        await fileStream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;
        return memoryStream;
    }

    public Task DeleteAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(fileName);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string fileName, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(File.Exists(GetFullPath(fileName)));
    }

    public Task<IEnumerable<StorageItem>> ListAsync(string prefix = "", bool recursive = true,
        CancellationToken cancellationToken = default)
    {
        var searchPath = string.IsNullOrEmpty(prefix) ? _basePath : Path.Combine(_basePath, prefix);

        if (!Directory.Exists(searchPath))
            return Task.FromResult(Enumerable.Empty<StorageItem>());

        var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var files = Directory.GetFiles(searchPath, "*", option);

        var items = files.Select(f =>
        {
            var info = new FileInfo(f);
            var relativePath = Path.GetRelativePath(_basePath, f).Replace('\\', '/');
            return new StorageItem(relativePath, (ulong)info.Length, info.LastWriteTimeUtc);
        });

        return Task.FromResult(items);
    }

    public Task<string> GetPresignedUrlAsync(string fileName, int expiryMinutes,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException(
            "Presigned URLs are not supported by FileSystemStorageService. Use an S3-compatible backend (Garage) for this feature.");
    }
}
```

**Step 3: Build to verify**

Run: `cd /Users/milo/milodev/gits/web/backend && dotnet build`
Expected: Build succeeds

**Step 4: Commit**

```bash
git add modules/storage/backend/Storage/FileSystemStorageService.cs modules/storage/backend/Storage/Options/FileSystemStorageOptions.cs
git commit -m "feat: add FileSystemStorageService as lightweight storage backend"
```

---

### Task 5: Implement GarageStorageService

Garage is a lightweight S3-compatible storage engine. Since it speaks S3, the existing MinIO .NET SDK works with it. We keep the `StorageService` class but rename it to `GarageStorageService` to make intent clear, and point it at Garage instead of MinIO.

**Files:**
- Create: `modules/storage/backend/Storage/GarageStorageService.cs`
- Create: `modules/storage/backend/Storage/Options/GarageStorageOptions.cs`

**Step 1: Create GarageStorageOptions**

Create `modules/storage/backend/Storage/Options/GarageStorageOptions.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace Storage.Options;

public sealed class GarageStorageOptions
{
    [Required]
    public required string Endpoint { get; init; }

    [Required]
    public required string AccessKey { get; init; }

    [Required]
    public required string SecretKey { get; init; }

    [Required]
    public required string Bucket { get; init; }
}
```

**Step 2: Create GarageStorageService**

Create `modules/storage/backend/Storage/GarageStorageService.cs`:

```csharp
using System.Net;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Serilog;
using Storage.Options;

namespace Storage;

public class GarageStorageService : IStorageService
{
    private readonly IMinioClient _client;
    private readonly string _bucketName;

    public GarageStorageService(IOptions<GarageStorageOptions> options)
    {
        Log.Information("Creating GarageStorageService with endpoint: {Endpoint}", options.Value.Endpoint);

        _client = new MinioClient()
            .WithEndpoint(options.Value.Endpoint)
            .WithCredentials(options.Value.AccessKey, options.Value.SecretKey)
            .WithSSL(false)
            .Build();

        _bucketName = options.Value.Bucket;
    }

    public async Task UploadAsync(string fileName, Stream content, CancellationToken cancellationToken = default)
    {
        var bucketExists = await _client.BucketExistsAsync(new BucketExistsArgs().WithBucket(_bucketName), cancellationToken);
        if (!bucketExists)
        {
            Log.Information("Bucket {Bucket} does not exist, creating it", _bucketName);
            await _client.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucketName), cancellationToken);
        }

        var putObjectArgs = new PutObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(fileName)
            .WithStreamData(content)
            .WithObjectSize(content.Length)
            .WithContentType("application/octet-stream");

        var response = await _client.PutObjectAsync(putObjectArgs, cancellationToken);
        if (response.ResponseStatusCode != HttpStatusCode.OK)
        {
            throw new Exception("Failed to upload file: " + response.ResponseContent);
        }
    }

    public async Task<Stream?> DownloadAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var responseStream = new MemoryStream();
        var getObjectArgs = new GetObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(fileName)
            .WithCallbackStream(stream => stream.CopyTo(responseStream));

        try
        {
            await _client.GetObjectAsync(getObjectArgs, cancellationToken);
            responseStream.Position = 0;
            return responseStream;
        }
        catch
        {
            return null;
        }
    }

    public async Task DeleteAsync(string fileName, CancellationToken cancellationToken = default)
    {
        await _client.RemoveObjectAsync(
            new RemoveObjectArgs().WithBucket(_bucketName).WithObject(fileName), cancellationToken);
    }

    public async Task<bool> ExistsAsync(string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            await _client.StatObjectAsync(
                new StatObjectArgs().WithBucket(_bucketName).WithObject(fileName), cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IEnumerable<StorageItem>> ListAsync(string prefix = "", bool recursive = true,
        CancellationToken cancellationToken = default)
    {
        var listObjectsArgs = new ListObjectsArgs()
            .WithBucket(_bucketName)
            .WithPrefix(prefix)
            .WithRecursive(recursive);

        var items = new List<StorageItem>();
        await foreach (var item in _client.ListObjectsEnumAsync(listObjectsArgs, cancellationToken))
        {
            items.Add(new StorageItem(item.Key, item.Size, item.LastModifiedDateTime));
        }
        return items;
    }

    public async Task<string> GetPresignedUrlAsync(string fileName, int expiryMinutes,
        CancellationToken cancellationToken = default)
    {
        return await _client.PresignedGetObjectAsync(
            new PresignedGetObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(fileName)
                .WithExpiry((int)Math.Round(TimeSpan.FromMinutes(expiryMinutes).TotalSeconds)));
    }
}
```

**Step 3: Build to verify**

Run: `cd /Users/milo/milodev/gits/web/backend && dotnet build`
Expected: Build succeeds

**Step 4: Commit**

```bash
git add modules/storage/backend/Storage/GarageStorageService.cs modules/storage/backend/Storage/Options/GarageStorageOptions.cs
git commit -m "feat: add GarageStorageService as lightweight S3-compatible storage backend"
```

---

### Task 6: Update StorageFeature to use FileSystemStorageService by default

**Files:**
- Modify: `modules/storage/backend/Storage/StorageFeature.cs`
- Modify: `modules/storage/backend/Storage/appsettings.json`
- Modify: `modules/storage/backend/Storage/appsettings.Development.json`

**Step 1: Update StorageFeature to select backend via config**

Replace the full content of `StorageFeature.cs`:

```csharp
using Core.Extensions;
using Core.Feature;
using Microsoft.Extensions.DependencyInjection;
using Storage.Options;

namespace Storage;

public class StorageFeature : IFeature
{
    void IFeature.ConfigureServices(IServiceCollection services)
    {
        var storageProvider = Environment.GetEnvironmentVariable("STORAGE__PROVIDER") ?? "filesystem";

        switch (storageProvider.ToLowerInvariant())
        {
            case "garage":
                services.AddRequiredConfigurationOptions<GarageStorageOptions>("GARAGE");
                services.AddSingleton<IStorageService, GarageStorageService>();
                break;
            case "filesystem":
            default:
                services.AddRequiredConfigurationOptions<FileSystemStorageOptions>("STORAGE");
                services.AddSingleton<IStorageService, FileSystemStorageService>();
                break;
        }
    }
}
```

**Step 2: Update appsettings.json (production defaults)**

Replace `modules/storage/backend/Storage/appsettings.json`:

```json
{
    "STORAGE__PROVIDER": "filesystem",
    "STORAGE__BASE_PATH": "/app/storage-data"
}
```

**Step 3: Update appsettings.Development.json**

Replace `modules/storage/backend/Storage/appsettings.Development.json`:

```json
{
    "STORAGE__PROVIDER": "filesystem",
    "STORAGE__BASE_PATH": "./storage-data"
}
```

**Step 4: Build to verify**

Run: `cd /Users/milo/milodev/gits/web/backend && dotnet build`
Expected: Build succeeds

**Step 5: Commit**

```bash
git add modules/storage/backend/Storage/StorageFeature.cs modules/storage/backend/Storage/appsettings.json modules/storage/backend/Storage/appsettings.Development.json
git commit -m "feat: switch storage to FileSystemStorageService by default, configurable via STORAGE__PROVIDER"
```

---

### Task 7: Update Docker Compose files for new storage setup

**Files:**
- Modify: `modules/storage/docker-compose.yml` (Garage for dev/optional S3 use)
- Modify: `modules/storage/compozerr.json`
- Modify: `docker-compose.yml` (production - add storage volume to backend)
- Modify: `docker-compose.watch.yml` (dev - remove minio dependency, add storage volume)

**Step 1: Replace MinIO with Garage in storage module docker-compose**

Replace `modules/storage/docker-compose.yml`:

```yaml
services:
  garage:
    image: dxflrs/garage:v1.0.1
    ports:
      - "1238:3900"
      - "1237:3902"
    volumes:
      - garage-data:/var/lib/garage/data
      - garage-meta:/var/lib/garage/meta
    environment:
      - GARAGE_ALLOW_WORLD_READABLE_SECRETS=true
    command: server
    restart: always
    deploy:
      resources:
        limits:
          memory: 256M

volumes:
  garage-data:
  garage-meta:
```

Note: Garage needs post-start configuration (layout, keys, buckets). This is documented in the README (Task 9).

**Step 2: Update compozerr.json**

Replace `modules/storage/compozerr.json`:

```json
{
    "type": "module",
    "name": "storage",
    "backend": {
        "defaultEnvProperties": {
            "STORAGE__PROVIDER": "filesystem",
            "STORAGE__BASE_PATH": "/app/storage-data"
        }
    }
}
```

Removed: `port`, `start`, `end`, `dockerComposeFile`, `readyMessage` - these are no longer needed since filesystem storage has no external dependency. The Garage docker-compose is available for optional use.

**Step 3: Update production docker-compose.yml - add storage volume to backend**

Replace `docker-compose.yml`:

```yaml
services:
  frontend:
    build:
      context: .
      dockerfile: Dockerfile.frontend
    ports:
      - "1234:5173"
    restart: always
    deploy:
      resources:
        limits:
          memory: 512M
        reservations:
          memory: 256M

  backend:
    build:
      context: .
      dockerfile: Dockerfile.backend
    ports:
      - "1235:5000"
    restart: always
    volumes:
      - private_keys_data:/app/privateKeys
      - storage_data:/app/storage-data
    deploy:
      resources:
        limits:
          memory: 2G
        reservations:
          memory: 512M

volumes:
  private_keys_data:
  storage_data:
```

Changes: Added `storage_data:/app/storage-data` volume mount to backend.

**Step 4: Update docker-compose.watch.yml - remove minio and openTelemetry**

In `docker-compose.watch.yml`, update the backend service:
- Remove `minio` and `openTelemetry` from `depends_on`
- Remove `Minio__Endpoint` and `OtelExporterOtlpEndpoint` from environment
- Add `storage_data` volume
- Add `STORAGE__PROVIDER=filesystem` and `STORAGE__BASE_PATH=/app/storage-data` to environment

**Step 5: Commit**

```bash
git add modules/storage/docker-compose.yml modules/storage/compozerr.json docker-compose.yml docker-compose.watch.yml
git commit -m "refactor: replace MinIO with filesystem storage in Docker configs, add Garage as optional S3 backend"
```

---

### Task 8: Clean up old MinIO references

**Files:**
- Modify: `modules/storage/backend/Storage/Storage.csproj` - keep Minio NuGet (needed for GarageStorageService)
- Delete or keep: `modules/storage/backend/Storage/StorageService.cs` - old MinIO implementation
- Delete or keep: `modules/storage/backend/Storage/Options/MinioOptions.cs` - old MinIO options

**Step 1: Delete old StorageService.cs (replaced by GarageStorageService)**

The old `StorageService.cs` contains both the `IStorageService` interface and the `StorageService` class. We need to extract the interface first.

Create `modules/storage/backend/Storage/IStorageService.cs`:

```csharp
namespace Storage;

public interface IStorageService
{
    Task UploadAsync(string fileName, Stream content, CancellationToken cancellationToken = default);
    Task<Stream?> DownloadAsync(string fileName, CancellationToken cancellationToken = default);
    Task DeleteAsync(string fileName, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string fileName, CancellationToken cancellationToken = default);

    Task<IEnumerable<StorageItem>> ListAsync(string prefix = "", bool recursive = true,
        CancellationToken cancellationToken = default);

    Task<string> GetPresignedUrlAsync(string fileName, int expiryMinutes,
        CancellationToken cancellationToken = default);
}

public sealed record StorageItem(string Name, ulong Size, DateTime? LastModified);
```

Then delete `StorageService.cs` and `Options/MinioOptions.cs`.

**Step 2: Build to verify nothing is broken**

Run: `cd /Users/milo/milodev/gits/web/backend && dotnet build`
Expected: Build succeeds

**Step 3: Commit**

```bash
git add modules/storage/backend/Storage/IStorageService.cs
git rm modules/storage/backend/Storage/StorageService.cs modules/storage/backend/Storage/Options/MinioOptions.cs
git commit -m "refactor: extract IStorageService to own file, remove old MinIO StorageService"
```

---

### Task 9: Add storage module README

**Files:**
- Create: `modules/storage/README.md`

**Step 1: Write comprehensive README**

Create `modules/storage/README.md` documenting:
- Overview of the storage module
- Available backends (FileSystem, Garage)
- Configuration for each backend
- How to switch between backends
- Garage setup instructions (layout, keys, buckets)
- Environment variables reference
- Migration guide from MinIO

Content:

```markdown
# Storage Module

Object storage abstraction for the application. Provides a pluggable `IStorageService` interface with multiple backend implementations.

## Backends

### FileSystem (Default)

Stores files directly on the local filesystem. Zero external dependencies, zero additional RAM usage. Ideal for single-server deployments.

**Configuration:**

| Variable | Description | Default |
|----------|-------------|---------|
| `STORAGE__PROVIDER` | Set to `filesystem` | `filesystem` |
| `STORAGE__BASE_PATH` | Directory path for file storage | `/app/storage-data` |

In Docker, mount a volume to the base path to persist data:

```yaml
volumes:
  - storage_data:/app/storage-data
```

**Limitations:**
- No presigned URL support (throws `NotSupportedException`)
- Single-server only (no replication)

### Garage (S3-Compatible)

[Garage](https://garagehq.deuxfleurs.fr/) is a lightweight, self-hosted S3-compatible storage engine written in Rust. Uses ~30-60 MB RAM vs MinIO's 2-4 GB.

**Configuration:**

| Variable | Description | Example |
|----------|-------------|---------|
| `STORAGE__PROVIDER` | Set to `garage` | `garage` |
| `GARAGE__ENDPOINT` | Garage S3 API endpoint | `garage:3900` |
| `GARAGE__ACCESS_KEY` | Garage API access key | (from `garage key create`) |
| `GARAGE__SECRET_KEY` | Garage API secret key | (from `garage key create`) |
| `GARAGE__BUCKET` | Bucket name | `default-bucket` |

**Docker Compose setup:**

The `docker-compose.yml` in this module starts a Garage instance. After first start, you need to configure it:

```bash
# 1. Start Garage
docker compose up -d

# 2. Get the node ID
docker compose exec garage /garage node id -q

# 3. Configure the layout (replace NODE_ID)
docker compose exec garage /garage layout assign -z dc1 -c 1G NODE_ID

# 4. Apply the layout
docker compose exec garage /garage layout apply --version 1

# 5. Create an API key
docker compose exec garage /garage key create app-key

# 6. Create a bucket
docker compose exec garage /garage bucket create default-bucket

# 7. Grant access
docker compose exec garage /garage bucket allow --read --write --owner default-bucket --key app-key
```

Use the access key and secret key from step 5 in your environment variables.

**Garage config file:**

Garage needs a config file at `/etc/garage.toml` inside the container. For simple single-node setups, mount one:

```toml
metadata_dir = "/var/lib/garage/meta"
data_dir = "/var/lib/garage/data"
db_engine = "sqlite"
replication_factor = 1

[s3_api]
s3_region = "garage"
api_bind_addr = "[::]:3900"

[s3_web]
bind_addr = "[::]:3902"

[admin]
api_bind_addr = "[::]:3903"

[rpc]
secret = "$(openssl rand -hex 32)"
bind_addr = "[::]:3901"
```

## Switching Backends

Change the `STORAGE__PROVIDER` environment variable:

```env
# Use local filesystem (default)
STORAGE__PROVIDER=filesystem

# Use Garage S3
STORAGE__PROVIDER=garage
```

No code changes needed. The `StorageFeature` automatically selects the correct implementation.

## Interface

All backends implement `IStorageService`:

```csharp
public interface IStorageService
{
    Task UploadAsync(string fileName, Stream content, CancellationToken ct);
    Task<Stream?> DownloadAsync(string fileName, CancellationToken ct);
    Task DeleteAsync(string fileName, CancellationToken ct);
    Task<bool> ExistsAsync(string fileName, CancellationToken ct);
    Task<IEnumerable<StorageItem>> ListAsync(string prefix, bool recursive, CancellationToken ct);
    Task<string> GetPresignedUrlAsync(string fileName, int expiryMinutes, CancellationToken ct);
}
```

## Current Usage

The storage module is currently used for deployment log persistence:
- `deployment-logs/{deploymentId}.log` - stored and retrieved during deployments

## Migration from MinIO

If migrating existing data from MinIO:

1. Export files from MinIO: `mc cp --recursive myminio/default-bucket/ ./exported-data/`
2. Copy to the filesystem storage path: `cp -r ./exported-data/* /app/storage-data/`
3. Update environment: `STORAGE__PROVIDER=filesystem`
4. Remove MinIO container
```

**Step 2: Commit**

```bash
git add modules/storage/README.md
git commit -m "docs: add storage module README with backend configuration guide"
```

---

### Task 10: Verify full build

**Step 1: Clean and rebuild entire solution**

Run: `cd /Users/milo/milodev/gits/web/backend && dotnet clean && dotnet build`
Expected: Build succeeds with 0 errors

**Step 2: Run tests**

Run: `cd /Users/milo/milodev/gits/web && dotnet test`
Expected: All existing tests pass

**Step 3: Final commit (if any fixups needed)**

---

## Summary of Expected Memory Savings

| Service | Before | After |
|---------|--------|-------|
| .NET Backend | ~1.5-2 GiB | ~500 MB |
| PostgreSQL | ~4-6 GiB (unbounded) | ~1-2 GiB (4 GiB limit) |
| MinIO | ~2-4 GiB | 0 (removed) |
| Aspire Dashboard | ~500 MB | 0 (disabled) |
| Frontend | ~200 MB | ~200 MB (unchanged) |
| **Total** | **~18 GiB** | **~3-4 GiB** |
