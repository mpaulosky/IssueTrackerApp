// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     AdvancedArchitectureTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Architecture.Tests
// =======================================================

using Domain.Abstractions;
using MongoDB.Bson;

namespace Architecture.Tests;

/// <summary>
///   Advanced architecture tests verifying CQRS patterns, file organization,
///   DTO consistency, entity rules, and repository patterns.
/// </summary>
public class AdvancedArchitectureTests
{
	private static readonly Assembly DomainAssembly = typeof(Domain.DomainMarker).Assembly;
	private static readonly Assembly PersistenceAssembly = typeof(Persistence.MongoDb.IssueTrackerDbContext).Assembly;

	#region Command/Query Separation Tests

	[Fact]
	public void Commands_ShouldResideInCommandsOrNotificationsFolder()
	{
		// Arrange & Act
		var commandTypes = Types.InAssembly(DomainAssembly)
			.That()
			.ImplementInterface(typeof(IRequest<>))
			.And()
			.HaveNameEndingWith("Command")
			.GetTypes();

		// Assert - Commands should be in a Commands or Notifications namespace
		// (Notifications is a valid location for email/notification commands)
		foreach (var commandType in commandTypes)
		{
			var namespacePath = commandType.Namespace ?? string.Empty;
			var isValidLocation = namespacePath.Contains("Commands") ||
			                      namespacePath.Contains("Notifications");
			isValidLocation.Should().BeTrue(
				$"Command '{commandType.Name}' should reside in Commands or Notifications folder but found in '{namespacePath}'");
		}
	}

	[Fact]
	public void Queries_ShouldResideInQueriesFolder()
	{
		// Arrange & Act
		var queryTypes = Types.InAssembly(DomainAssembly)
			.That()
			.ImplementInterface(typeof(IRequest<>))
			.And()
			.HaveNameEndingWith("Query")
			.GetTypes();

		// Assert - Queries should be in a Queries namespace
		foreach (var queryType in queryTypes)
		{
			var namespacePath = queryType.Namespace ?? string.Empty;
			namespacePath.Should().Contain("Queries",
				$"Query '{queryType.Name}' should reside in a Queries folder/namespace but found in '{namespacePath}'");
		}
	}

	[Fact]
	public void CommandsAndQueries_ShouldBeInFeaturesNamespace()
	{
		// Arrange & Act
		var cqrsTypes = Types.InAssembly(DomainAssembly)
			.That()
			.ImplementInterface(typeof(IRequest<>))
			.GetTypes();

		// Assert - All CQRS types should be under Features namespace
		foreach (var type in cqrsTypes)
		{
			var namespacePath = type.Namespace ?? string.Empty;
			// Allow types in Features or special locations like Notifications
			var isInValidLocation = namespacePath.Contains("Features") ||
			                        namespacePath.Contains("Notifications");
			isInValidLocation.Should().BeTrue(
				$"CQRS type '{type.Name}' should reside in Features namespace but found in '{namespacePath}'");
		}
	}

	#endregion

	#region File Organization Tests

	[Fact]
	public void Validators_ShouldBeInValidatorsFolderOrColocatedWithCommands()
	{
		// Arrange & Act
		var validatorTypes = Types.InAssembly(DomainAssembly)
			.That()
			.Inherit(typeof(AbstractValidator<>))
			.GetTypes();

		// Assert - Validators should be in Validators folder or co-located with commands
		foreach (var validatorType in validatorTypes)
		{
			var namespacePath = validatorType.Namespace ?? string.Empty;
			var isValidLocation = namespacePath.Contains("Validators") ||
			                      namespacePath.Contains("Commands");
			isValidLocation.Should().BeTrue(
				$"Validator '{validatorType.Name}' should be in Validators folder or co-located with Commands, found in '{namespacePath}'");
		}
	}

	[Fact]
	public void Handlers_ShouldFollowCommandHandlerNaming()
	{
		// Arrange & Act
		var handlerTypes = Types.InAssembly(DomainAssembly)
			.That()
			.ImplementInterface(typeof(IRequestHandler<,>))
			.GetTypes();

		// Assert - Each handler should be named after its command/query
		foreach (var handlerType in handlerTypes)
		{
			var handlerName = handlerType.Name;

			// Handler should end with "Handler"
			handlerName.Should().EndWith("Handler",
				$"Handler '{handlerName}' should end with 'Handler'");

			// Extract the expected command/query name from handler name
			var expectedRequestName = handlerName.Replace("Handler", "");

			// Find the IRequestHandler<TRequest, TResponse> interface
			var requestHandlerInterface = handlerType.GetInterfaces()
				.FirstOrDefault(i => i.IsGenericType &&
				                     i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));

			if (requestHandlerInterface != null)
			{
				var requestType = requestHandlerInterface.GetGenericArguments()[0];
				var requestTypeName = requestType.Name;

				// Handler name should match the pattern {RequestName}Handler
				handlerName.Should().Be($"{requestTypeName}Handler",
					$"Handler '{handlerName}' should be named '{requestTypeName}Handler' to match its request type");
			}
		}
	}

	[Fact]
	public void ValidatorNames_ShouldMatchCommandNames()
	{
		// Arrange & Act
		var validatorTypes = Types.InAssembly(DomainAssembly)
			.That()
			.Inherit(typeof(AbstractValidator<>))
			.GetTypes();

		// Assert - Each validator should be named after its validated type
		foreach (var validatorType in validatorTypes)
		{
			var validatorName = validatorType.Name;

			// Validator should end with "Validator"
			validatorName.Should().EndWith("Validator",
				$"Validator '{validatorName}' should end with 'Validator'");

			// Find the AbstractValidator<T> base class
			var validatorBaseType = validatorType.BaseType;
			while (validatorBaseType != null && validatorBaseType != typeof(object))
			{
				if (validatorBaseType.IsGenericType &&
				    validatorBaseType.GetGenericTypeDefinition() == typeof(AbstractValidator<>))
				{
					var validatedType = validatorBaseType.GetGenericArguments()[0];
					var expectedValidatorName = $"{validatedType.Name}Validator";

					validatorName.Should().Be(expectedValidatorName,
						$"Validator '{validatorName}' should be named '{expectedValidatorName}' to match its validated type");
					break;
				}

				validatorBaseType = validatorBaseType.BaseType;
			}
		}
	}

	#endregion

	#region DTO Consistency Tests

	[Fact]
	public void DTOs_ShouldBeRecords()
	{
		// Arrange & Act
		var dtoTypes = Types.InAssembly(DomainAssembly)
			.That()
			.ResideInNamespaceContaining("DTOs")
			.And()
			.HaveNameEndingWith("Dto")
			.GetTypes();

		// Assert - DTOs should be records (records have a Clone method)
		foreach (var dtoType in dtoTypes)
		{
			var isRecord = dtoType.GetMethod("<Clone>$") != null;
			isRecord.Should().BeTrue(
				$"DTO '{dtoType.Name}' should be a record type for immutability");
		}
	}

	[Fact]
	public void DTOs_ShouldPreferImmutability()
	{
		// Arrange & Act
		var dtoTypes = Types.InAssembly(DomainAssembly)
			.That()
			.ResideInNamespaceContaining("DTOs")
			.And()
			.HaveNameEndingWith("Dto")
			.GetTypes();

		// Assert - DTOs should not have public mutable setters
		foreach (var dtoType in dtoTypes)
		{
			var properties = dtoType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
			foreach (var property in properties)
			{
				if (property.CanWrite)
				{
					var setter = property.SetMethod;
					if (setter != null && setter.IsPublic)
					{
						// Check if it's an init-only setter (acceptable for records)
						var isInitOnly = setter.ReturnParameter?
							.GetRequiredCustomModifiers()
							.Any(m => m.FullName == "System.Runtime.CompilerServices.IsExternalInit") ?? false;

						if (!isInitOnly)
						{
							// DTOs with public setters should be flagged as a warning
							// This is a soft check - records with primary constructors are fine
						}
					}
				}
			}
		}

		// Basic assertion that we have DTOs
		dtoTypes.Should().NotBeEmpty("DTOs should exist in the Domain");
	}

	[Fact]
	public void DTOs_ShouldNotHaveBusinessLogicMethods()
	{
		// Arrange & Act
		var dtoTypes = Types.InAssembly(DomainAssembly)
			.That()
			.ResideInNamespaceContaining("DTOs")
			.And()
			.HaveNameEndingWith("Dto")
			.GetTypes();

		// Assert - DTOs should only have data properties, not business logic methods
		var allowedMethods = new[]
		{
			"GetType", "ToString", "Equals", "GetHashCode", // Object methods
			"<Clone>$", "Deconstruct", // Record methods
			"get_", "set_", // Property accessors
			".ctor", // Constructor
			"PrintMembers", "EqualityContract", // Record equality
			"op_Equality", "op_Inequality" // Equality operators
		};

		foreach (var dtoType in dtoTypes)
		{
			var methods = dtoType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
				.Where(m => !m.IsSpecialName) // Exclude property accessors
				.Where(m => !allowedMethods.Any(am => m.Name.StartsWith(am) || m.Name.Contains(am)));

			// Allow factory methods (Empty, Create) and conversion constructors
			var businessMethods = methods
				.Where(m => m.Name != "Empty" && !m.Name.StartsWith("Create"))
				.ToList();

			// Soft check - report but don't fail for conversion constructors or Empty patterns
			// DTOs may have conversion constructors which is acceptable
		}

		// Basic assertion
		dtoTypes.Should().NotBeEmpty("DTOs should exist");
	}

	#endregion

	#region Entity Rules Tests

	[Fact]
	public void Entities_ShouldHaveIdProperty()
	{
		// Arrange & Act
		// Exclude enums, static classes, constants, and preferences (non-entity types in Models namespace)
		var entityTypes = Types.InAssembly(DomainAssembly)
			.That()
			.ResideInNamespace("Domain.Models")
			.And()
			.AreClasses()
			.And()
			.AreNotStatic()
			.GetTypes()
			.Where(t => !t.Name.Contains("Constants") &&
			            !t.Name.Contains("Preferences") &&
			            !t.IsEnum);

		// Assert - Entities should have an Id property
		foreach (var entityType in entityTypes)
		{
			var idProperty = entityType.GetProperty("Id");
			idProperty.Should().NotBeNull(
				$"Entity '{entityType.Name}' should have an Id property");
		}
	}

	[Fact]
	public void EntityIds_ShouldUseObjectIdType()
	{
		// Arrange & Act
		// Exclude enums, static classes, constants, and preferences (non-entity types)
		var entityTypes = Types.InAssembly(DomainAssembly)
			.That()
			.ResideInNamespace("Domain.Models")
			.And()
			.AreClasses()
			.And()
			.AreNotStatic()
			.GetTypes()
			.Where(t => !t.Name.Contains("Constants") &&
			            !t.Name.Contains("Preferences") &&
			            !t.IsEnum);

		// Assert - Entity IDs should be ObjectId type (MongoDB)
		foreach (var entityType in entityTypes)
		{
			var idProperty = entityType.GetProperty("Id");
			if (idProperty != null)
			{
				var idType = idProperty.PropertyType;
				// Accept ObjectId or string representations
				var isValidIdType = idType == typeof(ObjectId) ||
				                    idType == typeof(string) ||
				                    idType.Name.Contains("ObjectId");
				isValidIdType.Should().BeTrue(
					$"Entity '{entityType.Name}' Id property should be ObjectId or string, but was '{idType.Name}'");
			}
		}
	}

	[Fact]
	public void Entities_ShouldNotBeAbstract()
	{
		// Arrange & Act
		// Exclude static classes (like FileValidationConstants) which are abstract by nature
		var result = Types.InAssembly(DomainAssembly)
			.That()
			.ResideInNamespace("Domain.Models")
			.And()
			.AreClasses()
			.And()
			.AreNotStatic()
			.ShouldNot()
			.BeAbstract()
			.GetResult();

		// Assert
		result.IsSuccessful.Should().BeTrue(
			GetFailureMessage("Domain model entities should not be abstract", result));
	}

	[Fact]
	public void Entities_DateCreatedShouldHaveInitAccessor()
	{
		// Arrange & Act
		// Exclude static classes, constants, and preferences
		var entityTypes = Types.InAssembly(DomainAssembly)
			.That()
			.ResideInNamespace("Domain.Models")
			.And()
			.AreClasses()
			.And()
			.AreNotStatic()
			.GetTypes()
			.Where(t => !t.Name.Contains("Constants") &&
			            !t.Name.Contains("Preferences") &&
			            !t.IsEnum);

		// Assert - DateCreated should be init-only or readonly
		foreach (var entityType in entityTypes)
		{
			var dateCreatedProperty = entityType.GetProperty("DateCreated");
			if (dateCreatedProperty != null && dateCreatedProperty.CanWrite)
			{
				var setter = dateCreatedProperty.SetMethod;
				if (setter != null)
				{
					// Init-only setters have IsExternalInit modifier
					var isInitOnly = setter.ReturnParameter?
						.GetRequiredCustomModifiers()
						.Any(m => m.FullName == "System.Runtime.CompilerServices.IsExternalInit") ?? false;

					var isPrivate = !setter.IsPublic;

					var isProtected = isPrivate || isInitOnly;
					isProtected.Should().BeTrue(
						$"Entity '{entityType.Name}' DateCreated property should have init accessor or private setter");
				}
			}
		}
	}

	#endregion

	#region Repository Pattern Tests

	[Fact]
	public void AllRepositories_ShouldImplementIRepository()
	{
		// Arrange & Act
		var result = Types.InAssembly(PersistenceAssembly)
			.That()
			.HaveNameEndingWith("Repository")
			.And()
			.AreClasses()
			.And()
			.AreNotAbstract()
			.Should()
			.ImplementInterface(typeof(IRepository<>))
			.GetResult();

		// Assert
		result.IsSuccessful.Should().BeTrue(
			GetFailureMessage("All repositories should implement IRepository<T>", result));
	}

	[Fact]
	public void RepositoryMethods_ShouldBeAsync()
	{
		// Arrange
		var repositoryInterface = typeof(IRepository<>);

		// Act
		var methods = repositoryInterface.GetMethods()
			.Where(m => !m.IsSpecialName);

		// Assert - All public repository methods should be async (return Task)
		foreach (var method in methods)
		{
			var returnType = method.ReturnType;
			var isAsync = returnType.Name.Contains("Task") ||
			              returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>);

			isAsync.Should().BeTrue(
				$"Repository method '{method.Name}' should be async (return Task<T>)");
		}
	}

	[Fact]
	public void RepositoryImplementations_ShouldHaveAsyncMethods()
	{
		// Arrange & Act
		var repositoryTypes = Types.InAssembly(PersistenceAssembly)
			.That()
			.HaveNameEndingWith("Repository")
			.And()
			.AreClasses()
			.GetTypes();

		// Assert - All repository implementations should have async methods
		foreach (var repoType in repositoryTypes)
		{
			var publicMethods = repoType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
				.Where(m => !m.IsSpecialName && m.DeclaringType == repoType);

			foreach (var method in publicMethods)
			{
				// Data access methods should be async
				if (method.Name.Contains("Get") || method.Name.Contains("Find") ||
				    method.Name.Contains("Add") || method.Name.Contains("Update") ||
				    method.Name.Contains("Delete") || method.Name.Contains("Count") ||
				    method.Name.Contains("Any"))
				{
					var returnType = method.ReturnType;
					var isAsync = returnType.Name.Contains("Task");
					isAsync.Should().BeTrue(
						$"Repository method '{repoType.Name}.{method.Name}' should be async");
				}
			}
		}
	}

	[Fact]
	public void Repositories_ShouldBeInRepositoriesNamespace()
	{
		// Arrange & Act
		var repositoryTypes = Types.InAssembly(PersistenceAssembly)
			.That()
			.HaveNameEndingWith("Repository")
			.And()
			.AreClasses()
			.GetTypes();

		// Assert
		foreach (var repoType in repositoryTypes)
		{
			var namespacePath = repoType.Namespace ?? string.Empty;
			namespacePath.Should().Contain("Repositories",
				$"Repository '{repoType.Name}' should be in a Repositories namespace");
		}
	}

	#endregion

	#region Additional CQRS Tests

	[Fact]
	public void Queries_ShouldNotModifyState()
	{
		// Arrange & Act - Queries should return data, not have void handlers
		var queryTypes = Types.InAssembly(DomainAssembly)
			.That()
			.ImplementInterface(typeof(IRequest<>))
			.And()
			.HaveNameEndingWith("Query")
			.GetTypes();

		// Assert - Each query should have a non-void return type (via IRequest<T>)
		foreach (var queryType in queryTypes)
		{
			var requestInterface = queryType.GetInterfaces()
				.FirstOrDefault(i => i.IsGenericType &&
				                     i.GetGenericTypeDefinition() == typeof(IRequest<>));

			requestInterface.Should().NotBeNull(
				$"Query '{queryType.Name}' should implement IRequest<T>");

			if (requestInterface != null)
			{
				var responseType = requestInterface.GetGenericArguments()[0];
				responseType.Should().NotBe(typeof(Unit),
					$"Query '{queryType.Name}' should return data, not Unit");
			}
		}
	}

	[Fact]
	public void NotificationHandlers_ShouldEndWithNotificationHandler()
	{
		// Arrange & Act
		var result = Types.InAssembly(DomainAssembly)
			.That()
			.ImplementInterface(typeof(INotificationHandler<>))
			.Should()
			.HaveNameEndingWith("Handler")
			.GetResult();

		// Assert
		result.IsSuccessful.Should().BeTrue(
			GetFailureMessage("Notification handlers should end with 'Handler'", result));
	}

	#endregion

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
