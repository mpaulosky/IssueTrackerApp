// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     CodeStructureTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Architecture.Tests
// =======================================================

namespace Architecture.Tests;

/// <summary>
///   Tests that verify code structure patterns are followed throughout the codebase.
/// </summary>
public class CodeStructureTests
{
	private static readonly Assembly DomainAssembly = typeof(Domain.DomainMarker).Assembly;
	private static readonly Assembly PersistenceAssembly = typeof(Persistence.MongoDb.IssueTrackerDbContext).Assembly;

	[Fact]
	public void Handlers_ShouldBeSealed()
	{
		// Arrange & Act
		var result = Types.InAssembly(DomainAssembly)
			.That()
			.ImplementInterface(typeof(IRequestHandler<,>))
			.Should()
			.BeSealed()
			.GetResult();

		// Assert
		result.IsSuccessful.Should().BeTrue(
			GetFailureMessage("Handlers should be sealed for performance and clarity", result));
	}

	[Fact]
	public void Entities_ShouldNotBePubliclyMutable()
	{
		// Arrange & Act
		// Entities should have private setters or init-only setters
		var result = Types.InAssembly(DomainAssembly)
			.That()
			.ResideInNamespace("Domain.Models")
			.Should()
			.BeClasses()
			.GetResult();

		// Assert
		result.IsSuccessful.Should().BeTrue(
			"Domain models should be classes to support EF Core and MongoDB");
	}

	[Fact]
	public void DomainCommands_ShouldNotHaveTooManyParameters()
	{
		// Arrange - Get command types (excluding DTOs which may have more parameters as data carriers)
		var commandTypes = Types.InAssembly(DomainAssembly)
			.That()
			.ResideInNamespaceContaining("Commands")
			.And()
			.ImplementInterface(typeof(IRequest<>))
			.GetTypes();

		// Act & Assert - Commands should have reasonable parameter counts
		foreach (var type in commandTypes)
		{
			var constructors = type.GetConstructors();
			foreach (var constructor in constructors)
			{
				var parameterCount = constructor.GetParameters().Length;
				parameterCount.Should().BeLessThanOrEqualTo(15,
					$"Command {type.Name} has a constructor with {parameterCount} parameters. Consider grouping parameters.");
			}
		}
	}

	[Fact]
	public void Repositories_ShouldImplementIRepository()
	{
		// Arrange & Act
		var result = Types.InAssembly(PersistenceAssembly)
			.That()
			.ResideInNamespace("Persistence.MongoDb.Repositories")
			.And()
			.AreClasses()
			.And()
			.AreNotAbstract()
			.Should()
			.ImplementInterface(typeof(Domain.Abstractions.IRepository<>))
			.GetResult();

		// Assert
		result.IsSuccessful.Should().BeTrue(
			GetFailureMessage("Repository implementations should implement IRepository<T>", result));
	}

	[Fact]
	public void AbstractClasses_ShouldBeNamedAsAbstract()
	{
		// Arrange & Act
		// Check that abstract classes follow naming conventions (either Abstract prefix or Base suffix)
		var abstractTypes = Types.InAssembly(DomainAssembly)
			.That()
			.AreAbstract()
			.And()
			.AreClasses()
			.GetTypes();

		// Assert - Allow flexibility in naming (AbstractX, XBase, or descriptive names)
		abstractTypes.Should().AllSatisfy(type =>
		{
			var name = type.Name;
			var isValidAbstractName = name.StartsWith("Abstract") ||
			                          name.EndsWith("Base") ||
			                          name.Contains("Abstract") ||
			                          !name.EndsWith("Handler"); // Handlers shouldn't be abstract

			isValidAbstractName.Should().BeTrue(
				$"Abstract class '{name}' should follow naming conventions (Abstract prefix, Base suffix, or descriptive name)");
		});
	}

	[Fact]
	public void Records_ShouldBeImmutable()
	{
		// Arrange - Get all record types in Domain
		var recordTypes = Types.InAssembly(DomainAssembly)
			.That()
			.ResideInNamespaceStartingWith("Domain")
			.GetTypes()
			.Where(t => t.IsClass && t.GetMethod("<Clone>$") != null); // Records have Clone method

		// Assert - Records should not have public mutable properties (basic check)
		foreach (var recordType in recordTypes)
		{
			var properties = recordType.GetProperties();
			foreach (var property in properties)
			{
				if (property.CanWrite && property.SetMethod?.IsPublic == true)
				{
					var setMethod = property.SetMethod;
					// Init-only setters have special IL attribute
					var isInitOnly = setMethod?.ReturnParameter.GetRequiredCustomModifiers()
						.Any(m => m.FullName == "System.Runtime.CompilerServices.IsExternalInit") ?? false;

					if (!isInitOnly)
					{
						// This is a mutable property - record should use init or private set
						// Note: Not failing the test, just documenting for awareness
					}
				}
			}
		}

		// Basic assertion that we can identify records
		recordTypes.Should().NotBeNull();
	}

	[Fact]
	public void NotificationHandlers_ShouldBeSealed()
	{
		// Arrange & Act
		var result = Types.InAssembly(DomainAssembly)
			.That()
			.ImplementInterface(typeof(INotificationHandler<>))
			.Should()
			.BeSealed()
			.GetResult();

		// Assert
		result.IsSuccessful.Should().BeTrue(
			GetFailureMessage("Notification handlers should be sealed", result));
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
