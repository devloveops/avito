using Minio;
using Minio.DataModel.Args;

namespace AvitoBackend.Services.Storage;

public class MinioFileStorageService : IFileStorageService
{
    private readonly IMinioClient _minio;

    public MinioFileStorageService(IConfiguration config)
    {
        _minio = new MinioClient()
            .WithEndpoint(config["MinIO:Endpoint"]!)
            .WithCredentials(config["MinIO:AccessKey"]!, config["MinIO:SecretKey"]!)
            .Build();
    }

    public async Task<string> UploadFileAsync(string bucketName, string objectName, Stream fileStream)
    {
        await _minio.PutObjectAsync(new PutObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithStreamData(fileStream)
            .WithObjectSize(fileStream.Length));
        return $"http://{_minio.Config.Endpoint}/{bucketName}/{objectName}";
    }
}