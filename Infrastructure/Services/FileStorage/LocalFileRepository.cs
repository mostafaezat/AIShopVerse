using Infrastructure.Common;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services.FileStorage
{
    public class LocalFileRepository : IFileRepository
    {
        private readonly FileStorageSettings _settings;
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private const long MaxFileSizeBytes = 5 * 1024 * 1024;

        public LocalFileRepository(IOptions<FileStorageSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
        {
            if (fileStream == null || fileStream.Length == 0)
                throw new InvalidOperationException("Invalid file.");

            if (fileStream.Length > MaxFileSizeBytes)
                throw new InvalidOperationException("File exceeds the maximum allowed size.");

            var extension = Path.GetExtension(fileName);
            if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException("Unsupported file type.");

            var newFileName = $"{Guid.NewGuid():N}{extension}";
            var relativeFolder = "products";
            var relativePath = $"{relativeFolder}/{newFileName}";

            var storageRoot = Path.GetFullPath(_settings.StorageRoot);
            Directory.CreateDirectory(Path.Combine(storageRoot, relativeFolder));

            var absolutePath = Path.Combine(storageRoot, relativeFolder, newFileName);

            await using (var output = new FileStream(absolutePath, FileMode.Create, FileAccess.Write))
            {
                await fileStream.CopyToAsync(output, cancellationToken);
            }

            return $"{_settings.BaseUrl}/{relativePath}";
        }

        public Task<Stream> DownloadAsync(string fileUri, CancellationToken cancellationToken = default)
        {
            var absolutePath = ResolveAbsolutePath(fileUri);
            if (!File.Exists(absolutePath))
                throw new FileNotFoundException("File not found.", fileUri);

            return Task.FromResult<Stream>(File.OpenRead(absolutePath));
        }

        public Task<bool> DeleteAsync(string fileUri, CancellationToken cancellationToken = default)
        {
            var absolutePath = ResolveAbsolutePath(fileUri);
            if (File.Exists(absolutePath))
            {
                File.Delete(absolutePath);
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        private string ResolveAbsolutePath(string fileUri)
        {
            var storageRoot = Path.GetFullPath(_settings.StorageRoot);
            var relative = fileUri.Replace(_settings.BaseUrl, "").TrimStart('/');
            var absolutePath = Path.GetFullPath(Path.Combine(storageRoot, relative));

            if (!absolutePath.StartsWith(storageRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Invalid file path.");

            return absolutePath;
        }
    }
}
