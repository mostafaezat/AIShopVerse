namespace Infrastructure.Services.FileStorage
{
    public class FileStorageSettings
    {
        public string BaseUrl { get; set; } = "/images";
        public string StorageRoot { get; set; } = "wwwroot/images";
        public bool UseLocalStorage { get; set; } = true;
    }
}
