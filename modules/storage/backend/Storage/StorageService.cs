using Minio;
using Minio.DataModel.Args;

namespace Storage;

public interface IStorageService
{
    Task<string> UploadAsync(string fileName, Stream content, CancellationToken cancellationToken = default);
    Task<Stream> DownloadAsync(string fileName, CancellationToken cancellationToken = default);
    Task DeleteAsync(string fileName, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string fileName, CancellationToken cancellationToken = default);

    Task<IEnumerable<StorageItem>> ListAsync(string prefix = "", bool recursive = true,
        CancellationToken cancellationToken = default);

    Task<string> GetPresignedUrlAsync(string fileName, int expiryMinutes,
        CancellationToken cancellationToken = default);
}

public sealed record StorageItem(string Name, ulong Size, DateTime? LastModified);

public class StorageService : IStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly string _bucketName;

    public StorageService(IConfiguration configuration)
    {
        var endpoint = configuration["MINIO_ENDPOINT"] ??
                       throw new ArgumentException("MINIO_ENDPOINT");
        var accessKey = configuration["MINIO_ACCESS_KEY"] ??
                        throw new ArgumentException("MINIO_ACCESS_KEY");
        var secretKey = configuration["MINIO_SECRET_KEY"] ??
                        throw new ArgumentException("MINIO_SECRET_KEY");

        _minioClient = new MinioClient()
            .WithEndpoint(endpoint)
            .WithCredentials(accessKey, secretKey)
            .WithSSL(false)
            .Build();

        _bucketName = configuration["MINIO_BUCKET"] ??
                      throw new ArgumentException("MINIO_BUCKET");
    }

    public async Task<string> UploadAsync(string fileName, Stream content,
        CancellationToken cancellationToken = default)
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
        var responseStream = new MemoryStream();

        var getObjectArgs = new GetObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(fileName)
            .WithCallbackStream(stream =>
            {
                stream.CopyTo(responseStream);
            });

        await _minioClient.GetObjectAsync(getObjectArgs, cancellationToken);
        return responseStream;
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

    public async Task<IEnumerable<StorageItem>> ListAsync(string prefix = "", bool recursive = true,
        CancellationToken cancellationToken = default)
    {
        var listObjectsArgs = new ListObjectsArgs()
            .WithBucket(_bucketName)
            .WithPrefix(prefix)
            .WithRecursive(recursive);

        var items = new List<StorageItem>();
        var list = _minioClient.ListObjectsEnumAsync(listObjectsArgs, cancellationToken);

        await foreach (var item in list)
        {
            items.Add(new StorageItem(item.Key, item.Size, item.LastModifiedDateTime));
        }

        return items;
    }

    public async Task<string> GetPresignedUrlAsync(string fileName, int expiryMinutes,
        CancellationToken cancellationToken = default)
    {
        var presignedGetObjectArgs = new PresignedGetObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(fileName)
            .WithExpiry((int)Math.Round(TimeSpan.FromMinutes(expiryMinutes).TotalSeconds));

        return await _minioClient.PresignedGetObjectAsync(presignedGetObjectArgs);
    }
}