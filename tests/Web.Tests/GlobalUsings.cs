// Copyright (c) IssueTrackerApp. All rights reserved.
// Licensed under the MIT License.

global using System.Net;
global using FluentAssertions;
global using Microsoft.AspNetCore.Mvc.Testing;
global using NSubstitute;
global using Xunit;

// Domain
global using Domain.Abstractions;
global using Domain.DTOs;
global using Domain.Features.Issues.Commands;
global using Domain.Features.Issues.Commands.Bulk;
global using Domain.Features.Issues.Queries;
global using Domain.Features.Comments.Commands;
global using Domain.Features.Comments.Queries;
global using Domain.Features.Attachments.Commands;
global using Domain.Features.Attachments.Queries;
global using MediatR;
global using MongoDB.Bson;
global using Microsoft.Extensions.Logging;
