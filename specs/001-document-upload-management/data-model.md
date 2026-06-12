# Data Model: Document Upload and Management

**Feature**: 001-document-upload-management  
**Date**: 2026-06-11  
**Source**: spec.md entities + clarifications + research.md decisions

---

## Entities

### Document

Represents an uploaded file and its metadata.

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `DocumentId` | `int` | ✅ | Primary key |
| `Title` | `string` (255) | ✅ | User-provided display name |
| `Description` | `string?` (2000) | ❌ | Optional free-form text |
| `Category` | `DocumentCategory` (enum) | ✅ | See enum below |
| `Tags` | `string?` (500) | ❌ | Comma-separated free-form tags |
| `ProjectId` | `int?` | ❌ | FK → `Project.ProjectId`; null = personal |
| `TaskId` | `int?` | ❌ | FK → `TaskItem.TaskItemId`; null = not task-linked |
| `FileName` | `string` (255) | ✅ | Original filename (display only; never used in path) |
| `StoredFilePath` | `string` (500) | ✅ | Relative path under `AppData/uploads/`; format: `{userId}/{projectOrPersonal}/{guid}.{ext}` |
| `FileSizeBytes` | `long` | ✅ | Bytes; captured at upload |
| `MimeType` | `string` (255) | ✅ | MIME type; e.g., `application/pdf` |
| `UploadedByUserId` | `int` | ✅ | FK → `User.UserId` |
| `UploadedAt` | `DateTime` | ✅ | UTC; set server-side at upload |
| `UpdatedAt` | `DateTime` | ✅ | UTC; updated on metadata edits or file replacement |
| `VersionNote` | `string?` (500) | ❌ | Optional note recorded when file is replaced |
| `IsDeleted` | `bool` | ✅ | Soft-delete flag (false by default); physical file deleted immediately; record retained briefly for FK integrity then removed by cleanup |

**Navigation properties**: `UploadedByUser`, `Project?`, `TaskItem?`, `DocumentShares`

---

### DocumentShare

Represents a share grant — a document owner sharing access with a specific recipient.

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `DocumentShareId` | `int` | ✅ | Primary key |
| `DocumentId` | `int` | ✅ | FK → `Document.DocumentId` |
| `SharedByUserId` | `int` | ✅ | FK → `User.UserId` (the owner/grantor) |
| `SharedWithUserId` | `int` | ✅ | FK → `User.UserId` (the recipient) |
| `SharedAt` | `DateTime` | ✅ | UTC timestamp of share action |

**Navigation properties**: `Document`, `SharedByUser`, `SharedWithUser`

---

### DocumentCategory (Enum)

```
ProjectDocuments
TeamResources
PersonalFiles
Reports
Presentations
Other
```

---

## Relationships

```
User (1) ──── (many) Document          [UploadedByUserId]
Project (0..1) ─── (many) Document     [ProjectId, nullable]
TaskItem (0..1) ─── (many) Document    [TaskId, nullable]
Document (1) ──── (many) DocumentShare [DocumentId]
User (1) ──── (many) DocumentShare     [SharedByUserId]
User (1) ──── (many) DocumentShare     [SharedWithUserId]
```

---

## Validation Rules

| Rule | Detail |
|------|--------|
| Max file size | 26,214,400 bytes (25 MB) enforced at upload before storage |
| Allowed MIME types | `application/pdf`, `application/msword`, `application/vnd.openxmlformats-officedocument.wordprocessingml.document`, `application/vnd.ms-excel`, `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`, `application/vnd.ms-powerpoint`, `application/vnd.openxmlformats-officedocument.presentationml.presentation`, `text/plain`, `image/jpeg`, `image/png` |
| Allowed extensions whitelist | `.pdf`, `.doc`, `.docx`, `.xls`, `.xlsx`, `.ppt`, `.pptx`, `.txt`, `.jpg`, `.jpeg`, `.png` |
| Title | Required; 1–255 characters |
| StoredFilePath uniqueness | Enforced by GUID generation; unique index on column |
| Tags | Optional; stored as comma-separated string; max 500 characters |
| ProjectId | If provided, requesting user must be a member of that project |

---

## EF Core Indexes (new additions to ApplicationDbContext)

| Entity | Column(s) | Reason |
|--------|-----------|--------|
| `Document` | `UploadedByUserId` | My Documents queries |
| `Document` | `ProjectId` | Project documents tab |
| `Document` | `UploadedAt` | Date-range filtering |
| `Document` | `Category` | Category filtering |
| `Document` | `StoredFilePath` | Unique constraint |
| `DocumentShare` | `DocumentId` | Share lookups |
| `DocumentShare` | `SharedWithUserId` | Shared with Me queries |

---

## State Transitions

```
[File selected by user]
        │
        ▼
[Validated: type, size]  ──FAIL──► Error returned to user
        │ PASS
        ▼
[Scanned: IFileScanner]  ──FAIL──► Error returned to user
        │ PASS (stub always passes in training)
        ▼
[Stored: unique path generated → file written to disk → metadata saved to DB]
        │
        ▼
[Active: visible in My Documents / Project Documents]
        │
   ┌────┴─────────┐
   ▼              ▼
[Metadata        [File replaced:
 edited]          old file deleted,
                  new file stored,
                  VersionNote saved]
        │
        ▼
[Deleted: physical file removed → DB record removed]
```
