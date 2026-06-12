namespace ContosoDashboard.Services;

/// <summary>
/// Local filesystem implementation of IFileStorageService.
/// Stores files in ContentRootPath/AppData/uploads/ (outside wwwroot).
///
/// Production migration: Replace with AzureBlobStorageService that uses
/// Azure.Storage.Blobs SDK. The path pattern {userId}/{projectId}/{guid}.{ext}
/// maps directly to Azure Blob names without schema changes.
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    private readonly string _uploadRoot;

    public LocalFileStorageService(IWebHostEnvironment env)
    {
        _uploadRoot = Path.Combine(env.ContentRootPath, "AppData", "uploads");
        Directory.CreateDirectory(_uploadRoot);
    }

    public async Task<string> UploadAsync(int userId, int? projectId, Stream content, string extension)
    {
        var folder = projectId.HasValue ? projectId.Value.ToString() : "personal";
        var relativePath = Path.Combine(userId.ToString(), folder, $"{Guid.NewGuid()}{extension}");
        var absolutePath = Path.Combine(_uploadRoot, relativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);

        using var fileStream = new FileStream(absolutePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(fileStream);

        // Normalize to forward slashes for consistency (and future Azure migration)
        return relativePath.Replace(Path.DirectorySeparatorChar, '/');
    }

    public Task DeleteAsync(string storedRelativePath)
    {
        var absolutePath = GetAbsolutePath(storedRelativePath);
        if (File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
        }
        return Task.CompletedTask;
    }

    public string GetAbsolutePath(string storedRelativePath)
    {
        // Normalize separators before combining to prevent path traversal
        var normalized = storedRelativePath.Replace('/', Path.DirectorySeparatorChar);
        return Path.GetFullPath(Path.Combine(_uploadRoot, normalized));
    }
}
