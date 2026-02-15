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
