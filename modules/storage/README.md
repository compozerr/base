# Storage Service

A microservice for file management using Docker with MinIO object storage and a web UI for browsing files.

## Features

- Upload files 
- Download files
- Delete files
- List all files
- Check if files exist
- Generate shareable links with expiry times
- Web-based file browser (similar to Google Drive)
- Persistent storage through Docker volumes

## Setup and Run

### Using Docker Compose

1. Navigate to the storage module directory:
   ```bash
   cd modules/storage
   ```

2. Build and start the services:
   ```bash
   docker-compose up -d
   ```

3. Access the services:
   - MinIO Web UI (file browser): http://localhost:9001

### Storage Information

Files are stored in MinIO, an S3-compatible object storage system. The data is persisted in a Docker volume named `minio-data`, so files remain available even when containers are stopped.

## API Usage

### In Your Applications

To use the Storage Service in your application:

1. Inject the `IStorageClient` interface into your services

```csharp
public class MyService
{
    private readonly IStorageClient _storageClient;

    public MyService(IStorageClient storageClient)
    {
        _storageClient = storageClient;
    }

    // Upload a file
    public async Task<string> UploadFileAsync(string fileName, Stream content)
    {
        return await _storageClient.UploadAsync(fileName, content);
    }
    
    // Download a file
    public async Task<Stream> DownloadFileAsync(string fileName)
    {
        return await _storageClient.DownloadAsync(fileName);
    }
    
    // List all files
    public async Task<IEnumerable<StorageItem>> ListFilesAsync()
    {
        return await _storageClient.ListAsync();
    }
    
    // Check if a file exists
    public async Task<bool> FileExistsAsync(string fileName)
    {
        return await _storageClient.ExistsAsync(fileName);
    }
    
    // Get a shareable link
    public async Task<string> GetShareableLinkAsync(string fileName, int expiryMinutes = 60)
    {
        return await _storageClient.GetPresignedUrlAsync(fileName, expiryMinutes);
    }
}
```

### API Endpoints

- `GET /storages` - Lists all files with metadata
- `GET /storages/{filename}` - Downloads a specific file
- `POST /storages/{filename}` - Uploads a file
- `DELETE /storages/{filename}` - Deletes a file
- `GET /storages/exists/{filename}` - Checks if a file exists
- `GET /storages/share/{filename}?expiryMinutes=60` - Generates a shareable link with optional expiry time

## Web UI for File Browsing

MinIO provides a web-based file browser that's similar to Google Drive:

1. Access the MinIO console at http://localhost:9001
2. Login with:
   - Username: minioadmin
   - Password: minioadmin
3. Navigate to the `storage-storage` bucket to browse, upload, download, and manage files through the web interface 