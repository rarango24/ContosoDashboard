using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using ContosoDashboard.Data;
using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public interface IDocumentService
{
    Task<Document> UploadDocumentAsync(
        int uploadedByUserId,
        string title,
        string? description,
        DocumentCategory category,
        string? tags,
        int? projectId,
        int? taskId,
        IBrowserFile file);

    Task<IEnumerable<Document>> GetMyDocumentsAsync(
        int userId,
        string? searchTerm = null,
        DocumentCategory? categoryFilter = null,
        int? projectFilter = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string sortBy = "UploadedAt",
        bool sortDesc = true);

    Task<Document?> GetDocumentByIdAsync(int documentId);

    Task<bool> CanAccessDocumentAsync(int requestingUserId, int documentId);

    Task<IEnumerable<Document>> GetRecentDocumentsAsync(int userId, int count = 5);
}

public class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorage;
    private readonly IFileScanner _fileScanner;

    // T015: Allowed MIME types and extensions whitelist (data-model.md validation rules)
    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.ms-powerpoint",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        "text/plain",
        "image/jpeg",
        "image/png"
    };

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".jpg", ".jpeg", ".png"
    };

    private const long MaxFileSizeBytes = 25L * 1024 * 1024; // 25 MB

    public DocumentService(
        ApplicationDbContext context,
        IFileStorageService fileStorage,
        IFileScanner fileScanner)
    {
        _context = context;
        _fileStorage = fileStorage;
        _fileScanner = fileScanner;
    }

    public static IReadOnlyCollection<string> GetAllowedMimeTypes() => AllowedMimeTypes;
    public static IReadOnlyCollection<string> GetAllowedExtensions() => AllowedExtensions;

    public async Task<Document> UploadDocumentAsync(
        int uploadedByUserId,
        string title,
        string? description,
        DocumentCategory category,
        string? tags,
        int? projectId,
        int? taskId,
        IBrowserFile file)
    {
        // Validate file extension
        var extension = Path.GetExtension(file.Name);
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException(
                $"File type '{extension}' is not supported. Allowed types: {string.Join(", ", AllowedExtensions)}");
        }

        // Validate file size
        if (file.Size > MaxFileSizeBytes)
        {
            throw new InvalidOperationException(
                $"File size {file.Size:N0} bytes exceeds the 25 MB limit.");
        }

        // Validate MIME type
        if (!AllowedMimeTypes.Contains(file.ContentType))
        {
            throw new InvalidOperationException(
                $"MIME type '{file.ContentType}' is not supported.");
        }

        // Open stream, scan, then store
        // Upload sequence: generate unique path → scan → save to disk → save metadata to DB
        // This prevents orphaned DB records if file save fails.
        await using var stream = file.OpenReadStream(MaxFileSizeBytes);

        var isClean = await _fileScanner.ScanAsync(stream);
        if (!isClean)
        {
            throw new InvalidOperationException(
                "The file was rejected by the security scanner. Please ensure the file is not infected.");
        }

        // Reset stream position after scanning
        stream.Seek(0, SeekOrigin.Begin);

        var storedPath = await _fileStorage.UploadAsync(uploadedByUserId, projectId, stream, extension);

        var document = new Document
        {
            Title = title.Trim(),
            Description = description?.Trim(),
            Category = category,
            Tags = string.IsNullOrWhiteSpace(tags) ? null : tags.Trim(),
            ProjectId = projectId,
            TaskId = taskId,
            FileName = file.Name,
            StoredFilePath = storedPath,
            FileSizeBytes = file.Size,
            MimeType = file.ContentType,
            UploadedByUserId = uploadedByUserId,
            UploadedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        return document;
    }

    public async Task<IEnumerable<Document>> GetMyDocumentsAsync(
        int userId,
        string? searchTerm = null,
        DocumentCategory? categoryFilter = null,
        int? projectFilter = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string sortBy = "UploadedAt",
        bool sortDesc = true)
    {
        var query = _context.Documents
            .Include(d => d.UploadedByUser)
            .Include(d => d.Project)
            .Where(d => d.UploadedByUserId == userId);

        if (categoryFilter.HasValue)
            query = query.Where(d => d.Category == categoryFilter.Value);

        if (projectFilter.HasValue)
            query = query.Where(d => d.ProjectId == projectFilter.Value);

        if (fromDate.HasValue)
            query = query.Where(d => d.UploadedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(d => d.UploadedAt <= toDate.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(d =>
                d.Title.ToLower().Contains(term) ||
                (d.Description != null && d.Description.ToLower().Contains(term)) ||
                (d.Tags != null && d.Tags.ToLower().Contains(term)) ||
                d.UploadedByUser.DisplayName.ToLower().Contains(term) ||
                (d.Project != null && d.Project.Name.ToLower().Contains(term)));
        }

        query = (sortBy, sortDesc) switch
        {
            ("Title", true)      => query.OrderByDescending(d => d.Title),
            ("Title", false)     => query.OrderBy(d => d.Title),
            ("Category", true)   => query.OrderByDescending(d => d.Category),
            ("Category", false)  => query.OrderBy(d => d.Category),
            ("FileSizeBytes", true)  => query.OrderByDescending(d => d.FileSizeBytes),
            ("FileSizeBytes", false) => query.OrderBy(d => d.FileSizeBytes),
            (_, true)            => query.OrderByDescending(d => d.UploadedAt),
            _                    => query.OrderBy(d => d.UploadedAt)
        };

        return await query.ToListAsync();
    }

    public async Task<Document?> GetDocumentByIdAsync(int documentId)
    {
        return await _context.Documents
            .Include(d => d.UploadedByUser)
            .Include(d => d.Project)
            .Include(d => d.DocumentShares)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId);
    }

    public async Task<bool> CanAccessDocumentAsync(int requestingUserId, int documentId)
    {
        var document = await _context.Documents
            .Include(d => d.DocumentShares)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId);

        if (document == null) return false;

        var user = await _context.Users.FindAsync(requestingUserId);
        if (user == null) return false;

        // Administrators can access all documents
        if (user.Role == UserRole.Administrator) return true;

        // Owner can always access their own document
        if (document.UploadedByUserId == requestingUserId) return true;

        // Recipient of a share grant
        if (document.DocumentShares.Any(s => s.SharedWithUserId == requestingUserId)) return true;

        // Project member (including ProjectManager) — if document is associated with a project
        if (document.ProjectId.HasValue)
        {
            var isMember = await _context.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == document.ProjectId.Value && pm.UserId == requestingUserId);
            if (isMember) return true;

            // Project Manager of the project
            var isManager = await _context.Projects
                .AnyAsync(p => p.ProjectId == document.ProjectId.Value && p.ProjectManagerId == requestingUserId);
            if (isManager) return true;
        }

        return false;
    }

    public async Task<IEnumerable<Document>> GetRecentDocumentsAsync(int userId, int count = 5)
    {
        return await _context.Documents
            .Include(d => d.Project)
            .Where(d => d.UploadedByUserId == userId)
            .OrderByDescending(d => d.UploadedAt)
            .Take(count)
            .ToListAsync();
    }
}
