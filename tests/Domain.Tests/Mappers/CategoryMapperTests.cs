// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     CategoryMapperTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain.Tests
// =======================================================

namespace Domain.Tests.Mappers;

/// <summary>
///   Unit tests for CategoryMapper static mapping methods.
/// </summary>
public sealed class CategoryMapperTests
{
	private readonly DateTime _dateCreated = new(2025, 1, 15, 10, 30, 0, DateTimeKind.Utc);
	private readonly DateTime _dateModified = new(2025, 2, 20, 14, 45, 0, DateTimeKind.Utc);

	#region ToDto(Category) Tests

	[Fact]
	public void ToDto_FromCategory_WithValidCategory_ReturnsCorrectDto()
	{
		// Arrange
		var categoryId = ObjectId.GenerateNewId();
		var archivedBy = new UserInfo
		{
			Id = "auth0|archiver",
			Name = "Archiver User",
			Email = "archiver@example.com"
		};
		var category = new Category
		{
			Id = categoryId,
			CategoryName = "Bug",
			CategoryDescription = "Bug reports and defects",
			DateCreated = _dateCreated,
			DateModified = _dateModified,
			Archived = true,
			ArchivedBy = archivedBy
		};

		// Act
		var result = CategoryMapper.ToDto(category);

		// Assert
		result.Should().NotBeNull();
		result.Id.Should().Be(categoryId);
		result.CategoryName.Should().Be("Bug");
		result.CategoryDescription.Should().Be("Bug reports and defects");
		result.DateCreated.Should().Be(_dateCreated);
		result.DateModified.Should().Be(_dateModified);
		result.Archived.Should().BeTrue();
		result.ArchivedBy.Id.Should().Be("auth0|archiver");
	}

	[Fact]
	public void ToDto_FromCategory_WithNullCategory_ReturnsEmptyDto()
	{
		// Arrange
		Category? category = null;

		// Act
		var result = CategoryMapper.ToDto(category);

		// Assert
		result.Should().NotBeNull();
		result.Id.Should().Be(ObjectId.Empty);
		result.CategoryName.Should().BeEmpty();
		result.CategoryDescription.Should().BeEmpty();
	}

	[Fact]
	public void ToDto_FromCategory_WithNonArchivedCategory_ReturnsArchivedFalse()
	{
		// Arrange
		var category = new Category
		{
			Id = ObjectId.GenerateNewId(),
			CategoryName = "Feature",
			CategoryDescription = "Feature requests",
			DateCreated = _dateCreated,
			DateModified = null,
			Archived = false,
			ArchivedBy = UserInfo.Empty
		};

		// Act
		var result = CategoryMapper.ToDto(category);

		// Assert
		result.Archived.Should().BeFalse();
		result.DateModified.Should().BeNull();
		result.ArchivedBy.Id.Should().BeEmpty();
		result.ArchivedBy.Name.Should().BeEmpty();
		result.ArchivedBy.Email.Should().BeEmpty();
	}

	[Fact]
	public void ToDto_FromCategory_MapsArchivedByCorrectly()
	{
		// Arrange
		var archivedBy = new UserInfo
		{
			Id = "auth0|admin",
			Name = "Admin User",
			Email = "admin@example.com"
		};
		var category = new Category
		{
			Id = ObjectId.GenerateNewId(),
			CategoryName = "Deprecated",
			CategoryDescription = "Deprecated items",
			Archived = true,
			ArchivedBy = archivedBy
		};

		// Act
		var result = CategoryMapper.ToDto(category);

		// Assert
		result.ArchivedBy.Should().NotBeNull();
		result.ArchivedBy.Id.Should().Be("auth0|admin");
		result.ArchivedBy.Name.Should().Be("Admin User");
		result.ArchivedBy.Email.Should().Be("admin@example.com");
	}

	#endregion

	#region ToDto(CategoryInfo) Tests

	[Fact]
	public void ToDto_FromCategoryInfo_WithValidCategoryInfo_ReturnsCorrectDto()
	{
		// Arrange
		var categoryId = ObjectId.GenerateNewId();
		var archivedBy = new UserInfo
		{
			Id = "auth0|info-archiver",
			Name = "Info Archiver",
			Email = "info@example.com"
		};
		var categoryInfo = new CategoryInfo
		{
			Id = categoryId,
			CategoryName = "Enhancement",
			CategoryDescription = "Enhancement requests",
			DateCreated = _dateCreated,
			DateModified = _dateModified,
			Archived = false,
			ArchivedBy = archivedBy
		};

		// Act
		var result = CategoryMapper.ToDto(categoryInfo);

		// Assert
		result.Should().NotBeNull();
		result.Id.Should().Be(categoryId);
		result.CategoryName.Should().Be("Enhancement");
		result.CategoryDescription.Should().Be("Enhancement requests");
		result.DateCreated.Should().Be(_dateCreated);
		result.DateModified.Should().Be(_dateModified);
		result.Archived.Should().BeFalse();
	}

	[Fact]
	public void ToDto_FromCategoryInfo_WithNullCategoryInfo_ReturnsEmptyDto()
	{
		// Arrange
		CategoryInfo? categoryInfo = null;

		// Act
		var result = CategoryMapper.ToDto(categoryInfo);

		// Assert
		result.Should().NotBeNull();
		result.Id.Should().Be(ObjectId.Empty);
		result.CategoryName.Should().BeEmpty();
	}

	[Fact]
	public void ToDto_FromCategoryInfo_WithEmptyCategoryInfo_ReturnsEmptyValuesDto()
	{
		// Arrange
		var categoryInfo = CategoryInfo.Empty;

		// Act
		var result = CategoryMapper.ToDto(categoryInfo);

		// Assert
		result.Should().NotBeNull();
		result.Id.Should().Be(ObjectId.Empty);
		result.CategoryName.Should().BeEmpty();
		result.CategoryDescription.Should().BeEmpty();
	}

	#endregion

	#region ToModel Tests

	[Fact]
	public void ToModel_WithValidDto_ReturnsCorrectModel()
	{
		// Arrange
		var categoryId = ObjectId.GenerateNewId();
		var archivedByDto = new UserDto("auth0|model-user", "Model User", "model@example.com");
		var dto = new CategoryDto(
			categoryId,
			"Documentation",
			"Documentation updates",
			_dateCreated,
			_dateModified,
			true,
			archivedByDto);

		// Act
		var result = CategoryMapper.ToModel(dto);

		// Assert
		result.Should().NotBeNull();
		result.Id.Should().Be(categoryId);
		result.CategoryName.Should().Be("Documentation");
		result.CategoryDescription.Should().Be("Documentation updates");
		result.DateCreated.Should().Be(_dateCreated);
		result.DateModified.Should().Be(_dateModified);
		result.Archived.Should().BeTrue();
		result.ArchivedBy.Id.Should().Be("auth0|model-user");
	}

	[Fact]
	public void ToModel_WithNullDto_ReturnsEmptyModel()
	{
		// Arrange
		CategoryDto? dto = null;

		// Act
		var result = CategoryMapper.ToModel(dto);

		// Assert
		result.Should().NotBeNull();
		result.Id.Should().Be(ObjectId.Empty);
		result.CategoryName.Should().BeEmpty();
		result.CategoryDescription.Should().BeEmpty();
	}

	[Fact]
	public void ToModel_WithEmptyDto_ReturnsDefaultModel()
	{
		// Arrange
		var dto = CategoryDto.Empty;

		// Act
		var result = CategoryMapper.ToModel(dto);

		// Assert
		result.Should().NotBeNull();
		result.Id.Should().Be(ObjectId.Empty);
		result.CategoryName.Should().BeEmpty();
	}

	[Fact]
	public void ToModel_WithNullDateModified_SetsNullDateModified()
	{
		// Arrange
		var dto = new CategoryDto(
			ObjectId.GenerateNewId(),
			"Test",
			"Test Description",
			_dateCreated,
			null,
			false,
			UserDto.Empty);

		// Act
		var result = CategoryMapper.ToModel(dto);

		// Assert
		result.DateModified.Should().BeNull();
	}

	#endregion

	#region ToInfo Tests

	[Fact]
	public void ToInfo_WithValidDto_ReturnsCorrectCategoryInfo()
	{
		// Arrange
		var categoryId = ObjectId.GenerateNewId();
		var archivedByDto = new UserDto("auth0|info-user", "Info User", "info@example.com");
		var dto = new CategoryDto(
			categoryId,
			"Security",
			"Security-related issues",
			_dateCreated,
			_dateModified,
			false,
			archivedByDto);

		// Act
		var result = CategoryMapper.ToInfo(dto);

		// Assert
		result.Should().NotBeNull();
		result.Id.Should().Be(categoryId);
		result.CategoryName.Should().Be("Security");
		result.CategoryDescription.Should().Be("Security-related issues");
		result.DateCreated.Should().Be(_dateCreated);
		result.DateModified.Should().Be(_dateModified);
		result.Archived.Should().BeFalse();
		result.ArchivedBy.Id.Should().Be("auth0|info-user");
	}

	[Fact]
	public void ToInfo_WithNullDto_ReturnsEmptyCategoryInfo()
	{
		// Arrange
		CategoryDto? dto = null;

		// Act
		var result = CategoryMapper.ToInfo(dto);

		// Assert
		result.Should().NotBeNull();
		result.Id.Should().Be(ObjectId.Empty);
		result.CategoryName.Should().BeEmpty();
		result.CategoryDescription.Should().BeEmpty();
	}

	[Fact]
	public void ToInfo_WithEmptyDto_ReturnsEmptyValuesCategoryInfo()
	{
		// Arrange
		var dto = CategoryDto.Empty;

		// Act
		var result = CategoryMapper.ToInfo(dto);

		// Assert
		result.Should().NotBeNull();
		result.Id.Should().Be(ObjectId.Empty);
		result.CategoryName.Should().BeEmpty();
		result.CategoryDescription.Should().BeEmpty();
	}

	#endregion

	#region ToDtoList Tests

	[Fact]
	public void ToDtoList_WithValidCategories_ReturnsCorrectDtoList()
	{
		// Arrange
		var categories = new List<Category>
		{
			new()
			{
				Id = ObjectId.GenerateNewId(),
				CategoryName = "Bug",
				CategoryDescription = "Bug reports",
				DateCreated = _dateCreated
			},
			new()
			{
				Id = ObjectId.GenerateNewId(),
				CategoryName = "Feature",
				CategoryDescription = "Feature requests",
				DateCreated = _dateCreated
			},
			new()
			{
				Id = ObjectId.GenerateNewId(),
				CategoryName = "Enhancement",
				CategoryDescription = "Enhancements",
				DateCreated = _dateCreated
			}
		};

		// Act
		var result = CategoryMapper.ToDtoList(categories);

		// Assert
		result.Should().NotBeNull();
		result.Should().HaveCount(3);
		result[0].CategoryName.Should().Be("Bug");
		result[1].CategoryName.Should().Be("Feature");
		result[2].CategoryName.Should().Be("Enhancement");
	}

	[Fact]
	public void ToDtoList_WithNullCollection_ReturnsEmptyList()
	{
		// Arrange
		IEnumerable<Category>? categories = null;

		// Act
		var result = CategoryMapper.ToDtoList(categories);

		// Assert
		result.Should().NotBeNull();
		result.Should().BeEmpty();
	}

	[Fact]
	public void ToDtoList_WithEmptyCollection_ReturnsEmptyList()
	{
		// Arrange
		var categories = new List<Category>();

		// Act
		var result = CategoryMapper.ToDtoList(categories);

		// Assert
		result.Should().NotBeNull();
		result.Should().BeEmpty();
	}

	[Fact]
	public void ToDtoList_WithSingleCategory_ReturnsSingleElementList()
	{
		// Arrange
		var categories = new List<Category>
		{
			new()
			{
				Id = ObjectId.GenerateNewId(),
				CategoryName = "Only Category",
				CategoryDescription = "The only category"
			}
		};

		// Act
		var result = CategoryMapper.ToDtoList(categories);

		// Assert
		result.Should().HaveCount(1);
		result[0].CategoryName.Should().Be("Only Category");
	}

	[Fact]
	public void ToDtoList_PreservesOrderOfCategories()
	{
		// Arrange
		var id1 = ObjectId.GenerateNewId();
		var id2 = ObjectId.GenerateNewId();
		var id3 = ObjectId.GenerateNewId();
		var categories = new List<Category>
		{
			new() { Id = id1, CategoryName = "First" },
			new() { Id = id2, CategoryName = "Second" },
			new() { Id = id3, CategoryName = "Third" }
		};

		// Act
		var result = CategoryMapper.ToDtoList(categories);

		// Assert
		result[0].Id.Should().Be(id1);
		result[1].Id.Should().Be(id2);
		result[2].Id.Should().Be(id3);
	}

	#endregion

	#region Round-Trip Tests

	[Fact]
	public void RoundTrip_CategoryToModelToDto_PreservesData()
	{
		// Arrange
		var categoryId = ObjectId.GenerateNewId();
		var archivedByDto = new UserDto("auth0|roundtrip", "RoundTrip User", "roundtrip@example.com");
		var originalDto = new CategoryDto(
			categoryId,
			"RoundTrip Category",
			"Testing round-trip mapping",
			_dateCreated,
			_dateModified,
			true,
			archivedByDto);

		// Act
		var model = CategoryMapper.ToModel(originalDto);
		var resultDto = CategoryMapper.ToDto(model);

		// Assert
		resultDto.Id.Should().Be(originalDto.Id);
		resultDto.CategoryName.Should().Be(originalDto.CategoryName);
		resultDto.CategoryDescription.Should().Be(originalDto.CategoryDescription);
		resultDto.DateCreated.Should().Be(originalDto.DateCreated);
		resultDto.DateModified.Should().Be(originalDto.DateModified);
		resultDto.Archived.Should().Be(originalDto.Archived);
		resultDto.ArchivedBy.Id.Should().Be(originalDto.ArchivedBy.Id);
	}

	[Fact]
	public void RoundTrip_CategoryInfoToDtoToInfo_PreservesData()
	{
		// Arrange
		var categoryId = ObjectId.GenerateNewId();
		var archivedBy = new UserInfo
		{
			Id = "auth0|infotrip",
			Name = "Info Trip User",
			Email = "infotrip@example.com"
		};
		var originalInfo = new CategoryInfo
		{
			Id = categoryId,
			CategoryName = "Info Trip Category",
			CategoryDescription = "Testing CategoryInfo round-trip",
			DateCreated = _dateCreated,
			DateModified = _dateModified,
			Archived = false,
			ArchivedBy = archivedBy
		};

		// Act
		var dto = CategoryMapper.ToDto(originalInfo);
		var resultInfo = CategoryMapper.ToInfo(dto);

		// Assert
		resultInfo.Id.Should().Be(originalInfo.Id);
		resultInfo.CategoryName.Should().Be(originalInfo.CategoryName);
		resultInfo.CategoryDescription.Should().Be(originalInfo.CategoryDescription);
		resultInfo.DateCreated.Should().Be(originalInfo.DateCreated);
		resultInfo.DateModified.Should().Be(originalInfo.DateModified);
		resultInfo.Archived.Should().Be(originalInfo.Archived);
		resultInfo.ArchivedBy.Id.Should().Be(originalInfo.ArchivedBy.Id);
	}

	#endregion
}
