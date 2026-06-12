using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models;

public class Document
{
    [Key]
    public int DocumentId { get; set; }

    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required]
    public DocumentCategory Category { get; set; } = DocumentCategory.Other;

    /// <summary>
    /// Optional comma-separated free-form tags (e.g., "quarterly,finance").
    /// Stored as a single string per the data-model decision (training simplicity).
    /// </summary>
    [MaxLength(500)]
    public string? Tags { get; set; }

    public int? ProjectId { get; set; }

    public int? TaskId { get; set; }

    /// <summary>Original filename shown to users; never used in file system paths.</summary>
    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Path relative to the upload root (AppData/uploads/).
    /// Format: {userId}/{projectId|"personal"}/{guid}.{ext}
    /// This same path pattern is used as an Azure Blob name for future migration.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string StoredFilePath { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    /// <summary>MIME type, e.g. "application/pdf". Max 255 chars to accommodate Office MIME types.</summary>
    [Required]
    [MaxLength(255)]
    public string MimeType { get; set; } = string.Empty;

    [Required]
    public int UploadedByUserId { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Optional note recorded when the file is replaced with a new version.</summary>
    [MaxLength(500)]
    public string? VersionNote { get; set; }

    // Navigation properties
    [ForeignKey("UploadedByUserId")]
    public virtual User UploadedByUser { get; set; } = null!;

    [ForeignKey("ProjectId")]
    public virtual Project? Project { get; set; }

    [ForeignKey("TaskId")]
    public virtual TaskItem? Task { get; set; }

    public virtual ICollection<DocumentShare> DocumentShares { get; set; } = new List<DocumentShare>();
}

/// <summary>Document categories available when uploading or editing a document.</summary>
public enum DocumentCategory
{
    ProjectDocuments,
    TeamResources,
    PersonalFiles,
    Reports,
    Presentations,
    Other
}
