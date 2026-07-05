## SixLabors ImageSharp Licensing Status

### Current Status
- Resolved.
- The codebase no longer uses SixLabors.ImageSharp.
- Thumbnail/image processing was migrated to SkiaSharp (MIT licensed).

### What Changed
1. Removed SixLabors.ImageSharp package usage from runtime and test projects.
2. Added SkiaSharp and Linux native assets for runtime compatibility.
3. Updated thumbnail generation and related tests to use SkiaSharp APIs.
4. Removed the temporary `sixlabors.lic` workflow from repository guidance.

### Historical Note
This document is retained as migration history for the licensing remediation completed in July 2026.
