namespace AvitoBackend.Services.Storage;

public interface IFileStorageService
{
    Task<string> UploadFileAsync(string bucketName, string objectName, Stream fileStream);
}