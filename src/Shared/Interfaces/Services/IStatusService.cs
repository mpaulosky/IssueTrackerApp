// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     IStatusService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.Services
// =============================================

namespace Shared.Interfaces.Services;

public interface IStatusService
{
	Task ArchiveStatus(StatusModel status);

	Task CreateStatus(StatusModel status);

	Task<StatusModel> GetStatus(string statusId);

	Task<List<StatusModel>> GetStatuses();

	Task UpdateStatus(StatusModel status);
}
