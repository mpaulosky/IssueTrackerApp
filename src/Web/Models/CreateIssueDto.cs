// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     CreateIssueDto.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.UI
// =============================================

namespace Web.Models;

public class CreateIssueDto
{
	[Required] [MaxLength(75)] public string? Title { get; set; }

	[Required] [MaxLength(500)] public string? Description { get; set; }

	[Required] public string? CategoryId { get; set; }
}