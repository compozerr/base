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
