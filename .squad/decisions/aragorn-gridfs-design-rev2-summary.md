# GridFS Design — Revision 2 Summary

## Overview

This document summarizes the critical revisions made to `aragorn-gridfs-design.md` after rubber-duck review by a general-purpose agent on 2025-05-04.

**Original Verdict:** BLOCKED — Revise before M2 begins  
**Revised Verdict:** APPROVED FOR M2 (pending Matthew's sign-off)

---

## Critical Issues Resolved

### 1. Metadata Schema — `attachmentId` Unpopulable for Originals

**Problem:** `IFileStorageService.UploadAsync()` is called *before* the Attachment entity exists, so `attachmentId` cannot be stored in GridFS metadata for original files.

**Resolution:**
- **Original files:** Metadata does NOT include `attachmentId` (entity doesn't exist yet)
- **Thumbnail files:** Metadata DOES include `attachmentId` (entity exists at thumbnail generation time)
- Authorization strategy changed to reverse-lookup via `Attachment.BlobUrl` pattern match

**Impact:** §1 metadata schema revised, §3 authorization logic revised

---

### 2. DI Registration — New MongoClient Per Request

**Problem:** Original design created `new MongoClient()` in scoped `GridFsStorageService` constructor, creating a new connection pool per HTTP request.

**Resolution:**
- Register `IGridFSBucket` as **Singleton** in DI
- Inject bucket into `GridFsStorageService` constructor
- Reuses existing MongoDB connection pool

**Code:**
```csharp
services.AddSingleton<IGridFSBucket>(sp => {
    var options = sp.GetRequiredService<IOptions<MongoDbSettings>>();
    var mongoClient = new MongoClient(options.Value.ConnectionString);
    var database = mongoClient.GetDatabase(options.Value.DatabaseName);
    return new GridFSBucket(database, new GridFSBucketOptions
    {
        BucketName = "attachments",
        ChunkSizeBytes = 255 * 1024
    });
});
services.AddScoped<IFileStorageService, GridFsStorageService>();
```

**Impact:** §4 DI registration completely rewritten

---

### 3. Code Bug — `gridfsObjectId` Used Before Assignment

**Problem:** Original code referenced `gridfsObjectId` variable before it was assigned (actually named `fileId` after the call).

**Resolution:**
```csharp
// BEFORE (broken):
fileName: gridfsObjectId.ToString(),  // ← undefined!
var fileId = await _bucket.UploadFromStreamAsync(...);

// AFTER (fixed):
fileName: ObjectId.GenerateNewId().ToString(),
var fileId = await _bucket.UploadFromStreamAsync(...);
```

**Impact:** §2 `UploadAsync` code sample fixed

---

### 4. No Transaction Boundary Between GridFS and Entity Save

**Problem:** If GridFS upload succeeds but Attachment entity save fails, the file is orphaned with no reference.

**Resolution:**
- Use MongoDB multi-document transactions (if EF Core supports)
- OR: Compensating transaction pattern (upload → try save → on failure, delete file)
- Added comprehensive error handling with cleanup logging

**Code Pattern:**
```csharp
ObjectId? uploadedFileId = null;
try {
    var blobUrl = await fileStorageService.UploadAsync(...);
    if (TryExtractFileIdFromUrl(blobUrl, out var fileId))
        uploadedFileId = fileId;
    
    dbContext.Attachments.Add(attachment);
    await dbContext.SaveChangesAsync();
}
catch (Exception ex) {
    if (uploadedFileId != null) {
        await fileStorageService.DeleteAsync($"/api/attachments/{uploadedFileId}");
        _logger.LogWarning("Cleaned up orphaned file {FileId}", uploadedFileId);
    }
    throw;
}
```

**Impact:** New §5 section "Transaction Boundaries and Error Handling" added

---

### 5. Unhandled Exceptions in `ExtractFileIdFromUrl`

**Problem:** Original helper threw `IndexOutOfRangeException` and `FormatException` on malformed URLs, propagating as HTTP 500.

**Resolution:**
```csharp
private bool TryExtractFileIdFromUrl(string url, out ObjectId fileId)
{
    fileId = ObjectId.Empty;
    var segments = url.Split('/', StringSplitOptions.RemoveEmptyEntries);
    if (segments.Length < 3)
        return false;
    return ObjectId.TryParse(segments[2], out fileId);
}

private ObjectId ExtractFileIdFromUrl(string url)
{
    if (!TryExtractFileIdFromUrl(url, out var fileId))
        throw new ArgumentException($"Invalid GridFS URL format: {url}");
    return fileId;
}
```

**Impact:** §2 helper method revised

---

## Concerns Addressed

### 6. Broken Authorization Chain

**Problem:** Authorization strategy relied on `attachmentId` in GridFS metadata, which doesn't exist for originals (see CRITICAL #1).

**Resolution:**
- Use **reverse-lookup** via `Attachment.BlobUrl` pattern match to find entity
- Then check `Issue` authorization via existing helper
- Alternative: Store `issueId` in metadata (requires interface change — deferred to M5 if needed)

**Impact:** §3 authorization logic completely rewritten with full code sample

---

### 7. Pre-Existing Architecture Violation

**Problem:** Existing `DownloadAttachmentAsync` injects concrete `Persistence.MongoDb.IssueTrackerDbContext`, violating layer dependency tests.

**Resolution:**
- M2 MUST fix this by injecting `IIssueTrackerDbContext` interface (from Domain)
- Documented as requirement in §3 endpoint implementation

**Impact:** §3 endpoint code sample revised, fix requirement noted

---

### 8. `DeleteAsync` Query Deletes Both Original and Thumbnail

**Problem:** Query `{ "metadata.attachmentId": <id> }` matches both original (if populated) and thumbnail, causing double-delete.

**Resolution:**
```csharp
var thumbnailFilter = Builders<GridFSFileInfo>.Filter.And(
    Builders<GridFSFileInfo>.Filter.Eq("metadata.attachmentId", attachment.Id),
    Builders<GridFSFileInfo>.Filter.Eq("metadata.fileType", "thumbnail")  // ← Explicit filter
);
```

**Impact:** §6 Item 5 (`DeleteAsync` implementation) revised

---

### 9. `GenerateThumbnailAsync` Implementation Unspecified

**Problem:** Original design showed `// ... thumbnail generation logic ...` placeholder with no guidance.

**Resolution:**
- Full 60-line implementation with ImageSharp
- Content-type check (skip non-images, return `null`)
- Error handling (corrupt images → log warning, return `null`)
- Metadata population with `attachmentId` (entity exists at this point)

**Impact:** §6 Item 3 now has complete implementation

---

### 10. M2/M4 Scope Boundary Contradiction

**Problem:** §6 dual-read logic was labeled "M4 Scope", but §5 said Sam must write it in M2.

**Resolution:**
- **M2 Scope:** GridFS-only `DownloadAsync` (throws `NotSupportedException` for Azure URLs)
- **M4 Scope:** Dual-read logic added after Sam completes M2
- Clarified in §6 Item 4 with explicit note

**Impact:** §6 Item 4 revised with scope clarification

---

## Optional Improvements Added

### 11. GridFS Indexes for Performance

**Added:**
```csharp
// Index for thumbnail lookup during cascade delete
await filesCollection.Indexes.CreateOneAsync(
    Builders<BsonDocument>.IndexKeys
        .Ascending("metadata.attachmentId")
        .Ascending("metadata.fileType")
);
```

**Impact:** §6 Item 9 added

---

### 12. GridFS Filename Redundancy

**Acknowledged but kept:** Using `ObjectId.ToString()` as GridFS filename maintains consistency with identifier strategy. Using `originalFileName` would be operationally useful but risks collision if sanitization fails.

**Impact:** No change to design (documented rationale)

---

### 13. `Results.File()` Missing `Content-Length`

**Problem:** `GridFSDownloadStream` has `.Length` property, but `Results.File()` doesn't pass it explicitly.

**Resolution:**
```csharp
return Results.Stream(
    stream,
    contentType: attachment.ContentType,
    fileDownloadName: attachment.FileName,
    lastModified: attachment.CreatedAt,
    entityTag: new EntityTagHeaderValue($"\"{attachment.Id}\""),
    enableRangeProcessing: false
);
```

**Impact:** §6 Item 7 revised

---

### 14. `IGridFSBucket` Not in DI

**Already addressed** in CRITICAL #2 resolution.

---

## Exit Criteria (Updated)

✅ Rubber-duck review completed (5 CRITICAL, 5 CONCERNS resolved)  
✅ Transaction boundaries defined (compensating pattern with cleanup)  
✅ Error handling specified (malformed URLs, thumbnail failures, orphaned files)  
✅ Full implementations provided (GenerateThumbnailAsync, DeleteAsync, authorization)  
✅ DI registration fixed (Singleton bucket, NOT per-request MongoClient)  
✅ GridFS indexes specified for performance  
✅ M2/M4 scope boundary clarified  

**Awaiting Matthew's approval to begin M2.**

---

## Files Modified

- `.squad/decisions/aragorn-gridfs-design.md` — All revisions incorporated
- This summary document for quick reference

## Next Steps

1. Matthew reviews revised design
2. If approved, Sam begins M2 implementation with zero ambiguity
3. Gimli prepares M3 test migration plan
