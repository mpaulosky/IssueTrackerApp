// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     GlobalUsings.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.AzureStorage.Tests
// =======================================================

// Testing
// System
global using System;
global using System.IO;
global using System.Threading;
global using System.Threading.Tasks;
// Azure Storage
global using Azure.Storage.Blobs;
global using Azure.Storage.Blobs.Models;
// Domain
global using Domain.Abstractions;
global using Domain.Models;

global using FluentAssertions;

global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
// Microsoft Extensions
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Logging.Abstractions;
global using Microsoft.Extensions.Options;

global using NSubstitute;
// Project Under Test
global using Persistence.AzureStorage;

global using Xunit;
