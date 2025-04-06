using System.Net;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Serilog;
using Storage.Options;
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

public class StorageService : IStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly string _bucketName;

    public StorageService(IOptions<MinioOptions> options)
    {
        Log.ForContext(nameof(options), options)
           .Information("Creating storage service");

        var endpoint = options.Value.Endpoint;
        var accessKey = options.Value.AccessKey;
        var secretKey = options.Value.SecretKey;

        _minioClient = new MinioClient()
            .WithEndpoint(endpoint)
            .WithCredentials(accessKey, secretKey)
            .WithSSL(false)
            .Build();

        Log.ForContext(nameof(options), options)
           .Information("Storage service created");

        _bucketName = options.Value.Bucket;
    }

    public async Task UploadAsync(string fileName, Stream content,
        CancellationToken cancellationToken = default)
    {
        var bucketExists = await _minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(_bucketName), cancellationToken);
        if (!bucketExists)
        {
            Log.ForContext(nameof(_bucketName), _bucketName)
               .Information("Bucket does not exist, creating it");

            await _minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucketName), cancellationToken);
        }

        var putObjectArgs = new PutObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(fileName)
            .WithStreamData(content)
            .WithObjectSize(content.Length)
            .WithContentType("application/octet-stream");

        Log.ForContext(nameof(putObjectArgs), putObjectArgs, true)
           .Information("Uploading file");

        try
        {
            var response = await _minioClient.PutObjectAsync(putObjectArgs, cancellationToken);
            if (response.ResponseStatusCode != HttpStatusCode.OK)
            {
                Log.ForContext(nameof(response), response, true)
                   .Error("Failed to upload file");

                throw new Exception("Failed to upload file " + response.ResponseContent);
            }
        }
        catch (Exception ex)
        {
            Log.ForContext(nameof(fileName), fileName)
                .ForContext(nameof(ex), ex)
                .Error("Failed to upload file");

            throw;
        }
    }
    public async Task<Stream?> DownloadAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var responseStream = new MemoryStream();

        var getObjectArgs = new GetObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(fileName)
            .WithCallbackStream(stream =>
            {
                stream.CopyTo(responseStream);
            });

        try
        {
            await _minioClient.GetObjectAsync(getObjectArgs, cancellationToken);
            return responseStream;
        }
        catch
        {
            return null;
        }
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