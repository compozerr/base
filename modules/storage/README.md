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
secret = "<generate with: openssl rand -hex 32>"
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
