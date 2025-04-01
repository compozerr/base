using Microsoft.Extensions.Configuration;
using Minio;
using Minio.DataModel.Args;

namespace Storage;

public interface IStorageService
{
    Task<string> UploadAsync(string fileName, Stream content, CancellationToken cancellationToken = default);
    Task<Stream> DownloadAsync(string fileName, CancellationToken cancellationToken = default);
    Task DeleteAsync(string fileName, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string fileName, CancellationToken cancellationToken = default);
    Task<IEnumerable<StorageItem>> ListAsync(string prefix = "", bool recursive = true, CancellationToken cancellationToken = default);
    Task<string> GetPresignedUrlAsync(string fileName, int expiryMinutes, CancellationToken cancellationToken = default);
}

public class StorageService : IStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly string _bucketName;

    public StorageService(IConfiguration configuration)
    {
        var endpoint = configuration["MINIO_ENDPOINT"] ?? throw new ArgumentNullException(nameof(configuration["MINIO_ENDPOINT"]));
        var accessKey = configuration["MINIO_ACCESS_KEY"] ?? throw new ArgumentNullException(nameof(configuration["MINIO_ACCESS_KEY"]));
        var secretKey = configuration["MINIO_SECRET_KEY"] ?? throw new ArgumentNullException(nameof(configuration["MINIO_SECRET_KEY"]));

        _minioClient = new MinioClient()
            .WithEndpoint(endpoint)
            .WithCredentials(accessKey, secretKey)
            .WithSSL(false)
            .Build();

        _bucketName = configuration["MINIO_BUCKET"] ?? throw new ArgumentNullException(nameof(configuration["MINIO_BUCKET"]));
    }

    public async Task<string> UploadAsync(string fileName, Stream content, CancellationToken cancellationToken = default)
    {
        var putObjectArgs = new PutObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(fileName)
            .WithStreamData(content)
            .WithObjectSize(content.Length)
            .WithContentType("application/octet-stream");

        await _minioClient.PutObjectAsync(putObjectArgs, cancellationToken);
        return $"/storages/{fileName}";
    }

    public async Task<Stream> DownloadAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var getObjectArgs = new GetObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(fileName);

        return await _minioClient.GetObjectAsync(getObjectArgs, cancellationToken);
    }

    public async Task DeleteAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var removeObjectArgs = new RemoveObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(fileName);

        await _minioClient.RemoveObjectAsync(removeObjectArgs, cancellationToken);
    }

    public async Task<bool> ExistsAsync(string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            var statObjectArgs = new StatObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(fileName);

            await _minioClient.StatObjectAsync(statObjectArgs, cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IEnumerable<StorageItem>> ListAsync(string prefix = "", bool recursive = true, CancellationToken cancellationToken = default)
    {
        var listObjectsArgs = new ListObjectsArgs()
            .WithBucket(_bucketName)
            .WithPrefix(prefix)
            .WithRecursive(recursive);

        var items = new List<StorageItem>();
        var list = _minioClient.ListObjectsAsync(listObjectsArgs, cancellationToken);

        await foreach (var item in list.WithCancellation(cancellationToken))
        {
            items.Add(new StorageItem
            {
                Name = item.ObjectName,
                Size = item.Size,
                LastModified = item.LastModifiedDateTime
            });
        }

        return items;
    }

    public async Task<string> GetPresignedUrlAsync(string fileName, int expiryMinutes, CancellationToken cancellationToken = default)
    {
        var presignedGetObjectArgs = new PresignedGetObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(fileName)
            .WithExpiry(TimeSpan.FromMinutes(expiryMinutes));

        return await _minioClient.PresignedGetObjectAsync(presignedGetObjectArgs, cancellationToken);
    }
} 