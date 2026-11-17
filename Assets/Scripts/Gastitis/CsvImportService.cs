using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public class CsvImportService : IImportService
{
	private const string Header = "Amount,Category,Description,Day";
	private readonly SpendCategoryLibrary _categories;


	public CsvImportService(SpendCategoryLibrary categories)
	{
		_categories = categories ?? throw new ArgumentNullException(nameof(categories));
	}

	public List<SpendingItem> Import(string path, int year, int month)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			throw new ArgumentException("A CSV path is required.", nameof(path));
		}

		if (month < 1 || month > 12)
		{
			throw new ArgumentOutOfRangeException(nameof(month), "The month must be between 1 and 12.");
		}

		if (!File.Exists(path))
		{
			throw new FileNotFoundException("The CSV file could not be found.", path);
		}

		var records = ParseRecords(File.ReadAllText(path));
		if (records.Count == 0 || records[0].Count != 4 ||
			!string.Equals(string.Join(",", records[0]), Header, StringComparison.OrdinalIgnoreCase))
		{
			throw new FormatException("The CSV header must be: " + Header);
		}

		var result = new List<SpendingItem>();
		for (int i = 1; i < records.Count; i++)
		{
			var fields = records[i];
			if (fields.Count == 1 && string.IsNullOrWhiteSpace(fields[0]))
			{
				continue;
			}

			if (fields.Count != 4)
			{
				Debug.LogWarning($"Skipping CSV row {i + 1}: expected 4 fields.");
				continue;
			}

			if (!decimal.TryParse(fields[0].Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out var amount) &&
				!decimal.TryParse(fields[0].Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out amount))
			{
				Debug.LogWarning($"Skipping CSV row {i + 1}: invalid amount.");
				continue;
			}

			if (!int.TryParse(fields[3].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var day) ||
				day < 1 || day > DateTime.DaysInMonth(year, month))
			{
				Debug.LogWarning($"Skipping CSV row {i + 1}: invalid day.");
				continue;
			}

			var categoryName = fields[1].Trim();
			if (string.IsNullOrWhiteSpace(categoryName))
			{
				Debug.LogWarning($"Skipping CSV row {i + 1}: category is empty.");
				continue;
			}

			var category = _categories.GetCategoryByName(categoryName);
			if (category == null)
			{
				category = CreateCategory(categoryName);
			}

			result.Add(new SpendingItem
			{
				SpendAmount = amount,
				CategoryID = category.Id,
				Description = fields[2],
				UTCDateTime = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc)
			});
		}

		return result;
	}

	private SpendCategory CreateCategory(string name)
	{
		if (_categories.Categories == null)
		{
			_categories.Categories = new List<SpendCategory>();
		}

		var nextId = 1;
		for (int i = 0; i < _categories.Categories.Count; i++)
		{
			nextId = Math.Max(nextId, _categories.Categories[i].Id + 1);
		}

		var category = new SpendCategory
		{
			Id = nextId,
			Name = name,
			IsSystemCategory = false
		};
		_categories.Categories.Add(category);
		return category;
	}

	private static List<List<string>> ParseRecords(string text)
	{
		var records = new List<List<string>>();
		var record = new List<string>();
		var field = new System.Text.StringBuilder();
		var quoted = false;

		for (int i = 0; i < text.Length; i++)
		{
			var current = text[i];
			if (current == '"')
			{
				if (quoted && i + 1 < text.Length && text[i + 1] == '"')
				{
					field.Append('"');
					i++;
				}
				else
				{
					quoted = !quoted;
				}
			}
			else if (current == ',' && !quoted)
			{
				record.Add(field.ToString());
				field.Length = 0;
			}
			else if ((current == '\r' || current == '\n') && !quoted)
			{
				if (current == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
				{
					i++;
				}

				record.Add(field.ToString());
				field.Length = 0;
				records.Add(record);
				record = new List<string>();
			}
			else
			{
				field.Append(current);
			}
		}

		if (field.Length > 0 || record.Count > 0)
		{
			record.Add(field.ToString());
			records.Add(record);
		}

		return records;
	}
}
