// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     NamingConventionTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Architecture.Tests
// =======================================================

namespace Architecture.Tests;

/// <summary>
///   Tests that verify naming conventions are followed throughout the codebase.
/// </summary>
public class NamingConventionTests
{
	private static readonly Assembly DomainAssembly = typeof(Domain.DomainMarker).Assembly;

	[Fact]
	public void Commands_ShouldEndWithCommand()
	{
		// Arrange & Act
		var result = Types.InAssembly(DomainAssembly)
			.That()
			.ImplementInterface(typeof(IRequest<>))
			.And()
			.ResideInNamespaceContaining("Commands")
			.Should()
			.HaveNameEndingWith("Command")
			.GetResult();

		// Assert
		result.IsSuccessful.Should().BeTrue(
			GetFailureMessage("Commands should end with 'Command'", result));
	}

	[Fact]
	public void Queries_ShouldEndWithQuery()
	{
		// Arrange & Act
		var result = Types.InAssembly(DomainAssembly)
			.That()
			.ImplementInterface(typeof(IRequest<>))
			.And()
			.ResideInNamespaceContaining("Queries")
			.Should()
			.HaveNameEndingWith("Query")
			.GetResult();

		// Assert
		result.IsSuccessful.Should().BeTrue(
			GetFailureMessage("Queries should end with 'Query'", result));
	}

	[Fact]
	public void Validators_ShouldEndWithValidator()
	{
		// Arrange & Act
		var result = Types.InAssembly(DomainAssembly)
			.That()
			.Inherit(typeof(AbstractValidator<>))
			.Should()
			.HaveNameEndingWith("Validator")
			.GetResult();

		// Assert
		result.IsSuccessful.Should().BeTrue(
			GetFailureMessage("Validators should end with 'Validator'", result));
	}

	[Fact]
	public void DTOs_WithDtoInName_ShouldEndWithDto()
	{
		// Arrange & Act
		// Check only types that are clearly DTOs (exclude request/response/paged types which are utility classes)
		var result = Types.InAssembly(DomainAssembly)
			.That()
			.ResideInNamespace("Domain.DTOs")
			.And()
			.AreClasses()
			.And()
			.DoNotHaveNameMatching(".*Request$")
			.And()
			.DoNotHaveNameMatching(".*Response.*")
			.And()
			.DoNotHaveNameMatching(".*Result.*")
			.Should()
			.HaveNameEndingWith("Dto")
			.GetResult();

		// Assert
		result.IsSuccessful.Should().BeTrue(
			GetFailureMessage("Entity DTOs in Domain.DTOs namespace should end with 'Dto'", result));
	}

	[Fact]
	public void CommandHandlers_ShouldEndWithHandler()
	{
		// Arrange & Act
		var result = Types.InAssembly(DomainAssembly)
			.That()
			.ImplementInterface(typeof(IRequestHandler<,>))
			.Should()
			.HaveNameEndingWith("Handler")
			.GetResult();

		// Assert
		result.IsSuccessful.Should().BeTrue(
			GetFailureMessage("Command handlers should end with 'Handler'", result));
	}

	[Fact]
	public void Interfaces_ShouldStartWithI()
	{
		// Arrange & Act
		var result = Types.InAssembly(DomainAssembly)
			.That()
			.AreInterfaces()
			.Should()
			.HaveNameStartingWith("I")
			.GetResult();

		// Assert
		result.IsSuccessful.Should().BeTrue(
			GetFailureMessage("Interfaces should start with 'I'", result));
	}

	private static string GetFailureMessage(string rule, TestResult result)
	{
		if (result.IsSuccessful)
		{
			return rule;
		}

		var failingTypes = result.FailingTypeNames ?? [];
		return $"{rule}. Failing types: {string.Join(", ", failingTypes)}";
	}
}
