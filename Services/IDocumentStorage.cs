namespace DocCapture.Services
{
    public interface IDocumentStorage
    {
        Task<string> SaveAsync(IFormFile file, int batchId, CancellationToken ct = default);
        Task DeleteAsync(string storagePath, CancellationToken ct = default);
        string GetPublicUrl(string storagePath);
    }
}