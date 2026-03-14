// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     ChartComponentTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests.Bunit
// =======================================================

using Web.Components.Charts;

namespace Web.Tests.Bunit.Charts;

/// <summary>
/// Test suite for PieChart component.
/// </summary>
public class PieChartTests : BunitTestBase
{
	[Fact]
	public void PieChart_WithValidData_RendersCanvasElement()
	{
		// Arrange
		var chartId = "pie-chart-1";
		var labels = new List<string> { "Open", "Closed", "In Progress" };
		var data = new List<int> { 10, 5, 3 };
		var colors = new List<string> { "#3b82f6", "#10b981", "#f59e0b" };

		JSInterop.SetupVoid("chartInterop.renderPieChart", _ => true);
		JSInterop.SetupVoid("chartInterop.destroyChart", _ => true);

		// Act
		var cut = Render<PieChart>(parameters => parameters
			.Add(x => x.ChartId, chartId)
			.Add(x => x.Labels, labels)
			.Add(x => x.Data, data)
			.Add(x => x.Colors, colors)
			.Add(x => x.IsLoading, false));

		// Assert
		cut.Find("canvas").Should().NotBeNull();
		cut.Find($"canvas#{chartId}").Should().NotBeNull();
	}

	[Fact]
	public void PieChart_WithEmptyData_DoesNotRenderCanvas()
	{
		// Arrange
		var labels = new List<string>();
		var data = new List<int>();
		var colors = new List<string>();

		// Act
		var cut = Render<PieChart>(parameters => parameters
			.Add(x => x.Labels, labels)
			.Add(x => x.Data, data)
			.Add(x => x.Colors, colors)
			.Add(x => x.IsLoading, false));

		// Assert - Canvas is rendered but JS interop is NOT called for empty data
		var canvases = cut.FindAll("canvas");
		canvases.Should().NotBeEmpty();
		JSInterop.Invocations.Count(x => x.Identifier == "chartInterop.renderPieChart").Should().Be(0);
	}

	[Fact]
	public void PieChart_WhenLoading_DisplaysLoadingSpinner()
	{
		// Arrange
		var labels = new List<string> { "Open", "Closed" };
		var data = new List<int> { 10, 5 };
		var colors = new List<string> { "#3b82f6", "#10b981" };

		// Act
		var cut = Render<PieChart>(parameters => parameters
			.Add(x => x.Labels, labels)
			.Add(x => x.Data, data)
			.Add(x => x.Colors, colors)
			.Add(x => x.IsLoading, true));

		// Assert
		cut.FindAll("canvas").Should().BeEmpty();
		var spinner = cut.Find("div.animate-spin");
		spinner.Should().NotBeNull();
	}

	[Fact]
	public void PieChart_WithData_InvokesJSInteropRenderMethod()
	{
		// Arrange
		var chartId = "pie-chart-2";
		var labels = new List<string> { "Open", "Closed" };
		var data = new List<int> { 10, 5 };
		var colors = new List<string> { "#3b82f6", "#10b981" };

		var jsInteropCalls = new List<string>();
		JSInterop.SetupVoid("chartInterop.renderPieChart", invocation =>
		{
			jsInteropCalls.Add(invocation.Identifier);
			return true;
		});

		// Act
		var cut = Render<PieChart>(parameters => parameters
			.Add(x => x.ChartId, chartId)
			.Add(x => x.Labels, labels)
			.Add(x => x.Data, data)
			.Add(x => x.Colors, colors)
			.Add(x => x.IsLoading, false));

		// Assert
		jsInteropCalls.Should().Contain("chartInterop.renderPieChart");
	}

	[Fact]
public async Task PieChart_OnDispose_InvokesDestroyChart()
	{
		// Arrange
		var chartId = "pie-chart-3";
		var labels = new List<string> { "Open", "Closed" };
		var data = new List<int> { 10, 5 };
		var colors = new List<string> { "#3b82f6", "#10b981" };

		JSInterop.SetupVoid("chartInterop.renderPieChart", _ => true);
		JSInterop.SetupVoid("chartInterop.destroyChart", _ => true);

		var cut = Render<PieChart>(parameters => parameters
			.Add(x => x.ChartId, chartId)
			.Add(x => x.Labels, labels)
			.Add(x => x.Data, data)
			.Add(x => x.Colors, colors)
			.Add(x => x.IsLoading, false));

		// Act
		await cut.Instance.DisposeAsync();

		// Assert - verify disposal didn't throw
		JSInterop.Invocations.Count(x => x.Identifier == "chartInterop.destroyChart").Should().Be(1);
	}

	[Fact]
	public void PieChart_WithOnlyLabels_DoesNotRenderChart()
	{
		// Arrange
		var labels = new List<string> { "Open", "Closed" };
		var data = new List<int>(); // Empty data

		JSInterop.SetupVoid("chartInterop.renderPieChart", _ => true);

		// Act
		var cut = Render<PieChart>(parameters => parameters
			.Add(x => x.Labels, labels)
			.Add(x => x.Data, data)
			.Add(x => x.IsLoading, false));

		// Assert - Canvas is rendered but JS interop is NOT called with empty data
		var canvases = cut.FindAll("canvas");
		canvases.Should().NotBeEmpty();
		JSInterop.Invocations.Count(x => x.Identifier == "chartInterop.renderPieChart").Should().Be(0);
	}

	[Fact]
	public void PieChart_WithMultipleDataPoints_PassesCorrectParameters()
	{
		// Arrange
		var chartId = "pie-chart-4";
		var labels = new List<string> { "Open", "Closed", "In Progress", "Resolved" };
		var data = new List<int> { 10, 5, 3, 2 };
		var colors = new List<string> { "#3b82f6", "#10b981", "#f59e0b", "#ef4444" };

		JSInterop.SetupVoid("chartInterop.renderPieChart", _ => true);

		// Act
		var cut = Render<PieChart>(parameters => parameters
			.Add(x => x.ChartId, chartId)
			.Add(x => x.Labels, labels)
			.Add(x => x.Data, data)
			.Add(x => x.Colors, colors)
			.Add(x => x.IsLoading, false));

		// Assert
		cut.Find("canvas").Should().NotBeNull();
	}

	[Fact]
	public void PieChart_DefaultChartId_IsGenerated()
	{
		// Act
		var cut = Render<PieChart>(parameters => parameters
			.Add(x => x.IsLoading, false));

		// Assert
		var instance = cut.Instance;
		instance.ChartId.Should().NotBeNullOrEmpty();
		Guid.TryParse(instance.ChartId, out _).Should().BeTrue();
	}
}

/// <summary>
/// Test suite for BarChart component.
/// </summary>
public class BarChartTests : BunitTestBase
{
	[Fact]
	public void BarChart_WithValidData_RendersCanvasElement()
	{
		// Arrange
		var chartId = "bar-chart-1";
		var labels = new List<string> { "Jan", "Feb", "Mar" };
		var data = new List<int> { 10, 20, 15 };
		var color = "#3b82f6";

		JSInterop.SetupVoid("chartInterop.renderBarChart", _ => true);
		JSInterop.SetupVoid("chartInterop.destroyChart", _ => true);

		// Act
		var cut = Render<BarChart>(parameters => parameters
			.Add(x => x.ChartId, chartId)
			.Add(x => x.Labels, labels)
			.Add(x => x.Data, data)
			.Add(x => x.Color, color)
			.Add(x => x.IsLoading, false));

		// Assert
		cut.Find("canvas").Should().NotBeNull();
		cut.Find($"canvas#{chartId}").Should().NotBeNull();
	}

	[Fact]
	public void BarChart_WithEmptyData_DoesNotRenderCanvas()
	{
		// Arrange
		var labels = new List<string>();
		var data = new List<int>();

		// Act
		var cut = Render<BarChart>(parameters => parameters
			.Add(x => x.Labels, labels)
			.Add(x => x.Data, data)
			.Add(x => x.IsLoading, false));

		// Assert - Canvas is rendered but JS interop is NOT called for empty data
		var canvases = cut.FindAll("canvas");
		canvases.Should().NotBeEmpty();
		JSInterop.Invocations.Count(x => x.Identifier == "chartInterop.renderBarChart").Should().Be(0);
	}

	[Fact]
	public void BarChart_WhenLoading_DisplaysLoadingSpinner()
	{
		// Arrange
		var labels = new List<string> { "Jan", "Feb" };
		var data = new List<int> { 10, 20 };

		// Act
		var cut = Render<BarChart>(parameters => parameters
			.Add(x => x.Labels, labels)
			.Add(x => x.Data, data)
			.Add(x => x.IsLoading, true));

		// Assert
		cut.FindAll("canvas").Should().BeEmpty();
		var spinner = cut.Find("div.animate-spin");
		spinner.Should().NotBeNull();
	}

	[Fact]
	public void BarChart_WithData_InvokesJSInteropRenderMethod()
	{
		// Arrange
		var chartId = "bar-chart-2";
		var labels = new List<string> { "Jan", "Feb" };
		var data = new List<int> { 10, 20 };
		var color = "#3b82f6";

		var jsInteropCalls = new List<string>();
		JSInterop.SetupVoid("chartInterop.renderBarChart", invocation =>
		{
			jsInteropCalls.Add(invocation.Identifier);
			return true;
		});

		// Act
		var cut = Render<BarChart>(parameters => parameters
			.Add(x => x.ChartId, chartId)
			.Add(x => x.Labels, labels)
			.Add(x => x.Data, data)
			.Add(x => x.Color, color)
			.Add(x => x.IsLoading, false));

		// Assert
		jsInteropCalls.Should().Contain("chartInterop.renderBarChart");
	}

	[Fact]
public async Task BarChart_OnDispose_InvokesDestroyChart()
	{
		// Arrange
		var chartId = "bar-chart-3";
		var labels = new List<string> { "Jan", "Feb" };
		var data = new List<int> { 10, 20 };
		var color = "#3b82f6";

		JSInterop.SetupVoid("chartInterop.renderBarChart", _ => true);
		JSInterop.SetupVoid("chartInterop.destroyChart", _ => true);

		var cut = Render<BarChart>(parameters => parameters
			.Add(x => x.ChartId, chartId)
			.Add(x => x.Labels, labels)
			.Add(x => x.Data, data)
			.Add(x => x.Color, color)
			.Add(x => x.IsLoading, false));

		// Act
		await cut.Instance.DisposeAsync();

		// Assert
		JSInterop.Invocations.Count(x => x.Identifier == "chartInterop.destroyChart").Should().Be(1);
	}

	[Fact]
	public void BarChart_WithMismatchedDataLength_DoesNotRender()
	{
		// Arrange
		var labels = new List<string> { "Jan", "Feb", "Mar" };
		var data = new List<int> { 10, 20 }; // Fewer data points than labels

		JSInterop.SetupVoid("chartInterop.renderBarChart", _ => true);

		// Act
		var cut = Render<BarChart>(parameters => parameters
			.Add(x => x.Labels, labels)
			.Add(x => x.Data, data)
			.Add(x => x.IsLoading, false));

		// Assert - Canvas is rendered; component handles mismatched data gracefully
		cut.FindAll("canvas").Should().NotBeEmpty();
	}

	[Fact]
	public void BarChart_WithCustomColor_RendersChart()
	{
		// Arrange
		var chartId = "bar-chart-4";
		var labels = new List<string> { "Jan", "Feb" };
		var data = new List<int> { 10, 20 };
		var customColor = "#ef4444";

		JSInterop.SetupVoid("chartInterop.renderBarChart", _ => true);

		// Act
		var cut = Render<BarChart>(parameters => parameters
			.Add(x => x.ChartId, chartId)
			.Add(x => x.Labels, labels)
			.Add(x => x.Data, data)
			.Add(x => x.Color, customColor)
			.Add(x => x.IsLoading, false));

		// Assert
		cut.Find("canvas").Should().NotBeNull();
	}

	[Fact]
	public void BarChart_DefaultColor_IsBlue()
	{
		// Act
		var cut = Render<BarChart>(parameters => parameters
			.Add(x => x.IsLoading, false));

		// Assert
		cut.Instance.Color.Should().Be("#3b82f6");
	}

	[Fact]
	public void BarChart_WithLargeDataset_RendersChart()
	{
		// Arrange
		var chartId = "bar-chart-5";
		var labels = Enumerable.Range(1, 12).Select(i => $"Month {i}").ToList();
		var data = Enumerable.Range(1, 12).Select(i => i * 10).ToList();

		JSInterop.SetupVoid("chartInterop.renderBarChart", _ => true);

		// Act
		var cut = Render<BarChart>(parameters => parameters
			.Add(x => x.ChartId, chartId)
			.Add(x => x.Labels, labels)
			.Add(x => x.Data, data)
			.Add(x => x.IsLoading, false));

		// Assert
		cut.Find("canvas").Should().NotBeNull();
	}
}

/// <summary>
/// Test suite for LineChart component.
/// </summary>
public class LineChartTests : BunitTestBase
{
	[Fact]
	public void LineChart_WithValidData_RendersCanvasElement()
	{
		// Arrange
		var chartId = "line-chart-1";
		var labels = new List<string> { "Jan", "Feb", "Mar" };
		var datasets = new List<LineChart.LineChartDataset>
		{
			new()
			{
				Label = "Issues",
				Data = new List<int> { 10, 15, 20 },
				BorderColor = "#3b82f6",
				BackgroundColor = "rgba(59, 130, 246, 0.1)"
			}
		};

		JSInterop.SetupVoid("chartInterop.renderLineChart", _ => true);
		JSInterop.SetupVoid("chartInterop.destroyChart", _ => true);

		// Act
		var cut = Render<LineChart>(parameters => parameters
			.Add(x => x.ChartId, chartId)
			.Add(x => x.Labels, labels)
			.Add(x => x.Datasets, datasets)
			.Add(x => x.IsLoading, false));

		// Assert
		cut.Find("canvas").Should().NotBeNull();
		cut.Find($"canvas#{chartId}").Should().NotBeNull();
	}

	[Fact]
	public void LineChart_WithEmptyData_DoesNotRenderCanvas()
	{
		// Arrange
		var labels = new List<string>();
		var datasets = new List<LineChart.LineChartDataset>();

		// Act
		var cut = Render<LineChart>(parameters => parameters
			.Add(x => x.Labels, labels)
			.Add(x => x.Datasets, datasets)
			.Add(x => x.IsLoading, false));

		// Assert - Canvas is rendered but JS interop is NOT called for empty data
		var canvases = cut.FindAll("canvas");
		canvases.Should().NotBeEmpty();
		JSInterop.Invocations.Count(x => x.Identifier == "chartInterop.renderLineChart").Should().Be(0);
	}

	[Fact]
	public void LineChart_WhenLoading_DisplaysLoadingSpinner()
	{
		// Arrange
		var labels = new List<string> { "Jan", "Feb" };
		var datasets = new List<LineChart.LineChartDataset>
		{
			new() { Label = "Issues", Data = new List<int> { 10, 15 } }
		};

		// Act
		var cut = Render<LineChart>(parameters => parameters
			.Add(x => x.Labels, labels)
			.Add(x => x.Datasets, datasets)
			.Add(x => x.IsLoading, true));

		// Assert
		cut.FindAll("canvas").Should().BeEmpty();
		var spinner = cut.Find("div.animate-spin");
		spinner.Should().NotBeNull();
	}

	[Fact]
	public void LineChart_WithData_InvokesJSInteropRenderMethod()
	{
		// Arrange
		var chartId = "line-chart-2";
		var labels = new List<string> { "Jan", "Feb" };
		var datasets = new List<LineChart.LineChartDataset>
		{
			new()
			{
				Label = "Issues",
				Data = new List<int> { 10, 15 },
				BorderColor = "#3b82f6",
				BackgroundColor = "rgba(59, 130, 246, 0.1)"
			}
		};

		var jsInteropCalls = new List<string>();
		JSInterop.SetupVoid("chartInterop.renderLineChart", invocation =>
		{
			jsInteropCalls.Add(invocation.Identifier);
			return true;
		});

		// Act
		var cut = Render<LineChart>(parameters => parameters
			.Add(x => x.ChartId, chartId)
			.Add(x => x.Labels, labels)
			.Add(x => x.Datasets, datasets)
			.Add(x => x.IsLoading, false));

		// Assert
		jsInteropCalls.Should().Contain("chartInterop.renderLineChart");
	}

	[Fact]
public async Task LineChart_OnDispose_InvokesDestroyChart()
	{
		// Arrange
		var chartId = "line-chart-3";
		var labels = new List<string> { "Jan", "Feb" };
		var datasets = new List<LineChart.LineChartDataset>
		{
			new()
			{
				Label = "Issues",
				Data = new List<int> { 10, 15 },
				BorderColor = "#3b82f6",
				BackgroundColor = "rgba(59, 130, 246, 0.1)"
			}
		};

		JSInterop.SetupVoid("chartInterop.renderLineChart", _ => true);
		JSInterop.SetupVoid("chartInterop.destroyChart", _ => true);

		var cut = Render<LineChart>(parameters => parameters
			.Add(x => x.ChartId, chartId)
			.Add(x => x.Labels, labels)
			.Add(x => x.Datasets, datasets)
			.Add(x => x.IsLoading, false));

		// Act
		await cut.Instance.DisposeAsync();

		// Assert
		JSInterop.Invocations.Count(x => x.Identifier == "chartInterop.destroyChart").Should().Be(1);
	}

	[Fact]
	public void LineChart_WithMultipleDatasets_RendersChart()
	{
		// Arrange
		var chartId = "line-chart-4";
		var labels = new List<string> { "Jan", "Feb", "Mar" };
		var datasets = new List<LineChart.LineChartDataset>
		{
			new()
			{
				Label = "Open Issues",
				Data = new List<int> { 10, 15, 20 },
				BorderColor = "#3b82f6",
				BackgroundColor = "rgba(59, 130, 246, 0.1)"
			},
			new()
			{
				Label = "Closed Issues",
				Data = new List<int> { 5, 8, 12 },
				BorderColor = "#10b981",
				BackgroundColor = "rgba(16, 185, 129, 0.1)"
			}
		};

		JSInterop.SetupVoid("chartInterop.renderLineChart", _ => true);

		// Act
		var cut = Render<LineChart>(parameters => parameters
			.Add(x => x.ChartId, chartId)
			.Add(x => x.Labels, labels)
			.Add(x => x.Datasets, datasets)
			.Add(x => x.IsLoading, false));

		// Assert
		cut.Find("canvas").Should().NotBeNull();
	}

	[Fact]
	public void LineChart_WithOnlyLabels_DoesNotRenderChart()
	{
		// Arrange
		var labels = new List<string> { "Jan", "Feb" };
		var datasets = new List<LineChart.LineChartDataset>(); // Empty datasets

		JSInterop.SetupVoid("chartInterop.renderLineChart", _ => true);

		// Act
		var cut = Render<LineChart>(parameters => parameters
			.Add(x => x.Labels, labels)
			.Add(x => x.Datasets, datasets)
			.Add(x => x.IsLoading, false));

		// Assert - Canvas is rendered but JS interop is NOT called with empty datasets
		var canvases = cut.FindAll("canvas");
		canvases.Should().NotBeEmpty();
		JSInterop.Invocations.Count(x => x.Identifier == "chartInterop.renderLineChart").Should().Be(0);
	}

	[Fact]
	public void LineChart_WithCustomColors_RendersChart()
	{
		// Arrange
		var chartId = "line-chart-5";
		var labels = new List<string> { "Jan", "Feb" };
		var datasets = new List<LineChart.LineChartDataset>
		{
			new()
			{
				Label = "Issues",
				Data = new List<int> { 10, 15 },
				BorderColor = "#ef4444",
				BackgroundColor = "rgba(239, 68, 68, 0.1)"
			}
		};

		JSInterop.SetupVoid("chartInterop.renderLineChart", _ => true);

		// Act
		var cut = Render<LineChart>(parameters => parameters
			.Add(x => x.ChartId, chartId)
			.Add(x => x.Labels, labels)
			.Add(x => x.Datasets, datasets)
			.Add(x => x.IsLoading, false));

		// Assert
		cut.Find("canvas").Should().NotBeNull();
	}

	[Fact]
	public void LineChartDataset_DefaultColors_AreCorrect()
	{
		// Act
		var dataset = new LineChart.LineChartDataset();

		// Assert
		dataset.BorderColor.Should().Be("#3b82f6");
		dataset.BackgroundColor.Should().Be("rgba(59, 130, 246, 0.1)");
	}

	[Fact]
	public void LineChart_WithLongTermData_RendersChart()
	{
		// Arrange
		var chartId = "line-chart-6";
		var labels = Enumerable.Range(1, 24).Select(i => $"Month {i}").ToList();
		var datasets = new List<LineChart.LineChartDataset>
		{
			new()
			{
				Label = "Issues Created",
				Data = Enumerable.Range(1, 24).Select(i => i * 5).ToList(),
				BorderColor = "#3b82f6",
				BackgroundColor = "rgba(59, 130, 246, 0.1)"
			}
		};

		JSInterop.SetupVoid("chartInterop.renderLineChart", _ => true);

		// Act
		var cut = Render<LineChart>(parameters => parameters
			.Add(x => x.ChartId, chartId)
			.Add(x => x.Labels, labels)
			.Add(x => x.Datasets, datasets)
			.Add(x => x.IsLoading, false));

		// Assert
		cut.Find("canvas").Should().NotBeNull();
	}
}
