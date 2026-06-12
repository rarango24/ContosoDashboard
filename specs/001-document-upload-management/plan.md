# Implementation Plan: Document Upload and Management

**Branch**: `001-document-upload-management` | **Date**: 2026-06-11 | **Spec**: [spec.md](spec.md)  
**Input**: Feature specification from `specs/001-document-upload-management/spec.md`

## Summary

Add centralized document upload and management to ContosoDashboard, enabling authenticated employees to upload work-related files (PDF, Office documents, images, text), organize them by category and optional project association, download/preview them in-browser, share them with specific users, and manage (edit metadata, replace, delete) their documents. The implementation uses ASP.NET Core 8.0 Blazor Server with EF Core, local disk storage behind an `IFileStorageService` abstraction (Azure-migration-ready), and an authorized Razor Pages controller endpoint for secure file serving. All business logic lives in a new `DocumentService`; RBAC is enforced at both page and service level.

## Technical Context

**Language/Version**: C# / .NET 8.0  
**Primary Dependencies**: ASP.NET Core 8.0 Blazor Server, Entity Framework Core 8 (SQL Server / LocalDB), Microsoft.AspNetCore.Authentication (existing cookie auth)  
**Storage**: SQL Server LocalDB (metadata) + local filesystem at `ContosoDashboard/AppData/uploads/` (file blobs, outside wwwroot)  
**Testing**: No test project currently configured — manual validation via quickstart.md scenarios  
**Target Platform**: Windows / macOS local dev (offline-capable training environment)  
**Project Type**: Single Blazor Server web application (no separate frontend/backend split)  
**Performance Goals**: Upload ≤ 30 s for 25 MB; document list page load ≤ 2 s for 500 documents; search ≤ 2 s  
**Constraints**: Offline-only (no cloud services); files stored outside `wwwroot`; GUID-based paths; authorized download endpoint required; virus scan = local stub (training)  
**Scale/Scope**: Up to ~500 documents per user; 4 user roles (Employee, TeamLead, ProjectManager, Administrator); 5 user stories

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Spec-Driven Development | ✅ PASS | spec.md written and clarified before any implementation |
| II. Training-Context Simplicity | ✅ PASS | Local disk storage, stub AV scanner, no external services; IFileStorageService abstraction is justified by stakeholder requirement for Azure migration path |
| III. Security-Aware Design | ✅ PASS | Files served via authorized endpoint; IDOR protection at service level; GUID paths prevent traversal; [Authorize] on all document pages |
| IV. Service-Layer Isolation | ✅ PASS | New `DocumentService` carries all business logic; Blazor pages/components call service only |
| V. Incremental, Story-Driven Delivery | ✅ PASS | 5 independently testable user stories (P1–P5) defined; tasks.md will be organized by story |

**Post-Phase 1 re-check**: ✅ All gates still pass — design artifacts introduce no new violations.

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
ContosoDashboard/
├── AppData/
│   └── uploads/                          # File blob storage (outside wwwroot, not web-accessible)
│       └── {userId}/{projectId}/{guid}.{ext}
├── Models/
│   ├── Document.cs                       # NEW — Document entity
│   └── DocumentShare.cs                  # NEW — DocumentShare entity
├── Data/
│   └── ApplicationDbContext.cs           # MODIFIED — add Document, DocumentShare DbSets + seed data
├── Services/
│   ├── IFileStorageService.cs            # NEW — abstraction interface
│   ├── LocalFileStorageService.cs        # NEW — local disk implementation
│   └── DocumentService.cs               # NEW — all document business logic
├── Pages/
│   ├── Documents.razor                   # NEW — My Documents + Shared with Me view
│   ├── DocumentUpload.razor              # NEW — upload form page
│   ├── DocumentDetails.razor             # NEW — detail / edit metadata page
│   └── ProjectDetails.razor             # MODIFIED — add project documents tab
├── Shared/
│   ├── NavMenu.razor                     # MODIFIED — add Documents nav item
│   └── RecentDocumentsWidget.razor       # NEW — dashboard Recent Documents widget
│   Index.razor                           # MODIFIED — embed RecentDocumentsWidget
└── Controllers/
    └── DocumentsController.cs            # NEW — authorized download/preview endpoint (Razor Pages MVC controller)

specs/001-document-upload-management/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   └── document-api.md
└── tasks.md             # Phase 2 output (/speckit.tasks — NOT created here)
```

**Structure Decision**: Single Blazor Server project. All new files slot into the existing `ContosoDashboard/` project folder following established conventions (Models/, Services/, Pages/, Shared/). A minimal MVC controller (`DocumentsController`) is added solely to serve files through an authorized endpoint — this is the standard ASP.NET Core pattern for serving protected static files and does not require a separate project.

## Complexity Tracking

> No constitution violations. No entries required.
