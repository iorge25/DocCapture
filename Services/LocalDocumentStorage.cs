namespace DocCapture.Services
{
    public class LocalDocumentStorage : IDocumentStorage
    {
        private static readonly string[] AllowedExtensions =
            { ".jpg", ".jpeg", ".png", ".pdf", ".tif", ".tiff" };

        private const long MaxBytes = 20 * 1024 * 1024;

        private readonly IWebHostEnvironment _env;
        private readonly ILogger<LocalDocumentStorage> _logger;

        public LocalDocumentStorage(IWebHostEnvironment env, ILogger<LocalDocumentStorage> logger)
        {
            _env = env;
            _logger = logger;
        }

        public async Task<string> SaveAsync(IFormFile file, int batchId, CancellationToken ct = default)
        {
            if (file is null || file.Length == 0)
                throw new ArgumentException("File is empty.", nameof(file));

            if (file.Length > MaxBytes)
                throw new ArgumentException($"File exceeds the {MaxBytes / 1024 / 1024} MB limit.");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
                throw new ArgumentException($"File type {ext} is not allowed.");

            var relativeDir = Path.Combine("uploads", batchId.ToString());
            var absoluteDir = Path.Combine(_env.WebRootPath, relativeDir);
            Directory.CreateDirectory(absoluteDir);

            var fileName = $"{Guid.NewGuid():N}{ext}";
            var absolutePath = Path.Combine(absoluteDir, fileName);

            await using (var stream = new FileStream(absolutePath, FileMode.Create))
            {
                await file.CopyToAsync(stream, ct);
            }

            var relativePath = Path.Combine(relativeDir, fileName).Replace('\\', '/');
            _logger.LogInformation("Saved {Original} to {Path}", file.FileName, relativePath);
            return relativePath;
        }

        public Task DeleteAsync(string storagePath, CancellationToken ct = default)
        {
            var absolutePath = Path.Combine(_env.WebRootPath, storagePath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(absolutePath))
                File.Delete(absolutePath);
            return Task.CompletedTask;
        }

        public string GetPublicUrl(string storagePath) => "/" + storagePath.TrimStart('/');
    }
}