# GridFS Storage Design — M1 Design Approval

## Status
Draft — awaiting Matthew's approval

## Summary
This design proposes a MongoDB GridFS-backed implementation of `IFileStorageService` that will replace the existing Azure Blob Storage and LocalFileStorage implementations. GridFS will store both original files and thumbnails in a single `attachments` bucket with metadata tags for differentiation. The service will return app-owned routes (e.g., `/api/attachments/{id}` and `/api/attachments/{id}/thumbnail`) instead of external URLs, providing a clean abstraction that eliminates external storage dependencies while maintaining the existing `IFileStorageService` contract.

## 1. Storage Model

### GridFS Bucket Design
**Single Bucket Approach with Metadata Tags**

- **Bucket Name:** `attachments` (configurable via `MongoDbSettings`)
- **Strategy:** Use a single GridFS bucket for both original files and thumbnails, differentiated by metadata
- **Rationale:** 
  - Simplifies bucket management (no need to create/maintain separate buckets)
  - Leverages GridFS metadata for type classification
  - Enables efficient querying by attachment relationship
  - Follows MongoDB best practices for GridFS organization

### File Metadata Schema
Each GridFS file will store the following metadata:

```csharp
{
  "attachmentId": ObjectId,        // Links to Attachment entity in attachments collection
  "contentType": "image/jpeg",     // MIME type
  "originalFileName": "report.pdf", // User-provided filename
  "fileType": "original" | "thumbnail", // Distinguishes file purpose
  "uploadedAt": ISODate,           // Timestamp
  "uploadedBy": "auth0|123"        // User identifier
}
```

**Key Design Points:**
- `attachmentId` creates bidirectional linkage: `Attachment.BlobUrl` references GridFS file, GridFS metadata references `Attachment.Id`
- `fileType` enables single-bucket organization without naming conventions
- All domain metadata lives in GridFS, making files self-describing
- `originalFileName` preserved for download `Content-Disposition` headers

### File Naming Strategy in GridFS
**GridFS Filename Convention:** `{ObjectId}` (the GridFS file's own ObjectId)

- **Originals:** GridFS filename = GridFS ObjectId (e.g., `507f1f77bcf86cd799439011`)
- **Thumbnails:** GridFS filename = GridFS ObjectId (e.g., `507f1f77bcf86cd799439012`)
- **Rationale:** 
  - GridFS ObjectIds are globally unique
  - No collision risk (unlike GUID prefixes)
  - Direct GridFS file lookup by `_id`
  - Metadata `fileType` field provides semantic meaning

## 2. Identifier Strategy

### Current State Analysis
- **BlobStorageService:** Returns Azure blob URIs (e.g., `https://{account}.blob.core.windows.net/{container}/{guid}/{filename}`)
- **LocalFileStorageService:** Returns relative web paths (e.g., `/uploads/{guid}_{filename}`)
- **Attachment Model:** Stores these as `BlobUrl` (string) and `ThumbnailUrl` (string?)
- **AttachmentCard UI:** Consumes URLs directly for `<img src>` and `<a href>` download links

### Design Decision: App-Owned Route Format

**Selected Approach:** Option B — App-owned routes with GridFS ObjectId

`UploadAsync()` will return: `/api/attachments/{gridfsObjectId}`  
`GenerateThumbnailAsync()` will return: `/api/attachments/{gridfsObjectId}/thumbnail`

**Examples:**
- Original: `/api/attachments/507f1f77bcf86cd799439011`
- Thumbnail: `/api/attachments/507f1f77bcf86cd799439012/thumbnail`

### Rationale

**Why not raw GridFS ObjectIds (Option A)?**
- Would break `AttachmentCard.razor` which expects URLs for `<img src>` and `<a href>`
- Requires changing `Attachment` model fields from "URL" semantics to "ID" semantics
- Forces API clients to construct download routes manually

**Why not keep Azure URL format (Option C)?**
- False abstraction — implies external storage provider
- Harder to route (requires parsing URL segments)
- Doesn't reflect architectural shift to MongoDB-native storage

**Why app-owned routes?**
✅ **Zero changes to `Attachment` model** — `BlobUrl` and `ThumbnailUrl` remain strings containing URLs  
✅ **Zero changes to `AttachmentCard.razor`** — URLs work directly in `<img src>` and `<a download>`  
✅ **Clean abstraction** — storage implementation hidden behind API routes  
✅ **Authentication-ready** — routes can enforce `RequireAuthorization()`  
✅ **Testable** — no need to mock external blob URLs  
✅ **Flexible** — can add `/api/attachments/{id}/metadata` or other operations later

### Impact on Attachment Model
**No changes required to `Attachment.cs` or `AttachmentDto.cs`**

```csharp
// Attachment.cs remains unchanged
public string BlobUrl { get; set; } = string.Empty;  // Still stores URL string
public string? ThumbnailUrl { get; set; }            // Still stores URL string
```

**Storage Example:**
```json
{
  "_id": ObjectId("67890..."),
  "fileName": "screenshot.png",
  "contentType": "image/png",
  "blobUrl": "/api/attachments/507f1f77bcf86cd799439011",
  "thumbnailUrl": "/api/attachments/507f1f77bcf86cd799439012/thumbnail"
}
```

### GridFsStorageService Return Format

```csharp
public class GridFsStorageService : IFileStorageService
{
    // Returns: /api/attachments/{gridfsObjectId}
    public async Task<string> UploadAsync(
        Stream content, 
        string fileName, 
        string contentType, 
        CancellationToken cancellationToken = default)
    {
        var fileId = await _bucket.UploadFromStreamAsync(
            fileName: gridfsObjectId.ToString(),
            source: content,
            options: new GridFSUploadOptions
            {
                Metadata = new BsonDocument
                {
                    { "contentType", contentType },
                    { "originalFileName", fileName },
                    { "fileType", "original" },
                    { "uploadedAt", DateTime.UtcNow }
                }
            },
            cancellationToken);
        
        return $"/api/attachments/{fileId}";
    }
    
    // Returns: /api/attachments/{gridfsObjectId}/thumbnail
    public async Task<string?> GenerateThumbnailAsync(
        string blobUrl, 
        CancellationToken cancellationToken = default)
    {
        var originalFileId = ExtractFileIdFromUrl(blobUrl);
        // ... thumbnail generation logic ...
        var thumbnailId = await _bucket.UploadFromStreamAsync(...);
        
        return $"/api/attachments/{thumbnailId}/thumbnail";
    }
}
```

**Helper Method:**
```csharp
private ObjectId ExtractFileIdFromUrl(string url)
{
    // url format: /api/attachments/507f1f77bcf86cd799439011 or 
    //             /api/attachments/507f1f77bcf86cd799439012/thumbnail
    var segments = url.Split('/', StringSplitOptions.RemoveEmptyEntries);
    var idSegment = segments[2]; // attachments/{id}
    return ObjectId.Parse(idSegment);
}
```

## 3. Download and Thumbnail Route Semantics

### Proposed Routes

#### **Primary Download Route**
**Endpoint:** `GET /api/attachments/{id}`  
**Purpose:** Download original file OR thumbnail (determined by GridFS metadata)  
**Response:**
- **Headers:**
  ```
  Content-Type: {file.Metadata.ContentType}
  Content-Disposition: attachment; filename="{file.Metadata.OriginalFileName}"
  Content-Length: {file.Length}
  ```
- **Body:** Binary file stream from GridFS

#### **Thumbnail Route (Explicit)**
**Endpoint:** `GET /api/attachments/{id}/thumbnail`  
**Purpose:** Explicitly request thumbnail variant  
**Response:**
- **Headers:**
  ```
  Content-Type: image/jpeg
  Content-Disposition: inline
  Content-Length: {thumbnail.Length}
  ```
- **Body:** Thumbnail JPEG stream
- **Fallback:** If no thumbnail exists, return 404 (UI shows original)

**Note:** The route `/api/attachments/{id}/thumbnail` is for **accessing** the thumbnail. The actual thumbnail GridFS file has its own ObjectId, so:
- `BlobUrl = /api/attachments/{originalGridFsId}`
- `ThumbnailUrl = /api/attachments/{thumbnailGridFsId}/thumbnail`

The `/thumbnail` suffix is a **route convention** that tells the endpoint "this ID is for a thumbnail file", not "generate a thumbnail from this original".

### Authentication Strategy
**Recommendation:** `RequireAuthorization()` on both routes

**Rationale:**
- Attachments belong to issues (which have access controls)
- Prevents unauthorized access to files via URL guessing
- Consistent with existing `AttachmentEndpoints` pattern
- Can be relaxed later for public-issue scenarios if needed

**Authorization Check Logic (recommended for M2):**
```csharp
// In endpoint handler:
// 1. Parse GridFS file ID from route
// 2. Fetch GridFS file metadata (includes attachmentId)
// 3. Query Attachment entity to get IssueId
// 4. Verify user has access to Issue (existing permission logic)
// 5. Stream file if authorized, otherwise 403 Forbidden
```

### Endpoint Implementation Location
**File:** `src/Web/Features/AttachmentEndpoints.cs` (extends existing file)

**Rationale:**
- Follows established pattern in `CommentEndpoints.cs` and `AttachmentEndpoints.cs`
- Keeps all attachment-related routes together
- Avoids creating new endpoint file

**Implementation Sketch:**
```csharp
// Add to existing AttachmentEndpoints.MapAttachmentEndpoints()
group.MapGet("/attachments/{id}", DownloadFileAsync)
    .WithName("DownloadFile")
    .RequireAuthorization();

group.MapGet("/attachments/{id}/thumbnail", DownloadThumbnailAsync)
    .WithName("DownloadThumbnail")
    .RequireAuthorization();

private static async Task<IResult> DownloadFileAsync(
    string id,
    [FromServices] IFileStorageService fileStorageService,
    [FromServices] IIssueTrackerDbContext dbContext,
    CancellationToken cancellationToken)
{
    // Implementation details in M2
}
```

**Note:** This **replaces** the current `DownloadAttachmentAsync` method (line 149-174) which uses `attachment.BlobUrl`. The new implementation will:
1. Parse GridFS ObjectId from `{id}` route parameter
2. Use `GridFsStorageService.DownloadAsync()` to stream from GridFS
3. Maintain same response structure (`Results.File()`)

## 4. DI Registration Strategy

### Service Location
**Project:** `src/Persistence.MongoDb/`  
**Namespace:** `Persistence.MongoDb.Services`  
**Class:** `GridFsStorageService : IFileStorageService`

**File Structure:**
```
src/Persistence.MongoDb/
├── Services/
│   ├── AuditLogWriterService.cs (existing)
│   └── GridFsStorageService.cs (new — M2 deliverable)
├── ServiceCollectionExtensions.cs (modified — add GridFS registration)
```

### Registration in `ServiceCollectionExtensions.cs`

**Add new method:**
```csharp
public static IServiceCollection AddGridFsStorage(
    this IServiceCollection services,
    IConfiguration configuration)
{
    services.AddScoped<IFileStorageService, GridFsStorageService>();
    return services;
}
```

**GridFsStorageService constructor dependencies:**
```csharp
public class GridFsStorageService : IFileStorageService
{
    private readonly IMongoDatabase _database;
    private readonly IGridFSBucket _bucket;
    private readonly ILogger<GridFsStorageService> _logger;

    public GridFsStorageService(
        IOptions<MongoDbSettings> settings,
        ILogger<GridFsStorageService> logger)
    {
        var mongoClient = new MongoClient(settings.Value.ConnectionString);
        _database = mongoClient.GetDatabase(settings.Value.DatabaseName);
        
        _bucket = new GridFSBucket(_database, new GridFSBucketOptions
        {
            BucketName = "attachments",
            ChunkSizeBytes = 255 * 1024 // 255KB chunks (GridFS default)
        });
        
        _logger = logger;
    }
}
```

### Web/Program.cs Selection Logic

**Current code (lines 128-139):**
```csharp
// Configure File Storage (Azure Blob or Local)
var blobConnectionString = builder.Configuration["BlobStorage:ConnectionString"];
if (!string.IsNullOrEmpty(blobConnectionString))
{
    builder.Services.AddAzureBlobStorage(builder.Configuration);
}
else
{
    builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
}
```

**Proposed change for M4 (not part of M1 design):**
```csharp
// Configure File Storage (GridFS by default, fallback to Local)
if (builder.Configuration.GetValue<bool>("MongoDB:UseGridFS", defaultValue: true))
{
    // GridFS is now the primary storage (requires existing MongoDB connection)
    builder.Services.AddGridFsStorage(builder.Configuration);
}
else
{
    // Fallback to local file storage for legacy/dev scenarios
    builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
}
```

**Configuration Setting:**
```json
// appsettings.json
{
  "MongoDB": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "IssueTracker",
    "UseGridFS": true  // New setting to enable GridFS
  }
}
```

**Note:** Azure Blob Storage registration will be removed in M4 after cutover validation.

## 5. Architecture Compliance

### Layer Dependency Analysis

#### ✅ **GridFsStorageService in Persistence.MongoDb**
- **Depends on:** 
  - `Domain.Abstractions` (implements `IFileStorageService`)
  - `MongoDB.Driver` (GridFS APIs)
  - `Microsoft.Extensions.*` (DI, Logging, Configuration)
- **Does NOT depend on:**
  - ❌ `Web` (no upward dependency)
  - ❌ `Persistence.AzureStorage` (no cross-persistence dependency)

**Verification:** Passes `LayerDependencyTests.Persistence_ShouldNotDependOn_Web()`

#### ✅ **IFileStorageService Interface in Domain.Abstractions**
- Already exists in `Domain` layer
- No changes required
- Contract remains stable

#### ✅ **Attachment Routes in Web/Features/AttachmentEndpoints.cs**
- Web layer depends on `Domain.Abstractions.IFileStorageService` ✅
- Web does NOT depend on `Persistence.MongoDb` directly ✅
- DI container resolves implementation at runtime

### Architecture Test Compatibility

**Existing Tests (no changes needed):**
- `LayerDependencyTests.Domain_ShouldNotDependOn_Persistence()` ✅
- `LayerDependencyTests.Persistence_ShouldNotDependOn_Web()` ✅
- `LayerDependencyTests.PersistenceMongoDb_ShouldNotDependOn_PersistenceAzureStorage()` ✅

**New Test (recommended for M2):**
```csharp
[Fact]
public void GridFsStorageService_ShouldImplement_IFileStorageService()
{
    var type = typeof(GridFsStorageService);
    type.Should().Implement<IFileStorageService>();
}
```

### Dependency Flow Diagram
```
┌─────────────────────────────────────────┐
│ Web Layer                                │
│ - AttachmentEndpoints                    │
│ - Program.cs (DI registration)           │
└────────┬────────────────────────────────┘
         │ depends on
         ▼
┌─────────────────────────────────────────┐
│ Domain Layer                             │
│ - IFileStorageService (abstraction)      │
│ - Attachment (model)                     │
└─────────────────────────────────────────┘
         ▲ implements
         │
┌────────┴────────────────────────────────┐
│ Persistence.MongoDb Layer                │
│ - GridFsStorageService                   │
│ - ServiceCollectionExtensions            │
└─────────────────────────────────────────┘
```

**Key Points:**
- Web depends on Domain ✅
- Persistence.MongoDb depends on Domain ✅
- Web does NOT depend on Persistence.MongoDb ✅ (resolved via DI)
- No circular dependencies ✅

## 6. Migration Compatibility

### Dual-Mode Rollout Strategy (M4 Scope)

**Phase 1: Parallel Write (New Uploads → GridFS, Old URLs Still Valid)**
1. Deploy `GridFsStorageService` registration
2. New attachments write to GridFS (return `/api/attachments/{id}` URLs)
3. Old attachments with Azure URLs continue to work via existing `DownloadAttachmentAsync()`
4. No data migration yet

**Phase 2: Data Migration (Background Job)**
1. Iterate over `Attachment` collection where `BlobUrl` contains external URLs
2. Download file from Azure → Upload to GridFS
3. Update `Attachment.BlobUrl` to new format
4. Preserve original metadata (FileName, ContentType, UploadedBy)

**Phase 3: Azure Decommission**
1. Verify no `Attachment` records reference Azure URLs
2. Remove `BlobStorageService` registration from `Program.cs`
3. Remove `Persistence.AzureStorage` project reference
4. Remove Azurite from `AppHost`

### Handling Existing Azure URLs

**During Dual-Mode (M4):**

The existing `DownloadAttachmentAsync` endpoint (lines 149-174 in `AttachmentEndpoints.cs`) will need modification:

```csharp
private static async Task<IResult> DownloadAttachmentAsync(
    string id,
    [FromServices] IFileStorageService fileStorageService,
    [FromServices] IIssueTrackerDbContext dbContext,
    CancellationToken cancellationToken)
{
    var attachment = await dbContext.Attachments
        .FirstOrDefaultAsync(a => a.Id == ObjectId.Parse(id), cancellationToken);

    if (attachment == null)
        return Results.NotFound();

    try
    {
        // BlobUrl will be either:
        // - Old format: https://{account}.blob.core.windows.net/...
        // - New format: /api/attachments/{gridfsId}
        
        var stream = await fileStorageService.DownloadAsync(
            attachment.BlobUrl, 
            cancellationToken);
        
        return Results.File(stream, attachment.ContentType, attachment.FileName);
    }
    catch (FileNotFoundException)
    {
        return Results.NotFound();
    }
}
```

**GridFsStorageService.DownloadAsync() Dual-Read Logic:**

```csharp
public async Task<Stream> DownloadAsync(
    string blobUrl, 
    CancellationToken cancellationToken = default)
{
    // Detect URL format
    if (blobUrl.StartsWith("/api/attachments/"))
    {
        // New format: GridFS file
        var fileId = ExtractFileIdFromUrl(blobUrl);
        return await _bucket.OpenDownloadStreamAsync(fileId, cancellationToken: cancellationToken);
    }
    else if (blobUrl.StartsWith("http://") || blobUrl.StartsWith("https://"))
    {
        // Old format: Azure blob URL (fallback during migration)
        _logger.LogWarning(
            "Accessing legacy Azure blob URL: {BlobUrl}. This should be migrated.", 
            blobUrl);
        
        // Throw NotSupportedException or delegate to a fallback service
        throw new NotSupportedException(
            "Azure blob URLs are no longer supported. Please migrate this attachment.");
    }
    else
    {
        // Local file format: /uploads/{filename} (LocalFileStorageService)
        _logger.LogWarning(
            "Accessing local file URL: {BlobUrl}. This should be migrated.", 
            blobUrl);
        
        throw new NotSupportedException(
            "Local file URLs are no longer supported. Please migrate this attachment.");
    }
}
```

**Migration Script (M4):**
```csharp
// Pseudocode for data migration background job
foreach (var attachment in dbContext.Attachments)
{
    if (attachment.BlobUrl.StartsWith("http") || attachment.BlobUrl.StartsWith("/uploads"))
    {
        // 1. Download from old storage
        var stream = await oldStorageService.DownloadAsync(attachment.BlobUrl);
        
        // 2. Upload to GridFS
        var newBlobUrl = await gridFsService.UploadAsync(
            stream, 
            attachment.FileName, 
            attachment.ContentType);
        
        // 3. Update Attachment entity
        attachment.BlobUrl = newBlobUrl;
        
        // 4. Handle thumbnail if exists
        if (!string.IsNullOrEmpty(attachment.ThumbnailUrl))
        {
            var thumbnailStream = await oldStorageService.DownloadAsync(attachment.ThumbnailUrl);
            attachment.ThumbnailUrl = await gridFsService.UploadAsync(
                thumbnailStream, 
                $"{attachment.FileName}_thumb.jpg", 
                "image/jpeg");
        }
        
        await dbContext.SaveChangesAsync();
    }
}
```

### Rollback Strategy
**If GridFS encounters issues during M4 deployment:**
1. Revert `Program.cs` registration to Azure Blob Storage
2. New uploads go back to Azure
3. Existing GridFS attachments remain accessible (GridFsStorageService stays registered but inactive)
4. No data loss (MongoDB retains files, Azure Blob still available)

## Recommended Decisions

### For Sam (M2 Implementation):

1. **Implement `GridFsStorageService` in `src/Persistence.MongoDb/Services/GridFsStorageService.cs`**
   - Constructor: `IOptions<MongoDbSettings>`, `ILogger<GridFsStorageService>`
   - Bucket name: `"attachments"` (configurable)
   - Chunk size: 255KB (GridFS default)

2. **UploadAsync() returns:** `/api/attachments/{gridfsObjectId}`
   - Store metadata: `contentType`, `originalFileName`, `fileType: "original"`, `uploadedAt`

3. **GenerateThumbnailAsync() returns:** `/api/attachments/{thumbnailGridFsId}/thumbnail`
   - Thumbnail stored as separate GridFS file
   - Metadata: `fileType: "thumbnail"`, `attachmentId` (links to original)

4. **DownloadAsync() logic:**
   - Parse GridFS ObjectId from `/api/attachments/{id}` format
   - Use `GridFSBucket.OpenDownloadStreamAsync(fileId)`
   - During migration (M4): detect Azure URLs and throw `NotSupportedException`

5. **DeleteAsync() logic:**
   - Parse GridFS ObjectId
   - Use `GridFSBucket.DeleteAsync(fileId)`
   - Also delete associated thumbnail (query by metadata `attachmentId`)

6. **Add `AddGridFsStorage()` extension method in `ServiceCollectionExtensions.cs`**

7. **Update `AttachmentEndpoints.cs`:**
   - Replace `DownloadAttachmentAsync` (line 149) to use GridFS file ID from route
   - Keep authentication: `RequireAuthorization()`

8. **Testing Requirements:**
   - Unit tests: Mock `IGridFSBucket` for service logic
   - Integration tests: Use `MongoDbFixture` (replica set `rs0` in Docker)
   - Verify routes return correct `Content-Type` and `Content-Disposition` headers

### For Gimli (M3 Test Migration):

1. **Replace Azurite tests with GridFS equivalents**
   - File: `tests/Persistence.MongoDb.Tests.Integration/GridFsStorageServiceTests.cs`
   - Use `MongoDbFixture` (existing pattern)
   - Test upload, download, delete, thumbnail generation

2. **Verify `AttachmentEndpointTests` work with GridFS URLs**
   - Ensure `/api/attachments/{id}` returns correct file streams
   - Test authorization failures (401/403)

### For Boromir & Sam (M4 Rollout):

1. **Deployment:**
   - Add `"MongoDB:UseGridFS": true` to `appsettings.Production.json`
   - Deploy `GridFsStorageService` registration

2. **Migration:**
   - Run data migration script (background job)
   - Verify all `Attachment.BlobUrl` values start with `/api/attachments/`

3. **Cleanup:**
   - Remove Azure Blob Storage registration from `Program.cs` (lines 128-139)
   - Remove `builder.Services.AddAzureBlobStorage()`
   - Remove `Persistence.AzureStorage` project reference
   - Update Aspire `AppHost` to remove Azurite provisioning
   - Archive `tests/Persistence.AzureStorage.Tests.Integration/` (move to `docs/archive/`)

### Configuration Changes (M2):

**No changes to `MongoDbSettings.cs`** — GridFS uses existing `ConnectionString` and `DatabaseName`.

**Optional (for explicit control):**
```csharp
public class MongoDbSettings
{
    // ... existing properties ...
    
    /// <summary>
    /// GridFS bucket name for attachments (default: "attachments")
    /// </summary>
    public string GridFsBucketName { get; set; } = "attachments";
}
```

---

## Exit Criteria for M1 Approval

✅ **Design document reviewed by Matthew**  
✅ **Identifier strategy confirmed** (app-owned routes: `/api/attachments/{id}`)  
✅ **GridFS bucket design validated** (single bucket with metadata)  
✅ **Download/thumbnail route semantics approved** (`GET /api/attachments/{id}` and `/api/attachments/{id}/thumbnail`)  
✅ **DI registration strategy confirmed** (`Persistence.MongoDb.ServiceCollectionExtensions`)  
✅ **Architecture compliance verified** (no boundary violations)  
✅ **Migration rollout plan accepted** (dual-mode → data migration → cleanup)

**Once approved, Sam can begin M2 implementation with zero ambiguity.**
