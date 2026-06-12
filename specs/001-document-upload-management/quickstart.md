# Quickstart Validation Guide: Document Upload and Management

**Feature**: 001-document-upload-management  
**Date**: 2026-06-11  
**Purpose**: End-to-end validation scenarios to prove each user story works correctly. Run these after implementation is complete.

---

## Prerequisites

1. ContosoDashboard application running locally (default: `https://localhost:7xxx`)
2. SQL Server LocalDB running (migrations applied)
3. `ContosoDashboard/AppData/uploads/` directory exists and is writable
4. Sample files available on your local machine:
   - A PDF file ≤ 25 MB (e.g., any PDF from your Downloads folder)
   - A JPEG or PNG image ≤ 25 MB
   - A text file (`.txt`)
   - Any file with an unsupported extension (e.g., rename any file to `.exe`)
   - A file > 25 MB (e.g., create one with: `dd if=/dev/zero bs=1M count=26 of=bigfile.bin`)

---

## Scenario 1 — US1: Upload a Document (P1)

**Goal**: Verify that a valid file is accepted, stored, and appears in the document list.

### Steps

1. Log in as **Ni Kang** (`ni.kang@contoso.com`)
2. Navigate to **Documents → Upload**
3. Select the PDF file; enter Title = `"Test Upload"`, Category = `Project Documents`
4. Click **Upload**

**Expected**: Progress indicator appears; success message shown; document appears in My Documents with correct title, category, upload date, and file size.

5. Attempt upload of the `.exe` file (same title)

**Expected**: Error message listing supported file types; no record created.

6. Attempt upload of the 26 MB file

**Expected**: Error message stating the 25 MB limit; no record created.

7. Attempt upload with Title left blank

**Expected**: Validation error on the Title field; form not submitted.

---

## Scenario 2 — US2: Browse and Find Documents (P2)

**Goal**: Verify sort, filter, and search work correctly and respect authorization.

### Setup

Upload at least three documents as Ni Kang with different categories (e.g., `Project Documents`, `Reports`, `Personal Files`) and optionally associate one with a project.

### Steps

1. Open **My Documents**

**Expected**: All uploaded documents appear with title, category, upload date, file size, project.

2. Apply Category filter = `Reports`

**Expected**: Only `Reports` documents shown.

3. Apply Date Range filter to yesterday–today

**Expected**: Only documents uploaded in that range shown.

4. Enter the word from one document's title in the search box

**Expected**: Matching documents returned within 2 seconds; documents from other users not visible.

5. Click the **Upload Date** column header twice (ascending then descending)

**Expected**: List reorders correctly each time.

---

## Scenario 3 — US3: Download and Preview (P3)

**Goal**: Verify authorized download and inline preview; verify unauthorized access is blocked.

### Steps

1. As Ni Kang, click **Download** on the uploaded PDF

**Expected**: Browser prompts to save file; filename matches the original.

2. Click **Preview** on the PDF

**Expected**: PDF renders inline in the browser tab; no download dialog.

3. Click **Preview** on the JPEG/PNG image

**Expected**: Image renders inline.

4. Copy the download URL; log out; paste URL in a new tab (unauthenticated)

**Expected**: Redirected to login page (401/redirect) — file NOT served.

5. Log in as a user with no access to Ni Kang's personal document; attempt the download URL directly

**Expected**: 403 Forbidden — file NOT served.

---

## Scenario 4 — US4: Edit Metadata and Delete (P4)

**Goal**: Verify metadata edits persist and deletion removes the document.

### Steps

1. As Ni Kang, open a document and click **Edit**
2. Change Title to `"Updated Title"`, change Category to `Presentations`, add tag `quarterly`
3. Save

**Expected**: My Documents list and detail immediately reflect the new values.

4. On the same document, click **Replace File** and upload the text file

**Expected**: New file stored; old file no longer served; VersionNote field saved.

5. Click **Delete** on the document; confirm the prompt

**Expected**: Document disappears from My Documents; attempting the old download URL returns 404.

6. Log in as **Ni Kang** and attempt to edit a document uploaded by **Camille Nicole**

**Expected**: Edit option not visible (or returns 403 if URL is crafted manually).

---

## Scenario 5 — US5: Share a Document and Receive Notification (P5)

**Goal**: Verify share grants, notifications, and Shared with Me visibility.

### Steps

1. Log in as **Ni Kang**; open a document; click **Share**; select **Floris Kregel** as recipient; confirm

**Expected**: Share recorded; success message shown.

2. Log in as **Floris Kregel**; open **Notifications**

**Expected**: Notification present: "Ni Kang shared a document with you: [document title]"

3. As Floris, open **Documents → Shared with Me**

**Expected**: The shared document appears with Ni Kang's name and share date.

4. As a fourth user (e.g., **Camille Nicole**), search for Ni Kang's document

**Expected**: Document does not appear in search results.

---

## Scenario 6 — Dashboard & Project Integration

**Goal**: Verify dashboard widget and project documents tab.

### Steps

1. Log in as **Ni Kang**; navigate to **Dashboard**

**Expected**: "Recent Documents" widget shows last 5 documents uploaded by Ni Kang.

2. Upload a new document associated with a project; navigate to that **Project Details** page

**Expected**: A "Documents" tab lists the newly uploaded document; all project members can see it.

3. Log in as **Camille Nicole** (Project Manager); open the same project's Documents tab

**Expected**: Upload button visible for Camille; she can upload a document from the project page.

---

## Validation Checklist

- [ ] US1: Valid upload succeeds; invalid type/size/missing fields rejected
- [ ] US2: Sort, filter (category, date range), and search work; authorization respected
- [ ] US3: Download and preview work; unauthorized access returns 401/403
- [ ] US4: Metadata edits persist; file replacement works; deletion removes document and file
- [ ] US5: Share creates notification; shared document appears in recipient's Shared with Me
- [ ] Dashboard widget shows last 5 uploads
- [ ] Project Documents tab visible to project members; upload available to PM

---

## References

- Data model: [data-model.md](data-model.md)
- File serving contract: [contracts/document-api.md](contracts/document-api.md)
- Spec: [spec.md](spec.md)
