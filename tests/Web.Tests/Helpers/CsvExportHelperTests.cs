// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     CsvExportHelperTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests
// =======================================================

using System.Text;

using Web.Helpers;

namespace Web.Tests.Helpers;

/// <summary>
///   Unit tests for the CsvExportHelper class.
///   Tests CSV export functionality including header generation, data formatting, and special character escaping.
/// </summary>
public sealed class CsvExportHelperTests
{
	#region ExportToCsv - Basic Functionality Tests

	[Fact]
	public void ExportToCsv_WithValidData_ReturnsNonEmptyByteArray()
	{
		// Arrange
		var data = new List<TestExportModel>
		{
			new() { Name = "Test", Value = 42 }
		};

		// Act
		var result = CsvExportHelper.ExportToCsv(data);

		// Assert
		result.Should().NotBeNull();
		result.Should().NotBeEmpty();
	}

	[Fact]
	public void ExportToCsv_WithEmptyCollection_ReturnsHeaderOnly()
	{
		// Arrange
		var data = new List<TestExportModel>();

		// Act
		var result = CsvExportHelper.ExportToCsv(data);
		var csvContent = Encoding.UTF8.GetString(result);

		// Assert
		csvContent.Should().Contain("\"Name\"");
		csvContent.Should().Contain("\"Value\"");
		csvContent.Trim().Split('\n').Should().HaveCount(1); // Header only
	}

	[Fact]
	public void ExportToCsv_GeneratesCorrectHeaderRow()
	{
		// Arrange
		var data = new List<TestExportModel>
		{
			new() { Name = "Test", Value = 1 }
		};

		// Act
		var result = CsvExportHelper.ExportToCsv(data);
		var csvContent = Encoding.UTF8.GetString(result);
		var lines = csvContent.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

		// Assert
		lines.Should().HaveCountGreaterThanOrEqualTo(1);
		lines[0].Should().Contain("\"Name\"");
		lines[0].Should().Contain("\"Value\"");
	}

	[Fact]
	public void ExportToCsv_GeneratesCorrectDataRows()
	{
		// Arrange
		var data = new List<TestExportModel>
		{
			new() { Name = "Item1", Value = 100 },
			new() { Name = "Item2", Value = 200 }
		};

		// Act
		var result = CsvExportHelper.ExportToCsv(data);
		var csvContent = Encoding.UTF8.GetString(result);
		var lines = csvContent.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

		// Assert
		lines.Should().HaveCount(3); // Header + 2 data rows
		lines[1].Should().Contain("Item1");
		lines[1].Should().Contain("100");
		lines[2].Should().Contain("Item2");
		lines[2].Should().Contain("200");
	}

	[Fact]
	public void ExportToCsv_ReturnsUtf8EncodedBytes()
	{
		// Arrange
		var data = new List<TestExportModel>
		{
			new() { Name = "Test", Value = 1 }
		};

		// Act
		var result = CsvExportHelper.ExportToCsv(data);
		var decodedString = Encoding.UTF8.GetString(result);

		// Assert
		decodedString.Should().NotBeNullOrEmpty();
		decodedString.Should().Contain("Test");
	}

	#endregion

	#region ExportToCsv - Null Value Handling Tests

	[Fact]
	public void ExportToCsv_WithNullPropertyValue_ReturnsEmptyString()
	{
		// Arrange
		var data = new List<TestExportModelWithNullable>
		{
			new() { Name = null, Value = 42 }
		};

		// Act
		var result = CsvExportHelper.ExportToCsv(data);
		var csvContent = Encoding.UTF8.GetString(result);
		var lines = csvContent.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

		// Assert
		lines.Should().HaveCount(2);
		// The data row should have an empty string for the null Name
		lines[1].Should().Contain(",");
	}

	[Fact]
	public void ExportToCsv_WithNullableIntNull_ReturnsEmptyString()
	{
		// Arrange
		var data = new List<TestExportModelWithNullable>
		{
			new() { Name = "Test", Value = null }
		};

		// Act
		var result = CsvExportHelper.ExportToCsv(data);
		var csvContent = Encoding.UTF8.GetString(result);

		// Assert
		csvContent.Should().Contain("Test");
	}

	#endregion

	#region ExportToCsv - Special Character Escaping Tests

	[Fact]
	public void ExportToCsv_WithCommaInValue_EscapesCorrectly()
	{
		// Arrange
		var data = new List<TestExportModel>
		{
			new() { Name = "Hello, World", Value = 1 }
		};

		// Act
		var result = CsvExportHelper.ExportToCsv(data);
		var csvContent = Encoding.UTF8.GetString(result);

		// Assert
		csvContent.Should().Contain("\"Hello, World\"");
	}

	[Fact]
	public void ExportToCsv_WithDoubleQuoteInValue_EscapesCorrectly()
	{
		// Arrange
		var data = new List<TestExportModel>
		{
			new() { Name = "Say \"Hello\"", Value = 1 }
		};

		// Act
		var result = CsvExportHelper.ExportToCsv(data);
		var csvContent = Encoding.UTF8.GetString(result);

		// Assert
		// Double quotes should be escaped by doubling them
		csvContent.Should().Contain("\"\"Hello\"\"");
	}

	[Fact]
	public void ExportToCsv_WithNewlineInValue_EscapesCorrectly()
	{
		// Arrange
		var data = new List<TestExportModel>
		{
			new() { Name = "Line1\nLine2", Value = 1 }
		};

		// Act
		var result = CsvExportHelper.ExportToCsv(data);
		var csvContent = Encoding.UTF8.GetString(result);

		// Assert
		csvContent.Should().Contain("\"Line1\nLine2\"");
	}

	[Fact]
	public void ExportToCsv_WithCarriageReturnInValue_EscapesCorrectly()
	{
		// Arrange
		var data = new List<TestExportModel>
		{
			new() { Name = "Line1\rLine2", Value = 1 }
		};

		// Act
		var result = CsvExportHelper.ExportToCsv(data);
		var csvContent = Encoding.UTF8.GetString(result);

		// Assert
		csvContent.Should().Contain("\"Line1\rLine2\"");
	}

	[Fact]
	public void ExportToCsv_WithMultipleSpecialCharacters_EscapesAllCorrectly()
	{
		// Arrange
		var data = new List<TestExportModel>
		{
			new() { Name = "Hello, \"World\"\nNew Line", Value = 1 }
		};

		// Act
		var result = CsvExportHelper.ExportToCsv(data);
		var csvContent = Encoding.UTF8.GetString(result);

		// Assert
		csvContent.Should().Contain("Hello, \"\"World\"\"");
		csvContent.Should().Contain("New Line");
	}

	#endregion

	#region ExportToCsv - Complex Type Tests

	[Fact]
	public void ExportToCsv_WithDateTimeProperty_FormatsCorrectly()
	{
		// Arrange
		var testDate = new DateTime(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc);
		var data = new List<TestExportModelWithDateTime>
		{
			new() { Name = "Test", CreatedAt = testDate }
		};

		// Act
		var result = CsvExportHelper.ExportToCsv(data);
		var csvContent = Encoding.UTF8.GetString(result);

		// Assert
		csvContent.Should().Contain("2024");
		csvContent.Should().Contain("Test");
	}

	[Fact]
	public void ExportToCsv_WithBooleanProperty_FormatsCorrectly()
	{
		// Arrange
		var data = new List<TestExportModelWithBoolean>
		{
			new() { Name = "Active Item", IsActive = true },
			new() { Name = "Inactive Item", IsActive = false }
		};

		// Act
		var result = CsvExportHelper.ExportToCsv(data);
		var csvContent = Encoding.UTF8.GetString(result);

		// Assert
		csvContent.Should().Contain("True");
		csvContent.Should().Contain("False");
	}

	[Fact]
	public void ExportToCsv_WithDecimalProperty_FormatsCorrectly()
	{
		// Arrange
		var data = new List<TestExportModelWithDecimal>
		{
			new() { Name = "Product", Price = 99.99m }
		};

		// Act
		var result = CsvExportHelper.ExportToCsv(data);
		var csvContent = Encoding.UTF8.GetString(result);

		// Assert
		csvContent.Should().Contain("99.99");
	}

	#endregion

	#region ExportToCsv - Multiple Properties Tests

	[Fact]
	public void ExportToCsv_WithManyProperties_IncludesAllInHeader()
	{
		// Arrange
		var data = new List<TestExportModelLarge>
		{
			new()
			{
				Id = 1,
				Name = "Test",
				Description = "Desc",
				Category = "Cat",
				Status = "Active"
			}
		};

		// Act
		var result = CsvExportHelper.ExportToCsv(data);
		var csvContent = Encoding.UTF8.GetString(result);
		var lines = csvContent.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

		// Assert
		lines[0].Should().Contain("\"Id\"");
		lines[0].Should().Contain("\"Name\"");
		lines[0].Should().Contain("\"Description\"");
		lines[0].Should().Contain("\"Category\"");
		lines[0].Should().Contain("\"Status\"");
	}

	[Fact]
	public void ExportToCsv_PreservesPropertyOrder()
	{
		// Arrange
		var data = new List<TestExportModel>
		{
			new() { Name = "Test", Value = 42 }
		};

		// Act
		var result = CsvExportHelper.ExportToCsv(data);
		var csvContent = Encoding.UTF8.GetString(result);
		var lines = csvContent.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

		// Assert
		var headerParts = lines[0].Split(',');
		var nameIndex = Array.FindIndex(headerParts, p => p.Contains("Name"));
		var valueIndex = Array.FindIndex(headerParts, p => p.Contains("Value"));
		nameIndex.Should().BeLessThan(valueIndex);
	}

	#endregion

	#region ExportToCsv - Large Dataset Tests

	[Fact]
	public void ExportToCsv_WithLargeDataset_HandlesCorrectly()
	{
		// Arrange
		var data = Enumerable.Range(1, 1000)
			.Select(i => new TestExportModel { Name = $"Item{i}", Value = i })
			.ToList();

		// Act
		var result = CsvExportHelper.ExportToCsv(data);
		var csvContent = Encoding.UTF8.GetString(result);
		var lines = csvContent.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

		// Assert
		lines.Should().HaveCount(1001); // Header + 1000 data rows
	}

	#endregion

	#region ExportToCsv - Edge Cases Tests

	[Fact]
	public void ExportToCsv_WithEmptyStringValue_HandlesCorrectly()
	{
		// Arrange
		var data = new List<TestExportModel>
		{
			new() { Name = "", Value = 1 }
		};

		// Act
		var result = CsvExportHelper.ExportToCsv(data);
		var csvContent = Encoding.UTF8.GetString(result);

		// Assert
		result.Should().NotBeNull();
		csvContent.Should().Contain("\"\""); // Empty quoted string
	}

	[Fact]
	public void ExportToCsv_WithWhitespaceValue_PreservesWhitespace()
	{
		// Arrange
		var data = new List<TestExportModel>
		{
			new() { Name = "   ", Value = 1 }
		};

		// Act
		var result = CsvExportHelper.ExportToCsv(data);
		var csvContent = Encoding.UTF8.GetString(result);

		// Assert
		csvContent.Should().Contain("   ");
	}

	[Fact]
	public void ExportToCsv_WithUnicodeCharacters_HandlesCorrectly()
	{
		// Arrange
		var data = new List<TestExportModel>
		{
			new() { Name = "日本語テスト", Value = 1 },
			new() { Name = "Émoji 🎉", Value = 2 }
		};

		// Act
		var result = CsvExportHelper.ExportToCsv(data);
		var csvContent = Encoding.UTF8.GetString(result);

		// Assert
		csvContent.Should().Contain("日本語テスト");
		csvContent.Should().Contain("Émoji");
	}

	#endregion

	#region Test Models

	private sealed class TestExportModel
	{
		public string Name { get; set; } = string.Empty;
		public int Value { get; set; }
	}

	private sealed class TestExportModelWithNullable
	{
		public string? Name { get; set; }
		public int? Value { get; set; }
	}

	private sealed class TestExportModelWithDateTime
	{
		public string Name { get; set; } = string.Empty;
		public DateTime CreatedAt { get; set; }
	}

	private sealed class TestExportModelWithBoolean
	{
		public string Name { get; set; } = string.Empty;
		public bool IsActive { get; set; }
	}

	private sealed class TestExportModelWithDecimal
	{
		public string Name { get; set; } = string.Empty;
		public decimal Price { get; set; }
	}

	private sealed class TestExportModelLarge
	{
		public int Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public string Description { get; set; } = string.Empty;
		public string Category { get; set; } = string.Empty;
		public string Status { get; set; } = string.Empty;
	}

	#endregion
}
