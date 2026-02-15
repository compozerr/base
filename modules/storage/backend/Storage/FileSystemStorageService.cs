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
