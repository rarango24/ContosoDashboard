---
description: "Task list template for feature implementation"
---

# Tasks: Document Upload and Management

**Input**: Design documents from `specs/001-document-upload-management/`  
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/document-api.md ✅, quickstart.md ✅

**Tests**: Not requested — no test tasks generated.

**Organization**: Tasks grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1–US5)
- All paths are relative to the repository root

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Add NuGet packages and configure application infrastructure needed by all stories.

- [X] T001 Add `Microsoft.AspNetCore.Mvc` controllers support and register `DocumentsController` route in `ContosoDashboard/Program.cs` (add `builder.Services.AddControllersWithViews()` and `app.MapControllers()`)
- [X] T002 [P] Register `IFileStorageService` → `LocalFileStorageService` and `IFileScanner` → `StubFileScanner` in `ContosoDashboard/Program.cs` DI container
- [X] T003 [P] Register `DocumentService` in `ContosoDashboard/Program.cs` DI container
- [X] T004 [P] Create `ContosoDashboard/AppData/uploads/` directory and add a `.gitkeep` file; add `ContosoDashboard/AppData/uploads/` to `.gitignore`
- [X] T005 [P] Configure `HubOptions` in `ContosoDashboard/Program.cs` to set `MaximumReceiveMessageSize` to 33,554,432 bytes (32 MB) for Blazor Server SignalR

**Checkpoint**: Program.cs updated; DI registrations in place; upload directory exists.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core models, DB schema, and service abstractions that every user story depends on. No story work begins until this phase is complete.

- [X] T006 Create `ContosoDashboard/Models/Document.cs` — `Document` entity per data-model.md (all fields: DocumentId, Title, Description, Category, Tags, ProjectId?, TaskId?, FileName, StoredFilePath, FileSizeBytes, MimeType, UploadedByUserId, UploadedAt, UpdatedAt, VersionNote?, IsDeleted; navigation properties)
- [X] T007 [P] Create `ContosoDashboard/Models/DocumentShare.cs` — `DocumentShare` entity per data-model.md (DocumentShareId, DocumentId, SharedByUserId, SharedWithUserId, SharedAt; navigation properties)
- [X] T008 [P] Add `DocumentCategory` enum to `ContosoDashboard/Models/Document.cs` (ProjectDocuments, TeamResources, PersonalFiles, Reports, Presentations, Other)
- [X] T009 Modify `ContosoDashboard/Data/ApplicationDbContext.cs` — add `DbSet<Document> Documents` and `DbSet<DocumentShare> DocumentShares`; configure relationships (Document→User, Document→Project?, Document→TaskItem?, DocumentShare→Document, DocumentShare→SharedByUser, DocumentShare→SharedWithUser); add all indexes from data-model.md; add `OnDelete(DeleteBehavior.Restrict)` for user FK references
- [X] T010 Create EF Core migration `AddDocumentEntities` by running `dotnet ef migrations add AddDocumentEntities --project ContosoDashboard`
- [X] T011 [P] Create `ContosoDashboard/Services/IFileStorageService.cs` — interface with methods: `Task<string> UploadAsync(int userId, int? projectId, Stream content, string extension)`, `Task DeleteAsync(string storedRelativePath)`, `string GetAbsolutePath(string storedRelativePath)`
- [X] T012 [P] Create `ContosoDashboard/Services/LocalFileStorageService.cs` — implement `IFileStorageService`; generate path as `{userId}/{projectId ?? "personal"}/{Guid.NewGuid()}.{extension}`; write stream to `ContosoDashboard/AppData/uploads/{path}`; implement `DeleteAsync` and `GetAbsolutePath`; inject `IWebHostEnvironment` to resolve root
- [X] T013 [P] Create `ContosoDashboard/Services/IFileScanner.cs` — interface: `Task<bool> ScanAsync(Stream content)` (true = clean); create `StubFileScanner.cs` in same folder that always returns `true`; add a code comment documenting the production `ClamAvScanner` path

**Checkpoint**: Entities, migrations, and service interfaces ready. `dotnet build` must pass.

---

## Phase 3: User Story 1 — Upload a Document (Priority: P1) 🎯 MVP

**Goal**: Authenticated users can select a file, provide metadata, and upload it securely. The file is validated, scanned, stored on disk, and metadata is saved to the database.

**Independent Test**: Use quickstart.md Scenario 1 — upload a valid PDF, verify it appears in My Documents; upload invalid type/size/missing title, verify rejections.

- [X] T014 [US1] Create `ContosoDashboard/Services/DocumentService.cs` with method `UploadDocumentAsync(int uploadedByUserId, string title, string? description, DocumentCategory category, string? tags, int? projectId, int? taskId, IBrowserFile file)` — validate file extension against whitelist, validate size ≤ 25 MB, call `IFileScanner.ScanAsync`, call `IFileStorageService.UploadAsync` to store file, save `Document` record via `ApplicationDbContext`, return created `Document`
- [X] T015 [P] [US1] Add `GetAllowedMimeTypes()` and `GetAllowedExtensions()` static helpers to `ContosoDashboard/Services/DocumentService.cs` — whitelist from data-model.md validation rules
- [X] T016 [US1] Create `ContosoDashboard/Pages/DocumentUpload.razor` — `[Authorize]` page at route `/documents/upload`; `InputFile` component (multi-file disabled for simplicity, single file per upload); Title (required), Description (optional), Category dropdown (required, bound to `DocumentCategory` enum), Project dropdown (optional, loaded from `ProjectService`), Tags (optional text input); submit calls `DocumentService.UploadDocumentAsync`; show progress indicator using `IBrowserFile` stream; show success or error message after upload
- [X] T017 [US1] Add "Documents" link to `ContosoDashboard/Shared/NavMenu.razor` pointing to `/documents`

**Checkpoint**: US1 fully functional. User can upload a valid document; invalid files are rejected with clear errors. Verify with quickstart.md Scenario 1.

---

## Phase 4: User Story 2 — Browse and Find My Documents (Priority: P2)

**Goal**: Users can view all their documents in a sortable, filterable list and search across title, description, tags, uploader name, and project.

**Independent Test**: Use quickstart.md Scenario 2 — upload 3+ docs with varied categories, verify sort/filter/search return correct subsets within 2 seconds.

- [X] T018 [US2] Add to `ContosoDashboard/Services/DocumentService.cs`: method `GetMyDocumentsAsync(int userId, string? searchTerm, DocumentCategory? categoryFilter, int? projectFilter, DateTime? fromDate, DateTime? toDate, string sortBy, bool sortDesc)` — queries `Document` records for `UploadedByUserId == userId` and `IsDeleted == false`; applies filters and sort; returns `IEnumerable<Document>` with navigation properties loaded
- [X] T019 [US2] Create `ContosoDashboard/Pages/Documents.razor` — `[Authorize]` page at route `/documents`; shows tabbed view with "My Documents" tab (default); table columns: Title, Category, Upload Date, File Size, Project; sort on column header click; filter controls (Category dropdown, Project dropdown, Date Range pickers); search input field; results update on filter/search change; each row has Download and Preview action links
- [X] T020 [P] [US2] Add `FormatFileSize(long bytes)` display helper in `ContosoDashboard/Pages/Documents.razor` (or a shared utility) to render file size as KB/MB

**Checkpoint**: US2 fully functional. My Documents view shows, sorts, filters, and searches correctly. Verify with quickstart.md Scenario 2.

---

## Phase 5: User Story 3 — Download and Preview (Priority: P3)

**Goal**: Authorized users can download any document they have access to, and preview PDFs/images inline in the browser. Unauthorized access returns 403.

**Independent Test**: Use quickstart.md Scenario 3 — download a PDF; preview PDF and image inline; verify unauthenticated and unauthorized requests are blocked.

- [ ] T021 [US3] Add to `ContosoDashboard/Services/DocumentService.cs`: method `CanAccessDocumentAsync(int requestingUserId, int documentId)` — enforces access rules from contracts/document-api.md (owner, project member, share recipient, Project Manager, Administrator); returns bool
- [ ] T022 [US3] Add to `ContosoDashboard/Services/DocumentService.cs`: method `GetDocumentByIdAsync(int documentId)` — returns `Document` or null
- [ ] T023 [US3] Create `ContosoDashboard/Controllers/DocumentsController.cs` — MVC controller with `[Authorize]`; action `Download(int id, bool inline = false)`: call `DocumentService.GetDocumentByIdAsync`, then `CanAccessDocumentAsync` (return 403 if false), then `IFileStorageService.GetAbsolutePath`, validate physical file exists (return 404 if not), return `PhysicalFile(path, mimeType, fileDownloadName)` with `Content-Disposition: attachment` or `inline` based on parameter

**Checkpoint**: US3 fully functional. Download and preview work for authorized users; 401/403 returned for unauthorized access. Verify with quickstart.md Scenario 3.

---

## Phase 6: User Story 4 — Manage Document Metadata and Delete (Priority: P4)

**Goal**: Document owners (and Project Managers for project docs) can edit metadata, replace the file with a new version, and permanently delete documents.

**Independent Test**: Use quickstart.md Scenario 4 — edit title/category/tags; replace file; delete; verify unauthorized edit/delete is blocked.

- [ ] T024 [US4] Add to `ContosoDashboard/Services/DocumentService.cs`: method `UpdateDocumentMetadataAsync(int requestingUserId, int documentId, string title, string? description, DocumentCategory category, string? tags)` — verifies caller is owner or ProjectManager of the document's project; updates fields; sets `UpdatedAt`
- [ ] T025 [P] [US4] Add to `ContosoDashboard/Services/DocumentService.cs`: method `ReplaceDocumentFileAsync(int requestingUserId, int documentId, IBrowserFile newFile, string? versionNote)` — validates new file (type, size, scan); calls `IFileStorageService.UploadAsync` for new file; calls `IFileStorageService.DeleteAsync` for old `StoredFilePath`; updates `Document.StoredFilePath`, `FileName`, `FileSizeBytes`, `MimeType`, `VersionNote`, `UpdatedAt`
- [ ] T026 [P] [US4] Add to `ContosoDashboard/Services/DocumentService.cs`: method `DeleteDocumentAsync(int requestingUserId, int documentId)` — verifies caller is owner, ProjectManager of document's project, or Administrator; calls `IFileStorageService.DeleteAsync`; removes `Document` record from DB (hard delete)
- [ ] T027 [US4] Create `ContosoDashboard/Pages/DocumentDetails.razor` — `[Authorize]` page at route `/documents/{id:int}`; shows document metadata; edit form (Title, Description, Category, Tags) visible only to owner/PM — calls `DocumentService.UpdateDocumentMetadataAsync` on save; "Replace File" section with `InputFile` — calls `DocumentService.ReplaceDocumentFileAsync`; "Delete" button with confirmation modal — calls `DocumentService.DeleteDocumentAsync`; Download and Preview links (to `DocumentsController`); authorization-aware: hide edit/delete controls when caller lacks rights

**Checkpoint**: US4 fully functional. Edit, replace, and delete work for authorized users; unauthorized users cannot see or trigger those actions. Verify with quickstart.md Scenario 4.

---

## Phase 7: User Story 5 — Share a Document and Receive Notifications (Priority: P5)

**Goal**: Document owners share documents with specific users. Recipients get an in-app notification and see the document in their "Shared with Me" section.

**Independent Test**: Use quickstart.md Scenario 5 — share a document; verify recipient notification and Shared with Me visibility; verify non-recipients cannot see the document.

- [ ] T028 [US5] Add to `ContosoDashboard/Services/DocumentService.cs`: method `ShareDocumentAsync(int ownerUserId, int documentId, int recipientUserId)` — verifies caller owns the document; creates `DocumentShare` record; calls `NotificationService` to create in-app notification for recipient with message "{{ownerDisplayName}} shared a document with you: {{documentTitle}}"
- [ ] T029 [P] [US5] Add to `ContosoDashboard/Services/DocumentService.cs`: method `GetSharedWithMeAsync(int userId)` — returns `Document` records where a `DocumentShare` exists with `SharedWithUserId == userId` and document `IsDeleted == false`
- [ ] T030 [US5] Add "Shared with Me" tab to `ContosoDashboard/Pages/Documents.razor` — lists documents returned by `DocumentService.GetSharedWithMeAsync`; columns: Title, Category, Shared By, Share Date, file size; each row has Download link
- [ ] T031 [US5] Add Share UI to `ContosoDashboard/Pages/DocumentDetails.razor` — "Share" button (visible to document owner only); modal with user search/select dropdown (loaded from `UserService.GetAllUsersAsync`); on confirm calls `DocumentService.ShareDocumentAsync`; shows success/error message

**Checkpoint**: US5 fully functional. Share creates notification; shared doc appears in recipient's Shared with Me. Verify with quickstart.md Scenario 5.

---

## Phase 8: Polish and Cross-Cutting Concerns

**Purpose**: Dashboard and project integrations, notifications for project document uploads, and final wiring.

- [ ] T032 Create `ContosoDashboard/Shared/RecentDocumentsWidget.razor` — Blazor component; calls `DocumentService.GetRecentDocumentsAsync(int userId, int count = 5)`; displays last 5 documents uploaded by the current user (title, upload date, file size, download link); add `GetRecentDocumentsAsync` method to `DocumentService`
- [ ] T033 [P] Modify `ContosoDashboard/Pages/Index.razor` — inject `DocumentService`; embed `<RecentDocumentsWidget />` in dashboard summary area
- [ ] T034 [P] Modify `ContosoDashboard/Pages/ProjectDetails.razor` — add "Documents" tab; inject `DocumentService`; call `GetProjectDocumentsAsync(int projectId, int requestingUserId)` (add method to `DocumentService` — returns documents associated with the project that the user can access); show document list with Download link; show Upload button for ProjectManager/TeamLead roles that navigates to `/documents/upload` with `?projectId={id}` query param pre-filled; add `GetProjectDocumentsAsync` method to `DocumentService`
- [ ] T035 [P] Update `DocumentService.UploadDocumentAsync` to trigger in-app notifications (via `NotificationService`) to all project members when a document is uploaded to a project (skip for personal uploads where `ProjectId` is null)
- [ ] T036 [P] Update `ContosoDashboard/Pages/DocumentUpload.razor` to read `?projectId` query param and pre-select the project in the Project dropdown when navigated from a project page

**Checkpoint**: Dashboard widget, project documents tab, and notifications all working. Verify with quickstart.md Scenario 6.

---

## Dependencies (Story Completion Order)

```
Phase 1 (Setup)
    │
    ▼
Phase 2 (Foundational) ← MUST complete before any story
    │
    ├──► Phase 3 (US1: Upload) ← MVP
    │         │
    │         ▼
    │    Phase 4 (US2: Browse) ← depends on US1 for test data
    │         │
    │         ▼
    │    Phase 5 (US3: Download) ← depends on US1 (files must exist)
    │         │
    │         ▼
    │    Phase 6 (US4: Edit/Delete) ← depends on US1 (docs must exist)
    │         │
    │         ▼
    │    Phase 7 (US5: Share) ← depends on US1 (docs must exist)
    │         │
    │         ▼
    └──► Phase 8 (Polish) ← integrations, depends on US1–US5
```

**Notes on parallelism**:
- Within each Phase, tasks marked `[P]` can run in parallel (they touch different files).
- US2, US3, US4, US5 each require only US1 to be complete (documents must exist to browse, download, edit, or share). They are otherwise independent of each other and can be developed in parallel by different developers.
- Phase 8 tasks T033, T034, T035, T036 are independently parallelizable (different files).

---

## Parallel Execution Examples

**Two-developer split after Phase 2**:

| Developer A | Developer B |
|-------------|-------------|
| Phase 3 (US1 — Upload) | waits for T006–T013 |
| Phase 4 (US2 — Browse) | Phase 5 (US3 — Download) after US1 complete |
| Phase 6 (US4 — Edit/Delete) | Phase 7 (US5 — Share) |
| Phase 8 T032, T033, T035 | Phase 8 T034, T036 |

---

## Implementation Strategy

**MVP scope**: Phases 1–3 (US1 only). After completing Phases 1–3, the application can upload, store, and list documents. This is independently demonstrable and delivers the core business value.

**Incremental delivery**:
1. MVP — Phase 1 + 2 + 3: Upload works, document appears in My Documents
2. Add Browse (Phase 4): Sort, filter, search
3. Add Download/Preview (Phase 5): Files retrievable securely
4. Add Edit/Delete (Phase 6): Lifecycle management
5. Add Share (Phase 7): Collaboration
6. Add Polish (Phase 8): Dashboard widget, project integration, notifications
