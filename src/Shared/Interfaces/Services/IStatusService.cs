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
	Task ArchiveStatus(Shared.Models.Status status);

	Task CreateStatus(Shared.Models.Status status);

	Task<Shared.Models.Status> GetStatus(string statusId);

	Task<List<Shared.Models.Status>> GetStatuses();

	Task UpdateStatus(Shared.Models.Status status);
}
