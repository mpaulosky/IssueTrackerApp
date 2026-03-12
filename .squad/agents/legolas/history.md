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