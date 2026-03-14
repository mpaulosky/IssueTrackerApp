// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     GlobalUsings.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain.Tests
// =======================================================

// Testing
global using Xunit;
global using FluentAssertions;
global using FluentValidation.TestHelper;
global using NSubstitute;

// System
global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using System.Linq.Expressions;
global using System.Threading;
global using System.Threading.Tasks;

// MediatR
global using MediatR;

// MongoDB
global using MongoDB.Bson;

// Microsoft Extensions
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Logging.Abstractions;

// Domain
global using Domain;
global using Domain.Abstractions;
global using Domain.DTOs;
global using Domain.Events;
global using Domain.Mappers;
global using Domain.Models;
global using Domain.Features.Comments.Commands;
global using Domain.Features.Comments.Queries;
global using Domain.Features.Comments.Validators;
global using Domain.Features.Attachments.Commands;
global using Domain.Features.Attachments.Queries;
global using Domain.Features.Attachments.Validators;
