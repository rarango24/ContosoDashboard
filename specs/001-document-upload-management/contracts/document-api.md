# Contract: Document File Serving Endpoint

**Feature**: 001-document-upload-management  
**Date**: 2026-06-11  
**Type**: HTTP controller endpoint (MVC, ASP.NET Core 8)

This contract defines the single HTTP endpoint exposed by `DocumentsController` for secure file download and inline preview. All other document operations (upload, metadata edit, delete, share) are handled server-side within Blazor Server components and do not expose HTTP endpoints.

---

## Authorization

All routes require an authenticated user (`[Authorize]`). The controller additionally enforces per-document access rights at the service layer before serving any bytes.

**Access rules** (checked by `DocumentService.CanAccessDocumentAsync`):

| Caller role | Permitted if… |
|-------------|---------------|
| Employee | Document was uploaded by the caller, OR caller is a member of the document's project, OR document has been shared with the caller |
| TeamLead | Same as Employee, plus documents uploaded by direct reports |
| ProjectManager | Same as Employee, plus any document associated with one of their projects |
| Administrator | Any document |

---

## GET `/documents/download/{id}`

Download a document as a file attachment.

### Path Parameters

| Name | Type | Description |
|------|------|-------------|
| `id` | `int` | Document ID |

### Query Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `inline` | `bool` | `false` | When `true`, sets `Content-Disposition: inline` for browser preview; when `false`, sets `attachment` |

### Success Response — `200 OK`

| Header | Value | Notes |
|--------|-------|-------|
| `Content-Type` | MIME type stored on Document record | e.g., `application/pdf`, `image/png` |
| `Content-Disposition` | `attachment; filename="{FileName}"` or `inline` | Determined by `inline` query param |
| Body | Raw file bytes | Streamed from `AppData/uploads/` |

### Error Responses

| Status | Condition |
|--------|-----------|
| `401 Unauthorized` | User is not authenticated |
| `403 Forbidden` | User is authenticated but does not have access to this document |
| `404 Not Found` | Document record does not exist, or physical file is missing |

### Notes

- The file is read from the path stored in `Document.StoredFilePath` (relative to the configured upload root).
- The `FileName` used in `Content-Disposition` is the original user-provided filename (`Document.FileName`), not the GUID-based stored path.
- Preview (`inline=true`) is only meaningful for PDFs and images; for other types the browser will fall back to download regardless of the header.

---

## IFileStorageService Interface Contract

Although not an HTTP API, this service interface is the primary contract for the file storage abstraction (see research.md, Topic 3 for rationale).

```
IFileStorageService
  UploadAsync(userId, projectId?, stream, extension) → storedRelativePath
  DeleteAsync(storedRelativePath) → void
  GetAbsolutePath(storedRelativePath) → string
```

**Path generation rule** (enforced by `LocalFileStorageService.UploadAsync`):

```
{userId}/{projectId or "personal"}/{Guid.NewGuid()}.{extension}
```

This path is stored verbatim in `Document.StoredFilePath` and is the same key that would be used as an Azure Blob name in a future migration.

---

## IFileScanner Interface Contract

```
IFileScanner
  ScanAsync(stream) → bool   // true = clean, false = threat detected
```

Training implementation: `StubFileScanner` always returns `true`.  
Production replacement: `ClamAvScanner` using `nClam` NuGet package.
