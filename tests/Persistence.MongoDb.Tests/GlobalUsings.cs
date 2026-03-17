// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     GlobalUsings.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.MongoDb.Tests
// =======================================================

// Testing
// System
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Linq.Expressions;
global using System.Threading;
global using System.Threading.Tasks;
// Domain
global using Domain.Abstractions;
global using Domain.Models;

global using FluentAssertions;
// Entity Framework Core
global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
// Microsoft Extensions
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Logging.Abstractions;
global using Microsoft.Extensions.Options;
// MongoDB
global using MongoDB.Bson;

global using NSubstitute;
// Project Under Test
global using Persistence.MongoDb;
global using Persistence.MongoDb.Configurations;
global using Persistence.MongoDb.Repositories;

global using Xunit;
