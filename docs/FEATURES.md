# Feature Documentation

This document provides detailed documentation for all features in IssueTrackerApp, including routes, components, and usage notes.

---

## Table of Contents

- [Issue Management](#issue-management)
- [User Dashboard](#user-dashboard)
- [Search and Filtering](#search-and-filtering)
- [Comments](#comments)
- [Category Management](#category-management)
- [Status Management](#status-management)

---

## Issue Management

Full CRUD (Create, Read, Update, Delete) functionality for tracking issues.

### Routes

| Route | Description | Authorization |
|-------|-------------|---------------|
| `/issues` | List all issues with search and filters | User |
| `/issues/create` | Create a new issue | User |
| `/issues/{id}` | View issue details with comments | User |
| `/issues/{id}/edit` | Edit an existing issue | User (owner) or Admin |

### Key Components

| Component | Location | Purpose |
|-----------|----------|---------|
| `Index.razor` | `Pages/Issues/` | Issue list with search, filters, and pagination |
| `Create.razor` | `Pages/Issues/` | Issue creation form with validation |
| `Details.razor` | `Pages/Issues/` | Issue details view with comments section |
| `Edit.razor` | `Pages/Issues/` | Issue edit form with validation |

### Features

- **Create Issue**: Title, description, category, and status selection
- **View Issue**: Full details with comments, status badges, and category tags
- **Edit Issue**: Update all fields with optimistic concurrency handling
- **Delete Issue**: Soft delete with confirmation modal
- **Archive/Restore**: Admin can archive and restore issues

### CQRS Commands & Queries

```
Domain/Features/Issues/
├── Commands/
│   ├── CreateIssueCommand.cs      # Create new issue
│   ├── UpdateIssueCommand.cs      # Update existing issue
│   ├── DeleteIssueCommand.cs      # Delete/archive issue
│   └── ChangeIssueStatusCommand.cs # Change issue status
├── Queries/
│   ├── GetIssueByIdQuery.cs       # Get single issue
│   ├── GetIssuesQuery.cs          # Get paginated list
│   └── SearchIssuesQuery.cs       # Full-text search with filters
└── Validators/
    ├── CreateIssueCommandValidator.cs
    └── UpdateIssueCommandValidator.cs
```

---

## User Dashboard

Personal analytics dashboard showing user-specific statistics and recent activity.

### Routes

| Route | Description | Authorization |
|-------|-------------|---------------|
| `/dashboard` | User's personal dashboard | User |

### Key Components

| Component | Location | Purpose |
|-----------|----------|---------|
| `Dashboard.razor` | `Pages/` | Main dashboard page with stats and actions |
| `StatusBadge.razor` | `Shared/` | Display issue status with color coding |
| `CategoryBadge.razor` | `Shared/` | Display category with styling |

### Features

- **Stats Cards**: Four metric cards showing:
  - Total Issues (all user's issues)
  - Open Issues (Open or In Progress status)
  - Resolved Issues (Resolved or Closed status)
  - This Week (issues created in last 7 days)

- **Recent Issues List**: Last 10 issues created by the user with:
  - Title with link to details
  - Status and category badges
  - Creation date

- **Quick Actions Panel**:
  - Create New Issue button
  - View All Issues link
  - Admin Dashboard link (Admin only)

- **Issue Summary Card**: Calculated statistics with percentage of open issues

### CQRS Query

```csharp
public record GetUserDashboardQuery(string UserId) : IRequest<Result<UserDashboardDto>>;
```

### Dashboard DTO

```csharp
public record UserDashboardDto(
    int TotalIssues,
    int OpenIssues,
    int ResolvedIssues,
    int ThisWeekIssues,
    IReadOnlyList<IssueDto> RecentIssues);
```

---

## Search and Filtering

Advanced search functionality with debounced input, multiple filters, and URL parameter persistence.

### Routes

| Route | Description |
|-------|-------------|
| `/issues?search=text` | Search by text |
| `/issues?status=Open` | Filter by status |
| `/issues?category=Bug` | Filter by category |
| `/issues?author=userId` | Filter by author |
| `/issues?page=2` | Pagination |

### Key Components

| Component | Location | Purpose |
|-----------|----------|---------|
| `SearchInput.razor` | `Shared/` | Debounced text search input |
| `FilterPanel.razor` | `Shared/` | Filter dropdowns for status/category |
| `Pagination.razor` | `Shared/` | Page navigation controls |
| `Index.razor` | `Pages/Issues/` | Integrates all search components |

### Features

- **Debounced Search**: 300ms delay to prevent excessive API calls
- **Multi-Filter Support**: Combine search text with status, category, and author filters
- **URL Synchronization**: All filters persist in URL query parameters
- **Bookmarkable URLs**: Share or bookmark specific search results
- **Browser Navigation**: Back/forward buttons work with filter state
- **Pagination**: Server-side pagination with configurable page size

### URL Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `search` | string | Full-text search term |
| `status` | string | Status name filter |
| `category` | string | Category name filter |
| `author` | string | Author user ID filter |
| `dateFrom` | date | Start date filter |
| `dateTo` | date | End date filter |
| `page` | int | Current page (default: 1) |
| `pageSize` | int | Items per page (default: 20) |

### Search Request Model

```csharp
public record IssueSearchRequest
{
    public string? SearchText { get; init; }
    public string? StatusFilter { get; init; }
    public string? CategoryFilter { get; init; }
    public string? AuthorId { get; init; }
    public DateOnly? DateFrom { get; init; }
    public DateOnly? DateTo { get; init; }
    public bool IncludeArchived { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
```

---

## Comments

Add, edit, and delete comments on issues to facilitate discussion and collaboration.

### Routes

Comments are displayed inline on the issue details page:

| Route | Description |
|-------|-------------|
| `/issues/{id}` | View issue with comments section |

### Key Components

| Component | Location | Purpose |
|-----------|----------|---------|
| `CommentsSection.razor` | `Issues/` | Comment list and add form |
| `Details.razor` | `Pages/Issues/` | Parent page hosting comments |

### Features

- **Add Comment**: Title and description with author auto-populated
- **Edit Comment**: Update comment text (author only)
- **Delete Comment**: Remove comment with confirmation (author or admin)
- **Author Display**: Shows commenter name and avatar
- **Timestamps**: Creation date displayed for each comment

### CQRS Commands & Queries

```
Domain/Features/Comments/
├── Commands/
│   ├── AddCommentCommand.cs       # Add new comment to issue
│   ├── UpdateCommentCommand.cs    # Update existing comment
│   └── DeleteCommentCommand.cs    # Delete comment
├── Queries/
│   └── GetIssueCommentsQuery.cs   # Get comments for an issue
└── Validators/
    ├── AddCommentCommandValidator.cs
    └── UpdateCommentCommandValidator.cs
```

### Comment Model

```csharp
public class Comment
{
    public ObjectId Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public UserDto Author { get; set; }      // Embedded user
    public IssueDto Issue { get; set; }      // Reference to parent
    public DateTime DateCreated { get; set; }
    public bool Archived { get; set; }
    public bool IsAnswer { get; set; }       // Mark as accepted answer
}
```

---

## Category Management

Admin functionality for managing issue categories.

### Routes

| Route | Description | Authorization |
|-------|-------------|---------------|
| `/admin` | Admin dashboard | Admin |
| `/admin/categories` | Category management | Admin |

### Key Components

| Component | Location | Purpose |
|-----------|----------|---------|
| `Categories.razor` | `Pages/Admin/` | Category CRUD interface |
| `AdminPageLayout.razor` | `Pages/Admin/` | Shared admin layout |
| `Index.razor` | `Pages/Admin/` | Admin dashboard |

### Features

- **List Categories**: View all categories with archive status
- **Create Category**: Add new category with name and description
- **Edit Category**: Update category details
- **Archive Category**: Soft delete (can be restored)
- **Restore Category**: Unarchive previously archived category

### CQRS Commands & Queries

```
Domain/Features/Categories/
├── Commands/
│   ├── CreateCategoryCommand.cs
│   ├── UpdateCategoryCommand.cs
│   └── ArchiveCategoryCommand.cs
├── Queries/
│   ├── GetCategoryByIdQuery.cs
│   └── GetCategoriesQuery.cs
└── Validators/
    ├── CreateCategoryCommandValidator.cs
    └── UpdateCategoryCommandValidator.cs
```

---

## Status Management

Admin functionality for managing issue statuses and workflows.

### Routes

| Route | Description | Authorization |
|-------|-------------|---------------|
| `/admin` | Admin dashboard | Admin |
| `/admin/statuses` | Status management | Admin |

### Key Components

| Component | Location | Purpose |
|-----------|----------|---------|
| `Statuses.razor` | `Pages/Admin/` | Status CRUD interface |
| `AdminPageLayout.razor` | `Pages/Admin/` | Shared admin layout |
| `StatusBadge.razor` | `Shared/` | Status display component |

### Features

- **List Statuses**: View all statuses with archive status
- **Create Status**: Add new status with name and description
- **Edit Status**: Update status details
- **Archive Status**: Soft delete (can be restored)
- **Restore Status**: Unarchive previously archived status
- **Status Colors**: Visual differentiation in UI

### Default Statuses

| Status | Description |
|--------|-------------|
| Open | Newly created issue |
| In Progress | Work has started |
| Resolved | Issue has been addressed |
| Closed | Issue is complete |

### CQRS Commands & Queries

```
Domain/Features/Statuses/
├── Commands/
│   ├── CreateStatusCommand.cs
│   ├── UpdateStatusCommand.cs
│   └── ArchiveStatusCommand.cs
├── Queries/
│   ├── GetStatusByIdQuery.cs
│   └── GetStatusesQuery.cs
└── Validators/
    ├── CreateStatusCommandValidator.cs
    └── UpdateStatusCommandValidator.cs
```

---

## Authorization Policies

Features are protected by role-based authorization:

| Policy | Access Level | Features |
|--------|--------------|----------|
| `UserPolicy` | Authenticated users | Issue CRUD, Dashboard, Comments |
| `AdminPolicy` | Admin role | Category/Status management, Admin dashboard |

### Implementation

```csharp
// In Web/Auth/AuthorizationPolicies.cs
public static class AuthorizationPolicies
{
    public const string UserPolicy = "User";
    public const string AdminPolicy = "Admin";
}

// Usage in Razor
@attribute [Authorize(Policy = AuthorizationPolicies.UserPolicy)]
```

---

## Shared Components

Reusable components used across multiple features:

| Component | Purpose |
|-----------|---------|
| `StatusBadge.razor` | Colored badge for issue status |
| `CategoryBadge.razor` | Styled badge for categories |
| `SearchInput.razor` | Debounced search input field |
| `FilterPanel.razor` | Dropdown filters for lists |
| `Pagination.razor` | Page navigation controls |
| `DeleteConfirmationModal.razor` | Confirmation dialog for deletions |
