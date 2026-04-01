// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     LabelsArchitectureTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Architecture.Tests
// =======================================================

using Domain.Features.Issues;

namespace Architecture.Tests;

/// <summary>
///   Architecture tests specific to the Labels feature slice.
/// </summary>
public class LabelsArchitectureTests
{
	private static readonly Assembly DomainAssembly = typeof(Domain.DomainMarker).Assembly;
	private static readonly Assembly WebAssembly = typeof(Program).Assembly;

	[Fact]
	public void AddLabelCommand_ShouldBeInDomainFeaturesIssuesCommandsNamespace()
	{
		// Arrange & Act
		var commandType = DomainAssembly.GetTypes()
			.FirstOrDefault(t => t.Name == "AddLabelCommand");

		// Assert
		commandType.Should().NotBeNull("AddLabelCommand should exist");
		commandType!.Namespace.Should().Be("Domain.Features.Issues.Commands",
			"AddLabelCommand should be in the Domain.Features.Issues.Commands namespace");
	}

	[Fact]
	public void RemoveLabelCommand_ShouldBeInDomainFeaturesIssuesCommandsNamespace()
	{
		// Arrange & Act
		var commandType = DomainAssembly.GetTypes()
			.FirstOrDefault(t => t.Name == "RemoveLabelCommand");

		// Assert
		commandType.Should().NotBeNull("RemoveLabelCommand should exist");
		commandType!.Namespace.Should().Be("Domain.Features.Issues.Commands",
			"RemoveLabelCommand should be in the Domain.Features.Issues.Commands namespace");
	}

	[Fact]
	public void AddLabelCommandHandler_ShouldBeSealed()
	{
		// Arrange & Act
		var handlerType = DomainAssembly.GetTypes()
			.FirstOrDefault(t => t.Name == "AddLabelCommandHandler");

		// Assert
		handlerType.Should().NotBeNull("AddLabelCommandHandler should exist");
		handlerType!.IsSealed.Should().BeTrue(
			"AddLabelCommandHandler should be sealed for performance and clarity");
	}

	[Fact]
	public void RemoveLabelCommandHandler_ShouldBeSealed()
	{
		// Arrange & Act
		var handlerType = DomainAssembly.GetTypes()
			.FirstOrDefault(t => t.Name == "RemoveLabelCommandHandler");

		// Assert
		handlerType.Should().NotBeNull("RemoveLabelCommandHandler should exist");
		handlerType!.IsSealed.Should().BeTrue(
			"RemoveLabelCommandHandler should be sealed for performance and clarity");
	}

	[Fact]
	public void AddLabelCommandHandler_ShouldImplementIRequestHandler()
	{
		// Arrange & Act
		var handlerType = DomainAssembly.GetTypes()
			.FirstOrDefault(t => t.Name == "AddLabelCommandHandler");

		// Assert
		handlerType.Should().NotBeNull("AddLabelCommandHandler should exist");
		var implementsHandler = handlerType!.GetInterfaces()
			.Any(i => i.IsGenericType &&
								i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));

		implementsHandler.Should().BeTrue(
			"AddLabelCommandHandler should implement IRequestHandler<AddLabelCommand, Result>");
	}

	[Fact]
	public void RemoveLabelCommandHandler_ShouldImplementIRequestHandler()
	{
		// Arrange & Act
		var handlerType = DomainAssembly.GetTypes()
			.FirstOrDefault(t => t.Name == "RemoveLabelCommandHandler");

		// Assert
		handlerType.Should().NotBeNull("RemoveLabelCommandHandler should exist");
		var implementsHandler = handlerType!.GetInterfaces()
			.Any(i => i.IsGenericType &&
								i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));

		implementsHandler.Should().BeTrue(
			"RemoveLabelCommandHandler should implement IRequestHandler<RemoveLabelCommand, Result>");
	}

	[Fact]
	public void AddLabelCommand_ShouldNotDependOn_Web()
	{
		// Arrange & Act
		var commandType = DomainAssembly.GetTypes()
			.FirstOrDefault(t => t.Name == "AddLabelCommand");

		// Assert
		commandType.Should().NotBeNull("AddLabelCommand should exist");
		var dependencies = commandType!.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
			.Select(f => f.FieldType.Namespace)
			.Concat(commandType.GetProperties().Select(p => p.PropertyType.Namespace))
			.Where(ns => ns != null);

		dependencies.Should().NotContain(ns => ns!.StartsWith("Web"),
			"AddLabelCommand should not depend on Web project");
	}

	[Fact]
	public void RemoveLabelCommand_ShouldNotDependOn_Web()
	{
		// Arrange & Act
		var commandType = DomainAssembly.GetTypes()
			.FirstOrDefault(t => t.Name == "RemoveLabelCommand");

		// Assert
		commandType.Should().NotBeNull("RemoveLabelCommand should exist");
		var dependencies = commandType!.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
			.Select(f => f.FieldType.Namespace)
			.Concat(commandType.GetProperties().Select(p => p.PropertyType.Namespace))
			.Where(ns => ns != null);

		dependencies.Should().NotContain(ns => ns!.StartsWith("Web"),
			"RemoveLabelCommand should not depend on Web project");
	}

	[Fact]
	public void AddLabelCommand_ShouldNotDependOn_Persistence()
	{
		// Arrange & Act
		var commandType = DomainAssembly.GetTypes()
			.FirstOrDefault(t => t.Name == "AddLabelCommand");

		// Assert
		commandType.Should().NotBeNull("AddLabelCommand should exist");
		var dependencies = commandType!.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
			.Select(f => f.FieldType.Namespace)
			.Concat(commandType.GetProperties().Select(p => p.PropertyType.Namespace))
			.Where(ns => ns != null);

		dependencies.Should().NotContain(ns => ns!.StartsWith("Persistence"),
			"AddLabelCommand should not depend on Persistence projects");
	}

	[Fact]
	public void RemoveLabelCommand_ShouldNotDependOn_Persistence()
	{
		// Arrange & Act
		var commandType = DomainAssembly.GetTypes()
			.FirstOrDefault(t => t.Name == "RemoveLabelCommand");

		// Assert
		commandType.Should().NotBeNull("RemoveLabelCommand should exist");
		var dependencies = commandType!.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
			.Select(f => f.FieldType.Namespace)
			.Concat(commandType.GetProperties().Select(p => p.PropertyType.Namespace))
			.Where(ns => ns != null);

		dependencies.Should().NotContain(ns => ns!.StartsWith("Persistence"),
			"RemoveLabelCommand should not depend on Persistence projects");
	}

	[Fact]
	public void IssueModel_LabelsProperty_ShouldBeMutableList()
	{
		// Arrange & Act
		var issueType = DomainAssembly.GetTypes()
			.FirstOrDefault(t => t.Name == "Issue" && t.Namespace == "Domain.Models");

		// Assert
		issueType.Should().NotBeNull("Issue model should exist in Domain.Models");
		var labelsProperty = issueType!.GetProperty("Labels");
		labelsProperty.Should().NotBeNull("Issue model should have a Labels property");

		var propertyType = labelsProperty!.PropertyType;
		propertyType.Should().Be(typeof(List<string>),
			"Issue model Labels property should be List<string> for mutability");
	}

	[Fact]
	public void IssueDto_LabelsProperty_ShouldBeImmutableList()
	{
		// Arrange & Act
		var issueDtoType = DomainAssembly.GetTypes()
			.FirstOrDefault(t => t.Name == "IssueDto" && t.Namespace == "Domain.DTOs");

		// Assert
		issueDtoType.Should().NotBeNull("IssueDto should exist in Domain.DTOs");
		
		// For records, check constructor parameters
		var constructor = issueDtoType!.GetConstructors()
			.FirstOrDefault(c => c.GetParameters().Any(p => p.Name == "Labels"));

		constructor.Should().NotBeNull("IssueDto should have a constructor with Labels parameter");
		
		var labelsParameter = constructor!.GetParameters()
			.FirstOrDefault(p => p.Name == "Labels");

		labelsParameter.Should().NotBeNull("IssueDto constructor should have Labels parameter");

		var parameterType = labelsParameter!.ParameterType;
		parameterType.Should().Be(typeof(IReadOnlyList<string>),
			"IssueDto Labels property should be IReadOnlyList<string> for immutability");
	}

	[Fact]
	public void ILabelService_ShouldBeInDomainNamespace_IfExists()
	{
		// Arrange & Act
		var serviceType = DomainAssembly.GetTypes()
			.FirstOrDefault(t => t.Name == "ILabelService");

		// Assert - Only test if the type exists (may not be on main yet)
		if (serviceType != null)
		{
			serviceType.Namespace.Should().Be("Domain.Features.Issues",
				"ILabelService interface should be in Domain.Features.Issues namespace");
		}
	}

	[Fact]
	public void LabelService_ShouldBeInWebNamespace_IfExists()
	{
		// Arrange & Act
		var serviceType = WebAssembly.GetTypes()
			.FirstOrDefault(t => t.Name == "LabelService");

		// Assert - Only test if the type exists (may not be on main yet)
		if (serviceType != null)
		{
			serviceType.Namespace.Should().Be("Web.Services",
				"LabelService implementation should be in Web.Services namespace");
		}
	}

	[Fact]
	public void LabelService_ShouldImplementILabelService_IfExists()
	{
		// Arrange & Act
		var serviceType = WebAssembly.GetTypes()
			.FirstOrDefault(t => t.Name == "LabelService");

		// Assert - Only test if the type exists (may not be on main yet)
		if (serviceType != null)
		{
			var implementsInterface = serviceType.GetInterfaces()
				.Any(i => i.Name == "ILabelService");

			implementsInterface.Should().BeTrue(
				"LabelService should implement ILabelService");
		}
	}

	[Fact]
	public void LabelService_ShouldBeSealed_IfExists()
	{
		// Arrange & Act
		var serviceType = WebAssembly.GetTypes()
			.FirstOrDefault(t => t.Name == "LabelService");

		// Assert - Only test if the type exists (may not be on main yet)
		if (serviceType != null)
		{
			serviceType.IsSealed.Should().BeTrue(
				"LabelService should be sealed for performance and clarity");
		}
	}

	[Fact]
	public void LabelCommandHandlers_ShouldReturnResultOfIssueDto()
	{
		// Arrange & Act
		var addHandlerType = DomainAssembly.GetTypes()
			.FirstOrDefault(t => t.Name == "AddLabelCommandHandler");
		var removeHandlerType = DomainAssembly.GetTypes()
			.FirstOrDefault(t => t.Name == "RemoveLabelCommandHandler");

		// Assert
		addHandlerType.Should().NotBeNull("AddLabelCommandHandler should exist");
		removeHandlerType.Should().NotBeNull("RemoveLabelCommandHandler should exist");

		// Check AddLabelCommandHandler
		var addHandlerInterface = addHandlerType!.GetInterfaces()
			.FirstOrDefault(i => i.IsGenericType &&
													 i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));
		addHandlerInterface.Should().NotBeNull();
		
		var addResponseType = addHandlerInterface!.GetGenericArguments()[1];
		addResponseType.Name.Should().Contain("Result",
			"AddLabelCommandHandler should return a Result type");

		// Check RemoveLabelCommandHandler
		var removeHandlerInterface = removeHandlerType!.GetInterfaces()
			.FirstOrDefault(i => i.IsGenericType &&
													 i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));
		removeHandlerInterface.Should().NotBeNull();
		
		var removeResponseType = removeHandlerInterface!.GetGenericArguments()[1];
		removeResponseType.Name.Should().Contain("Result",
			"RemoveLabelCommandHandler should return a Result type");
	}
}
