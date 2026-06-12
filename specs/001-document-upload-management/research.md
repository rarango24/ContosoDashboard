# Research: Document Upload and Management

**Feature**: 001-document-upload-management  
**Date**: 2026-06-11  
**Status**: Complete — no NEEDS CLARIFICATION markers remain

---

## Topic 1: File Upload in Blazor Server (.NET 8)

**Decision**: Use the `InputFile` component (`IBrowserFile`) for browser-side file selection. Stream each file from `IBrowserFile.OpenReadStream(maxAllowedSize)` directly to disk rather than buffering the full content in memory. Increase the SignalR `MaximumReceiveMessageSize` to at least 32 MB in `Program.cs` to prevent connection drops during large uploads.

**Rationale**: `InputFile` is the native Blazor file-input component and handles the browser File API without JavaScript interop. Streaming avoids holding a 25 MB file in server memory; it reads in small chunks directly to the `FileStream`. The SignalR default message size (~32 KB) is far below 25 MB — raising it is required for Blazor Server uploads.

**Alternatives Considered**:
- `IFormFile` (MVC) — not available in Blazor Server components.
- Custom JS interop — adds complexity; `InputFile` is sufficient.
- Base64 encoding — inflates payload 33%; wasteful and slower.

---

## Topic 2: Serving Protected Files (Authorized Download/Preview Endpoint)

**Decision**: Add a minimal Razor Pages–style MVC controller `DocumentsController` with an authorized action `Download(int id, bool inline)`. The action validates the requesting user's permissions via `DocumentService`, then returns `PhysicalFile(path, mimeType, fileDownloadName)` with `Content-Disposition: attachment` for downloads or `inline` for PDF/image previews.

**Rationale**: Files stored outside `wwwroot` are not web-accessible; they MUST be served via an explicit endpoint. A controller action allows `[Authorize]` enforcement before any file I/O. `Content-Disposition: inline` triggers native browser rendering for PDFs and images without a separate viewer. MVC controller is preferred over Minimal API here because the project already uses ASP.NET Core Razor Pages patterns and a controller is more discoverable for trainees.

**Alternatives Considered**:
- Static file middleware with policy — harder to integrate per-resource ownership checks.
- Minimal API — functional but introduces a new pattern inconsistent with the existing codebase.
- Serving from `wwwroot` — bypasses authorization entirely; rejected on security grounds.

---

## Topic 3: Virus/Malware Scan Stub (Offline Training)

**Decision**: Define `IFileScanner` interface with a single method `Task<bool> ScanAsync(Stream content)`. Implement `StubFileScanner` that always returns `true` (clean) for the training environment. Register it in `Program.cs` via dependency injection. Document the stub clearly with a code comment directing implementors to swap in `ClamAvScanner` (using the `nClam` NuGet package) for production.

**Rationale**: A stub models the correct async interface and allows the full upload flow — including the scan gate before storage — to be exercised in training without external infrastructure. The `IFileScanner` abstraction ensures the business logic (`DocumentService`) does not need modification when a real scanner is added. `nClam` + ClamAV is the recommended open-source path for production: offline-capable (local daemon), widely adopted, and has a maintained .NET binding.

**Alternatives Considered**:
- Windows Defender API — OS-specific; not portable across macOS/Linux dev environments.
- VirusTotal API — requires internet; violates offline constraint (Constitution Principle II).
- No scanning step at all — acceptable for training but removes the teachable security pattern; rejected.

---

## Summary: All NEEDS CLARIFICATION Resolved

| Unknown | Resolution |
|---------|------------|
| File upload mechanism in Blazor Server | `InputFile` + `IBrowserFile.OpenReadStream` + SignalR `MaximumReceiveMessageSize` ≥ 32 MB |
| Protected file serving pattern | MVC controller + `PhysicalFile()` + `[Authorize]` + `Content-Disposition` header |
| Virus scan in offline training environment | `IFileScanner` interface + `StubFileScanner` (always clean); production path = `nClam` + ClamAV |
