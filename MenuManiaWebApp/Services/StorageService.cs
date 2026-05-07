using Google.Cloud.Storage.V1;

namespace MenuManiaCloudPlatform.Services
{
    public class StorageService
    {
        private readonly ILogger<StorageService> _logger;
        private readonly string _bucketName;
        private readonly StorageClient _storageClient;

        public StorageService(ILogger<StorageService> logger, IConfiguration config)
        {
            _logger = logger;

            _bucketName = config["GoogleCloud:BucketName"]
                ?? throw new InvalidOperationException("Bucket name is missing.");

            _storageClient = StorageClient.Create();
        }

        public async Task<string> UploadFileAsync(IFormFile file, string restaurantId)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File is empty.");
            }

            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };

            if (!allowedExtensions.Contains(extension))
            {
                throw new ArgumentException("Invalid file type.");
            }

            string fileName = $"restaurants/{restaurantId}/{Guid.NewGuid()}{extension}";

            using var stream = file.OpenReadStream();

            await _storageClient.UploadObjectAsync(
                _bucketName,
                fileName,
                file.ContentType,
                stream
            );

            _logger.LogInformation("File uploaded: {FileName}", fileName);

            return fileName;
        }

        public string GetBucketName()
        {
            return _bucketName;
        }
    }
}