// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     GlobalUsings.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests.Bunit
// =======================================================

// Testing
// System
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading;
global using System.Threading.Tasks;
// bUnit
global using Bunit;
global using Bunit.TestDoubles;
// Application
global using Domain.Abstractions;
global using Domain.DTOs;
global using Domain.DTOs.Analytics;
global using Domain.Features.Issues;

global using FluentAssertions;

global using Microsoft.AspNetCore.Components;
global using Microsoft.AspNetCore.Components.Forms;
// Microsoft
global using Microsoft.Extensions.DependencyInjection;

global using NSubstitute;

global using Web.Components.Issues;
global using Web.Components.Pages.Admin;
global using Web.Services;

global using Xunit;
