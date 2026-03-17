// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     CsvExportHelper.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web
// =======================================================

using System.Reflection;
using System.Text;

namespace Web.Helpers;

/// <summary>
/// Helper class for exporting data to CSV format.
/// </summary>
public static class CsvExportHelper
{
	/// <summary>
	/// Exports a collection of objects to CSV format.
	/// </summary>
	/// <typeparam name="T">The type of objects to export.</typeparam>
	/// <param name="data">The collection of objects to export.</param>
	/// <returns>CSV data as byte array.</returns>
	public static byte[] ExportToCsv<T>(IEnumerable<T> data)
	{
		var csv = new StringBuilder();
		var type = typeof(T);
		var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

		// Write header
		csv.AppendLine(string.Join(",", properties.Select(p => $"\"{p.Name}\"")));

		// Write data rows
		foreach (var item in data)
		{
			var values = properties.Select(p =>
			{
				var value = p.GetValue(item);
				return value is null ? string.Empty : EscapeCsvValue(value.ToString() ?? string.Empty);
			});
			csv.AppendLine(string.Join(",", values));
		}

		return Encoding.UTF8.GetBytes(csv.ToString());
	}

	/// <summary>
	/// Escapes a CSV value by wrapping in quotes and escaping internal quotes.
	/// </summary>
	/// <param name="value">The value to escape.</param>
	/// <returns>Escaped CSV value.</returns>
	private static string EscapeCsvValue(string value)
	{
		if (string.IsNullOrEmpty(value))
			return "\"\"";

		// Escape double quotes by doubling them
		var escaped = value.Replace("\"", "\"\"");

		// Wrap in quotes if contains comma, newline, or quotes
		if (escaped.Contains(',') || escaped.Contains('\n') || escaped.Contains('\r') || escaped.Contains('\"'))
		{
			return $"\"{escaped}\"";
		}

		return $"\"{escaped}\"";
	}
}
