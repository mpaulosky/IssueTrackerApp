// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     CachingArchitectureTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  Architecture.Tests
// =============================================

namespace Architecture.Tests;

/// <summary>
///   Architecture tests that enforce caching conventions.
///   Issue #238: DistributedCacheHelper registration + naming rules.
/// </summary>
public sealed class CachingArchitectureTests
{
	private static readonly Assembly WebAssembly = typeof(Program).Assembly;

	// ── Test 1 ────────────────────────────────────────────────────────────────

	/// <summary>
	///   Verifies that <c>DistributedCacheHelper</c> lives in the
	///   <c>Web.Services</c> namespace.
	/// </summary>
	[Fact]
	public void DistributedCacheHelper_ShouldBeInServicesNamespace()
	{
		// Arrange & Act
		var result = Types.InAssembly(WebAssembly)
			.That()
			.HaveName("DistributedCacheHelper")
			.Should()
			.ResideInNamespace("Web.Services")
			.GetResult();

		// Assert
		result.IsSuccessful.Should().BeTrue(
			because: "DistributedCacheHelper must be in the Web.Services namespace so the whole codebase can locate it consistently");
	}

	// ── Test 2 ────────────────────────────────────────────────────────────────

	/// <summary>
	///   Verifies that services under <c>Web.Services</c> whose name ends with
	///   "Service" do NOT take a direct dependency on
	///   <c>Microsoft.Extensions.Caching.Distributed.IDistributedCache</c>.
	///   They must go through <c>DistributedCacheHelper</c> instead.
	///   <para>
	///     Exceptions (by design):
	///     <list type="bullet">
	///       <item><c>DistributedCacheHelper</c> itself — it wraps IDistributedCache.</item>
	///       <item>
	///         <c>UserManagementService</c> in <c>Web.Features.Admin.Users</c> —
	///         Sprint 2 decision: uses IDistributedCache directly for Auth0 user
	///         list versioning.
	///       </item>
	///       <item>
	///         <c>AnalyticsService</c> — pre-Sprint-1 service that uses IDistributedCache
	///         directly for analytics aggregation caching; excluded from this rule as a
	///         known legacy exception.
	///       </item>
	///     </list>
	///   </para>
	/// </summary>
	[Fact]
	public void Services_ThatUseCache_ShouldDependOnDistributedCacheHelper()
	{
		// Arrange & Act — find "Service"-named types in Web.Services that reference
		// IDistributedCache directly (they should not, except the allowed exclusions).
		var violatingTypes = Types.InAssembly(WebAssembly)
			.That()
			.ResideInNamespace("Web.Services")
			.And()
			.HaveNameEndingWith("Service")
			.GetTypes()
			.Where(t => t.Name != "DistributedCacheHelper") // allowed wrapper
			.Where(t => t.Name != "AnalyticsService")       // pre-Sprint-1 legacy exception
			.Where(t => HasDirectIDistributedCacheDependency(t))
			.ToList();

		// Assert
		violatingTypes.Should().BeEmpty(
			because: "services in Web.Services should use DistributedCacheHelper rather than IDistributedCache directly; " +
			         "known exceptions are DistributedCacheHelper itself, AnalyticsService (pre-Sprint-1 legacy), and " +
			         "UserManagementService (Web.Features.Admin.Users, Sprint-2 design decision)");
	}

	// ── Test 3 ────────────────────────────────────────────────────────────────

	/// <summary>
	///   Best-effort: verifies that no service class in <c>Web.Services</c>
	///   exposes public cache-key constants (they should be private/internal).
	/// </summary>
	[Fact]
	public void CacheKeyConstants_ShouldBePrivateOrInternal()
	{
		// Arrange
		var serviceTypes = Types.InAssembly(WebAssembly)
			.That()
			.ResideInNamespace("Web.Services")
			.And()
			.HaveNameEndingWith("Service")
			.GetTypes();

		// Act — collect any public const fields whose name suggests a cache key
		var publicCacheKeys = serviceTypes
			.SelectMany(t => t.GetFields(
				System.Reflection.BindingFlags.Public |
				System.Reflection.BindingFlags.Static |
				System.Reflection.BindingFlags.DeclaredOnly))
			.Where(f => f.IsLiteral) // const fields are Literal
			.Where(f => f.Name.Contains("Cache", StringComparison.OrdinalIgnoreCase) ||
			            f.Name.Contains("Key",   StringComparison.OrdinalIgnoreCase))
			.ToList();

		// Assert
		publicCacheKeys.Should().BeEmpty(
			because: "cache key constants should be private or internal to prevent external coupling to implementation details");
	}

	// ── helpers ───────────────────────────────────────────────────────────────

	/// <summary>
	///   Returns <c>true</c> when <paramref name="type" /> has a constructor
	///   parameter of type <c>IDistributedCache</c>.
	/// </summary>
	private static bool HasDirectIDistributedCacheDependency(Type type)
	{
		return type
			.GetConstructors()
			.Any(ctor => ctor
				.GetParameters()
				.Any(p => p.ParameterType.FullName ==
				          "Microsoft.Extensions.Caching.Distributed.IDistributedCache"));
	}
}
