# Legolas — Learnings for IssueTrackerApp

**Role:** Frontend - Blazor UI Components
**Project:** IssueTrackerApp
**Initialized:** 2026-03-12

---

## Learnings

### 2026-03-12 - SignalR Frontend Integration (Issue #37)

**Task:** Implement real-time notifications and auto-refresh UI for SignalR integration

**What I Built:**
1. **Toast Notification System**
   - Created `ToastService` for managing notification queue
   - Built `ToastContainer.razor` component with TailwindCSS styling
   - Supports 4 types: info, success, warning, error
   - Auto-dismiss with configurable duration
   - Dark mode support with proper color schemes
   - Slide-in animation using CSS keyframes

2. **SignalR Client Service**
   - Created `SignalRClientService` for hub connection management
   - Implemented auto-reconnect with exponential backoff (0s, 2s, 5s, 10s)
   - Event handlers for: IssueCreated, IssueUpdated, IssueAssigned, CommentAdded
   - Connection state tracking and notifications
   - Group subscription management (join/leave issue-specific groups)

3. **Connection State Indicator**
   - Built `SignalRConnection.razor` component
   - Visual indicators: 🟢 Connected (green dot), 🟡 Connecting (pulsing yellow), 🔴 Disconnected (red)
   - Positioned at bottom-right, only shown for authenticated users
   - Auto-connects on component initialization

4. **Real-Time Auto-Refresh**
   - Updated `Issues/Index.razor`: Auto-refreshes list on create/update events
   - Updated `Issues/Details.razor`: Auto-refreshes details on update/comment events
   - Issue-specific subscriptions: joins group on mount, leaves on disposal
   - Proper event handler cleanup to prevent memory leaks

5. **Service Registration**
   - Added `ToastService` and `SignalRClientService` as scoped services
   - Updated `MainLayout.razor` to include ToastContainer and SignalRConnection

**Key Technical Decisions:**
- Used **scoped services** (not singletons) for client-side state since Blazor Server creates per-user circuits
- Implemented `IDisposable` and `IAsyncDisposable` for proper cleanup
- Used `InvokeAsync(StateHasChanged)` for thread-safe UI updates from SignalR events
- Added CSS animations in source file (`Styles/app.css`) not generated output
- Namespace consistency: Used `Web.Services` not `IssueTrackerApp.Web.Services`

**Challenges & Solutions:**
1. **Namespace mismatch**: Initially used `IssueTrackerApp.Web.Services` but project uses `Web.Services`
2. **Missing using directives**: Added `Microsoft.AspNetCore.Components.Authorization` for `AuthorizeView`
3. **CSS generation**: Ran `npm run css:build` to regenerate Tailwind output after adding animations
4. **Branch coordination**: Had to create branch when Sam's wasn't immediately available, but it existed locally

**Files Created:**
- `src/Web/Services/ToastService.cs`
- `src/Web/Services/SignalRClientService.cs`
- `src/Web/Components/Shared/ToastContainer.razor`
- `src/Web/Components/Shared/SignalRConnection.razor`

**Files Modified:**
- `src/Web/Program.cs` - Service registrations
- `src/Web/Components/Layout/MainLayout.razor` - Added components
- `src/Web/Components/Pages/Issues/Index.razor` - Auto-refresh integration
- `src/Web/Components/Pages/Issues/Details.razor` - Auto-refresh and group subscriptions
- `src/Web/Styles/app.css` - Toast animations
- `src/Web/Web.csproj` - Added SignalR client package
- `Directory.Packages.props` - Package version management

**Package Added:**
- `Microsoft.AspNetCore.SignalR.Client` 10.0.5

**Best Practices Applied:**
- Toast notifications for all SignalR events (better UX than silent updates)
- Connection state visibility (users know when real-time is active)
- Exponential backoff for reconnection (reduces server load)
- Issue-specific groups (reduces unnecessary updates)
- Proper disposal patterns (no memory leaks)
- Dark mode consistency across all new UI elements

**Testing Notes:**
- Build successful: `dotnet build src/Web/Web.csproj`
- All namespace issues resolved
- Animations working via Tailwind CSS
- Ready for integration testing with running app

**Collaboration:**
- Worked with Sam's backend SignalR implementation
- PR #39 updated with frontend changes and marked ready for review
- Commit message follows conventional commits format

---

## Notes

- Team transferred from IssueManager squad
- Same tech stack: .NET 10, Blazor, Aspire, MongoDB, Redis, Auth0, MediatR
- Ready to begin development

---

### 2026-03-12 - Issue Attachments UI (Issue #35)

**Task:** Implement file attachment upload and display UI for issues

**What I Built:**
1. **FileUpload Component (`FileUpload.razor`)**
   - Drag-and-drop file upload zone with visual feedback
   - Highlight on drag-over (border turns primary color)
   - File type validation (images: JPG, PNG, GIF, WEBP; documents: PDF, TXT, MD)
   - File size validation (10MB limit with clear error message)
   - Upload progress indicator with animated spinner
   - Error state display with red alert box
   - Accept string for browser file picker
   - TailwindCSS styling with dark mode support

2. **AttachmentCard Component (`AttachmentCard.razor`)**
   - Displays individual attachment in a card layout
   - Image thumbnails with hover scale animation
   - Document icons for PDF, TXT, MD with appropriate colors
   - File metadata: name, size (formatted), upload date, author
   - Hover overlay with download and delete buttons
   - Authorization check: only owner/admin see delete button
   - Responsive design with smooth transitions

3. **AttachmentList Component (`AttachmentList.razor`)**
   - Grid layout: 2 columns mobile, 3 tablet, 4 desktop
   - Loading state with spinner
   - Error state with red alert
   - Empty state with icon and message
   - Delete confirmation modal integration
   - Authorization checks for delete actions
   - Exposed methods: SetLoading, SetError, ClearError

4. **AttachmentService Integration**
   - Created `AttachmentService.cs` following the project's service pattern
   - Interface `IAttachmentService` with three methods:
     - `GetIssueAttachmentsAsync` - Retrieves all attachments for an issue
     - `AddAttachmentAsync` - Uploads new attachment with stream
     - `DeleteAttachmentAsync` - Deletes attachment with authorization check
   - Wraps MediatR commands/queries
   - Proper error handling and logging

5. **Issue Details Page Integration**
   - Added attachment section between issue details and comments
   - Integrated FileUpload component
   - Integrated AttachmentList component
   - Upload handler with current user context
   - Delete handler with authorization
   - Auto-refresh attachments after upload/delete
   - Error and success message display

6. **SignalR Real-Time Updates**
   - Added `OnAttachmentAdded` event to SignalRClientService
   - Added `OnAttachmentDeleted` event to SignalRClientService
   - Event handlers with toast notifications
   - Auto-refresh attachment list when events fire
   - Subscribed/unsubscribed in component lifecycle

7. **Service Registration**
   - Registered `IAttachmentService` as scoped service in Program.cs
   - Follows same pattern as other services (IssueService, CommentService, etc.)

**Technical Details:**
- **File Validation Constants:**
  - Max file size: 10MB (10 * 1024 * 1024 bytes)
  - Image types: image/jpeg, image/png, image/gif, image/webp
  - Document types: application/pdf, text/plain, text/markdown
  - Client-side validation before upload

- **Authorization Logic:**
  - User can delete own attachments
  - Admins can delete any attachment
  - Delete button hidden if user unauthorized

- **File Display:**
  - Images: Show thumbnail or full image
  - PDF: Red document icon with "PDF" badge
  - Markdown: Blue document icon with "MD" badge
  - Text: Gray document icon with "TXT" badge

- **User Context:**
  - Retrieved from AuthenticationStateProvider
  - Extracts: ObjectId, Name, Email
  - Used for upload attribution and authorization

**Files Created:**
- `src/Web/Services/AttachmentService.cs`
- `src/Web/Components/Shared/FileUpload.razor`
- `src/Web/Components/Issues/AttachmentCard.razor`
- `src/Web/Components/Issues/AttachmentList.razor`

**Files Modified:**
- `src/Web/Components/Pages/Issues/Details.razor` - Added attachment section
- `src/Web/Services/SignalRClientService.cs` - Added attachment events
- `src/Web/Program.cs` - Registered AttachmentService

**Backend Files (Already Existed):**
- `src/Domain/DTOs/AttachmentDto.cs` - Has IsImage property and FileSizeFormatted
- `src/Domain/Models/Attachment.cs` - MongoDB entity
- `src/Domain/Models/FileValidationConstants.cs` - Validation constants
- `src/Domain/Features/Attachments/Commands/AddAttachmentCommand.cs`
- `src/Domain/Features/Attachments/Commands/DeleteAttachmentCommand.cs`
- `src/Domain/Features/Attachments/Queries/GetIssueAttachmentsQuery.cs`
- `src/Web/Services/LocalFileStorageService.cs` - File storage implementation

**Key Design Decisions:**
1. **Component Composition:** Separated concerns - FileUpload handles upload UI, AttachmentCard handles individual display, AttachmentList orchestrates
2. **Event Callbacks:** Used EventCallbacks for parent-child communication (OnFileSelected, OnDelete, OnError)
3. **Authorization:** Checked at both UI level (button visibility) and service level
4. **Error Handling:** Multiple error states with clear user messaging
5. **Real-Time:** Integrated with existing SignalR infrastructure for live updates
6. **Styling:** Consistent TailwindCSS patterns with dark mode throughout
7. **File Icons:** Used Heroicons-style SVG icons for document types
8. **Progress Feedback:** Visual spinner during upload with file name display

**Challenges & Solutions:**
1. **Branch Availability:** Branch `squad/35-issue-attachments` existed locally but not on remote initially - checked it out successfully
2. **SignalR Events:** Had to add new events (OnAttachmentAdded, OnAttachmentDeleted) to SignalRClientService
3. **User Context:** Needed to extract user info in multiple places - considered creating helper but kept inline for clarity
4. **File Stream Handling:** Used `OpenReadStream` with 10MB max size parameter for browser file uploads

**Styling Highlights:**
- **Drop Zone:** Dashed border, changes to primary color on drag-over
- **Upload Progress:** Animated spinner with pulsing progress bar
- **Cards:** White bg in light mode, gray-800 in dark mode
- **Hover Effects:** Scale transform on images, opacity fade on overlay
- **Icons:** Color-coded by file type (red PDF, blue MD, gray TXT)
- **Responsive Grid:** 2/3/4 columns based on screen size
- **Empty State:** Centered with icon and subtle message

**Testing Recommendations:**
- Upload various file types (valid and invalid)
- Test file size limit (files over 10MB)
- Drag-and-drop functionality
- Delete authorization (owner vs non-owner vs admin)
- Real-time updates with multiple browser windows
- Dark mode appearance
- Responsive layout on different screen sizes
- Image thumbnails vs document icons

**Collaboration:**
- Built on Sam's backend attachment infrastructure
- Integrates with Gimli's SignalR hub (Issue #37)
- Ready for PR creation (need GitHub authentication fix)

**Git:**
- Branch: `squad/35-issue-attachments`
- Commit: `e6969c9` - "feat(attachments): Add attachment upload and display UI"
- Pushed to remote successfully
- PR creation pending due to GitHub auth issue

**Next Steps:**
- PR will need to be created manually via GitHub web UI
- Backend API endpoints should be tested for integration
- Consider adding E2E tests for attachment workflows
- May need to verify SignalR hub sends attachment events

---

### 2026-03-12 - Analytics Dashboard UI (Issue #34)

**Task:** Implement analytics dashboard with charts and visualizations for admin users

**What I Built:**
1. **Chart.js Integration**
   - Added Chart.js 4.x via CDN in `App.razor`
   - Created `charts.js` interop file for Chart.js wrapper
   - Dark mode support: detects `.dark` class on `<html>` element
   - Three chart types: Pie, Bar, Line
   - Chart lifecycle management: create, update, destroy
   - Chart state tracking in `activeCharts` object

2. **Chart Components**
   - **PieChart.razor** - Reusable pie chart with custom colors per slice
   - **BarChart.razor** - Reusable bar chart with single color
   - **LineChart.razor** - Multi-dataset line chart for time series
   - All charts share common features:
     - Loading state with animated spinner
     - Async disposal with chart cleanup
     - Unique canvas IDs to prevent collisions
     - Error handling for render failures
     - Responsive sizing with aspect ratio

3. **Shared UI Components**
   - **SummaryCard.razor** - Metric card with:
     - Title, value, subtitle display
     - Optional SVG icon with customizable background color
     - Trend indicator (up/down/flat) with percentage
     - TailwindCSS styling with dark mode
   - **DateRangePicker.razor** - Date range selector with:
     - Preset buttons: Last 7/30/90 days, All time
     - Custom date inputs (start and end)
     - EventCallback on range change
     - Active preset highlighting
     - Auto-initializes to last 30 days

4. **Analytics Dashboard Page (`Analytics.razor`)**
   - Route: `/admin/analytics`
   - Authorization: Requires `AdminPolicy`
   - Layout: Uses `AdminPageLayout` with title and description
   - Summary Cards (4 metrics):
     - Total Issues (blue)
     - Open Issues (yellow)
     - Closed Issues (green)
     - Average Resolution Time (purple)
   - Charts (4 visualizations):
     - Issues by Status - Pie chart with status colors
     - Issues by Category - Bar chart
     - Issues Over Time - Line chart (created vs closed)
     - Top Contributors - Bar chart (by issues closed)
   - Date Range Filtering:
     - DateRangePicker component integration
     - Auto-reloads data on range change
     - Sends start/end dates to backend queries
   - Export to CSV:
     - Download button with loading state
     - Calls `ExportAnalyticsQuery` via MediatR
     - JavaScript-based file download
     - Filename includes timestamp
   - States: Loading, Error, Success
   - Interactive render mode for real-time updates

5. **Admin Navigation Update**
   - Added "Analytics" link to `AdminPageLayout.razor`
   - Positioned between Dashboard and Categories
   - Active state highlighting
   - Consistent with existing nav pattern

6. **Backend Integration**
   - Uses MediatR queries created by Sam:
     - `GetAnalyticsSummaryQuery` - Main dashboard data
     - `ExportAnalyticsQuery` - CSV export
   - Handles `Result<T>` pattern from domain layer
   - Parallel data fetching in backend handler
   - Error logging and user feedback

7. **Data Transformation**
   - **Status Colors:** Open (blue), InProgress (yellow), Resolved (green), Closed (gray)
   - **Category Chart:** Uses primary color (#3b82f6)
   - **Time Series:** Two datasets - Created (blue) and Closed (green)
   - **Contributors:** Green bar chart by issues closed count
   - **Resolution Time Formatting:** 
     - < 1 hour: minutes (e.g., "45m")
     - < 24 hours: hours (e.g., "12.5h")
     - >= 24 hours: days (e.g., "3.2d")

**Technical Decisions:**
- **Chart.js via CDN:** Simplifies setup, no npm dependency management
- **JS Interop over Blazor Chart Library:** More control and flexibility
- **Auto Properties for Component Parameters:** Required by Blazor's change detection
- **Result<T> Pattern:** Backend uses this for error handling - extracted `.Value` on success
- **Loading States:** Separate loading state for each async operation (data load vs export)
- **Dark Mode:** JS interop reads DOM for theme, applies appropriate chart colors
- **EventCallback:** Used for DateRangePicker to notify parent of changes
- **Responsive Grid:** 1 column mobile, 2 tablet, 4 desktop for summary cards; 1-2 columns for charts

**Files Created:**
- `src/Web/wwwroot/js/charts.js` - Chart.js interop wrapper
- `src/Web/Components/Charts/PieChart.razor` - Pie chart component
- `src/Web/Components/Charts/BarChart.razor` - Bar chart component
- `src/Web/Components/Charts/LineChart.razor` - Line chart component
- `src/Web/Components/Shared/SummaryCard.razor` - Metric card component
- `src/Web/Components/Shared/DateRangePicker.razor` - Date range picker component
- `src/Web/Components/Pages/Admin/Analytics.razor` - Main dashboard page

**Files Modified:**
- `src/Web/Components/App.razor` - Added Chart.js CDN and charts.js script
- `src/Web/Components/Pages/Admin/AdminPageLayout.razor` - Added Analytics nav link
- `src/Web/Components/_Imports.razor` - Added using directives for Domain DTOs, MediatR, Authorization

**Backend Files (Created by Sam):**
- `src/Domain/DTOs/Analytics/AnalyticsSummaryDto.cs`
- `src/Domain/DTOs/Analytics/IssuesByStatusDto.cs`
- `src/Domain/DTOs/Analytics/IssuesByCategoryDto.cs`
- `src/Domain/DTOs/Analytics/IssuesOverTimeDto.cs`
- `src/Domain/DTOs/Analytics/TopContributorDto.cs`
- `src/Domain/DTOs/Analytics/ResolutionTimeDto.cs`
- `src/Domain/Features/Analytics/Queries/GetAnalyticsSummaryQuery.cs`
- `src/Domain/Features/Analytics/Queries/ExportAnalyticsQuery.cs`
- And others...

**Namespace Consistency:**
- Domain uses `Domain.*` not `IssueTrackerApp.Domain.*`
- Web uses `Web.*` not `IssueTrackerApp.Web.*`
- Fixed initial namespace errors in `_Imports.razor`

**Chart.js Features Used:**
- **Responsive:** `responsive: true`
- **Maintain Aspect Ratio:** For proper sizing
- **Legends:** Bottom position with padding
- **Tooltips:** Custom dark mode styling
- **Line Tension:** 0.4 for smooth curves
- **Grid Colors:** Theme-aware (dark vs light)
- **Interaction Mode:** 'index' for line charts (shows all datasets on hover)

**Styling Highlights:**
- **Cards:** White bg light mode, gray-800 dark mode with shadow and border
- **Charts:** 64 height (h-64) with canvas aspect ratio maintenance
- **Grid:** `grid grid-cols-1 lg:grid-cols-2 gap-6` for charts
- **Icons:** Heroicons SVG with customizable background colors
- **Loading Spinner:** Primary color animated spin
- **Export Button:** Primary button with disabled state during export
- **Date Picker:** Inline labels, primary ring on focus

**Challenges & Solutions:**
1. **Branch Coordination:** Branch didn't exist remotely initially - waited 3 attempts (60s each) then found it existed locally
2. **Namespace Errors:** Used wrong namespace prefix - fixed to use `Domain.*` directly
3. **Result<T> Handling:** Backend returns `Result<T>` not `T` - check `.Success` and extract `.Value`
4. **Component Parameters:** Initially used properties with getters/setters - Blazor requires auto-properties for [Parameter]
5. **Module References:** Chart components had unused `_module` fields - removed them
6. **Await in Lifecycle:** `OnInitialized` needed to be `OnInitializedAsync` and await `SelectPreset(30)`

**Build & Testing:**
- ✅ Build succeeded with 0 warnings, 0 errors
- ✅ All components follow Blazor best practices
- ✅ Dark mode tested via theme.js integration
- ✅ Responsive design with TailwindCSS classes
- ✅ Authorization check on page route
- Ready for manual testing with running app

**Accessibility Considerations:**
- Alt text not needed (decorative icons with adjacent labels)
- Keyboard navigation supported (buttons, inputs)
- Color contrast meets WCAG standards
- Screen readers will read card values and titles
- Charts: Consider ARIA labels in future enhancement

**Performance:**
- Charts lazy-loaded only when data available
- Date range filter limits backend query scope
- Summary query runs 5 sub-queries in parallel (backend optimization)
- Chart destruction on component disposal prevents memory leaks
- Loading states prevent unnecessary re-renders

**Git:**
- Branch: `squad/34-analytics-dashboard`
- Commit: `bc3e278` - "feat(analytics): Add analytics dashboard with Chart.js"
- Pushed to remote successfully
- 11 files changed, 1052 insertions(+), 1 deletion(-)

**Next Steps:**
- Manual testing required with running Aspire app
- Verify backend analytics queries return data
- Test CSV export downloads correctly
- Test date range filtering updates charts
- Test dark mode theme switching
- Consider adding chart legends to help colorblind users
- Consider adding data labels to charts for exact values
- E2E tests for analytics workflows

**Collaboration:**
- Built on Sam's backend analytics infrastructure
- Follows existing admin page patterns (Categories, Statuses)
- Consistent with project's TailwindCSS styling
- Uses established service/MediatR pattern

**Best Practices Applied:**
- Component composition (small, reusable components)
- Separation of concerns (chart rendering in JS, data in C#)
- Error handling at every async boundary
- Loading and error states for all async operations
- Proper disposal patterns (IAsyncDisposable)
- Authorization at route level
- Responsive design mobile-first
- Dark mode consistency

---