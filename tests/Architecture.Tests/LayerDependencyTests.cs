// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     LayerDependencyTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Architecture.Tests
// =======================================================

namespace Architecture.Tests;

/// <summary>
///   Tests that verify clean architecture layer dependencies are respected.
/// </summary>
public class LayerDependencyTests
{
	private static readonly Assembly DomainAssembly = typeof(Domain.DomainMarker).Assembly;
	private static readonly Assembly WebAssembly = typeof(Program).Assembly;
	private static readonly Assembly PersistenceAssembly = typeof(Persistence.MongoDb.IssueTrackerDbContext).Assembly;
	private static readonly Assembly AzureStorageAssembly = typeof(Persistence.AzureStorage.BlobStorageService).Assembly;

	[Fact]
	public void Domain_ShouldNotDependOn_Web()
	{
		// Arrange & Act
		var result = Types.InAssembly(DomainAssembly)
			.ShouldNot()
			.HaveDependencyOn("Web")
			.GetResult();

		// Assert
		result.IsSuccessful.Should().BeTrue(
			because: "Domain layer should not depend on Web layer to maintain clean architecture");
	}

	[Fact]
	public void Domain_ShouldNotDependOn_Persistence()
	{
		// Arrange & Act
		var result = Types.InAssembly(DomainAssembly)
			.ShouldNot()
			.HaveDependencyOn("Persistence.MongoDb")
			.GetResult();

		// Assert
		result.IsSuccessful.Should().BeTrue(
			because: "Domain layer should not depend on Persistence layer to maintain clean architecture");
	}

	/// <summary>
	///   Verifies that the Domain layer does not depend on Persistence.AzureStorage.
	/// </summary>
	[Fact]
	public void Domain_ShouldNotDependOn_PersistenceAzureStorage()
	{
		// Arrange & Act
		var result = Types.InAssembly(DomainAssembly)
			.ShouldNot()
			.HaveDependencyOn("Persistence.AzureStorage")
			.GetResult();

		// Assert
		result.IsSuccessful.Should().BeTrue(
			because: "Domain layer should not depend on Persistence.AzureStorage layer to maintain clean architecture");
	}

	[Fact]
	public void Domain_ShouldNotDependOn_Infrastructure()
	{
		// Arrange & Act
		var result = Types.InAssembly(DomainAssembly)
			.ShouldNot()
			.HaveDependencyOnAny(
				"Microsoft.AspNetCore",
				"Microsoft.EntityFrameworkCore",
				"Azure.Storage")
			.GetResult();

		// Assert
		result.IsSuccessful.Should().BeTrue(
			because: "Domain layer should not depend on infrastructure concerns");
	}

	[Fact]
	public void Persistence_ShouldHaveDependencyOn_Domain()
	{
		// Arrange & Act
		// Verify that at least some types in Persistence depend on Domain
		var persistenceTypes = Types.InAssembly(PersistenceAssembly)
			.That()
			.ResideInNamespaceStartingWith("Persistence.MongoDb")
			.GetTypes();

		var hasDomainDependency = persistenceTypes
			.Any(t => t.GetInterfaces()
				.Any(i => i.Namespace?.StartsWith("Domain") == true) ||
				t.GetProperties()
					.Any(p => p.PropertyType.Namespace?.StartsWith("Domain") == true) ||
				t.GetMethods()
					.Any(m => m.GetParameters()
						.Any(param => param.ParameterType.Namespace?.StartsWith("Domain") == true) ||
						m.ReturnType.Namespace?.StartsWith("Domain") == true));

		// Assert
		hasDomainDependency.Should().BeTrue(
			because: "Persistence layer should depend on Domain layer for entities and abstractions");
	}

	[Fact]
	public void Persistence_ShouldNotDependOn_Web()
	{
		// Arrange & Act
		var result = Types.InAssembly(PersistenceAssembly)
			.ShouldNot()
			.HaveDependencyOn("Web")
			.GetResult();

		// Assert
		result.IsSuccessful.Should().BeTrue(
			because: "Persistence layer should not depend on Web layer");
	}

	[Fact]
	public void Web_ShouldHaveDependencyOn_Domain()
	{
		// Arrange & Act
		// Verify that at least some types in Web depend on Domain
		var webTypes = Types.InAssembly(WebAssembly)
			.That()
			.ResideInNamespaceStartingWith("Web")
			.GetTypes();

		var hasDomainDependency = webTypes
			.Any(t => t.GetInterfaces()
				.Any(i => i.Namespace?.StartsWith("Domain") == true) ||
				t.GetProperties()
					.Any(p => p.PropertyType.Namespace?.StartsWith("Domain") == true) ||
				t.GetFields()
					.Any(f => f.FieldType.Namespace?.StartsWith("Domain") == true));

		// Assert
		hasDomainDependency.Should().BeTrue(
			because: "Web layer should depend on Domain layer for business logic");
	}

	/// <summary>
	///   Verifies that the Persistence.AzureStorage layer does not depend on Web.
	/// </summary>
	[Fact]
	public void PersistenceAzureStorage_ShouldNotDependOn_Web()
	{
		// Arrange & Act
		var result = Types.InAssembly(AzureStorageAssembly)
			.ShouldNot()
			.HaveDependencyOn("Web")
			.GetResult();

		// Assert
		result.IsSuccessful.Should().BeTrue(
			because: "Persistence.AzureStorage layer should not depend on Web layer");
	}

	/// <summary>
	///   Verifies that the Persistence.AzureStorage layer does not depend on Persistence.MongoDb.
	/// </summary>
	[Fact]
	public void PersistenceAzureStorage_ShouldNotDependOn_PersistenceMongoDb()
	{
		// Arrange & Act
		var result = Types.InAssembly(AzureStorageAssembly)
			.ShouldNot()
			.HaveDependencyOn("Persistence.MongoDb")
			.GetResult();

		// Assert
		result.IsSuccessful.Should().BeTrue(
			because: "Persistence.AzureStorage layer should not depend on Persistence.MongoDb layer");
	}

	/// <summary>
	///   Verifies that the Persistence.AzureStorage layer depends on Domain.
	/// </summary>
	[Fact]
	public void PersistenceAzureStorage_ShouldDependOn_Domain()
	{
		// Arrange & Act
		// Verify that at least some types in Persistence.AzureStorage depend on Domain
		var azureStorageTypes = Types.InAssembly(AzureStorageAssembly)
			.That()
			.ResideInNamespaceStartingWith("Persistence.AzureStorage")
			.GetTypes();

		var hasDomainDependency = azureStorageTypes
			.Any(t => t.GetInterfaces()
				.Any(i => i.Namespace?.StartsWith("Domain") == true) ||
				t.GetProperties()
					.Any(p => p.PropertyType.Namespace?.StartsWith("Domain") == true) ||
				t.GetMethods()
					.Any(m => m.GetParameters()
						.Any(param => param.ParameterType.Namespace?.StartsWith("Domain") == true) ||
						m.ReturnType.Namespace?.StartsWith("Domain") == true));

		// Assert
		hasDomainDependency.Should().BeTrue(
			because: "Persistence.AzureStorage layer should depend on Domain layer for entities and abstractions");
	}

	/// <summary>
	///   Verifies that the Persistence.MongoDb layer does not depend on Persistence.AzureStorage.
	/// </summary>
	[Fact]
	public void PersistenceMongoDb_ShouldNotDependOn_PersistenceAzureStorage()
	{
		// Arrange & Act
		var result = Types.InAssembly(PersistenceAssembly)
			.ShouldNot()
			.HaveDependencyOn("Persistence.AzureStorage")
			.GetResult();

		// Assert
		result.IsSuccessful.Should().BeTrue(
			because: "Persistence.MongoDb layer should not depend on Persistence.AzureStorage layer");
	}
}
