using System.Data;

namespace Infrastructure.Common
{
    public interface IFileRepository
    {
        Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);
        Task<Stream> DownloadAsync(string fileUri, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(string fileUri, CancellationToken cancellationToken = default);
    }
}
