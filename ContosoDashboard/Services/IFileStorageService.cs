namespace ContosoDashboard.Services;

/// <summary>
/// Abstraction for file blob storage. Enables future migration from local disk to Azure Blob Storage
/// by swapping implementations in DI without changing business logic or UI.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Stores a file stream and returns the relative path where it was saved.
    /// Path format: {userId}/{projectId|"personal"}/{Guid}.{extension}
    /// This same path pattern works as an Azure Blob name for migration.
    /// </summary>
    Task<string> UploadAsync(int userId, int? projectId, Stream content, string extension);

    /// <summary>Deletes the file at the given relative path.</summary>
    Task DeleteAsync(string storedRelativePath);

    /// <summary>Returns the absolute filesystem path for the given relative stored path.</summary>
    string GetAbsolutePath(string storedRelativePath);
}
