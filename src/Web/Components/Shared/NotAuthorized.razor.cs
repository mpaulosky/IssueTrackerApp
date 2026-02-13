// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     NotAuthorized.razor.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.UI
// =============================================

namespace Web.Components.Shared;

/// <summary>
///   NotAuthorized class
/// </summary>
/// <seealso cref="Microsoft.AspNetCore.Components.ComponentBase" />
public partial class NotAuthorized
{
	/// <summary>
	///   Closes the page method.
	/// </summary>
	private void ClosePage() { NavManager.NavigateTo("/"); }
}