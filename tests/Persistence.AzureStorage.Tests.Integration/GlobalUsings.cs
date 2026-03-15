// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     GlobalUsings.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.AzureStorage.Tests.Integration
// =======================================================

// Testing
global using Xunit;
global using FluentAssertions;

// System
global using System;
global using System.IO;
global using System.Threading;
global using System.Threading.Tasks;

// Microsoft Extensions
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Logging.Abstractions;
global using Microsoft.Extensions.Options;

// Azure Storage
global using Azure.Storage.Blobs;
global using Azure.Storage.Blobs.Models;

// Testcontainers
global using Testcontainers.Azurite;

// Project Under Test
global using Persistence.AzureStorage;

// Domain
global using Domain.Abstractions;
global using Domain.Models;
