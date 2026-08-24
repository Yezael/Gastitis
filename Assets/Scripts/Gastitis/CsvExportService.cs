using System.Collections.Generic;
using System.Text;
using System.IO;
using UnityEngine;

public class CsvExportService : IExportService
{
    private readonly SpendCategoryLibrary _categories;

    public CsvExportService(SpendCategoryLibrary categories)
    {
        _categories = categories;
    }

    public void Export(List<SpendingItem> spendings, string monthName)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Amount,Category,Description,Day");

        foreach (var s in spendings)
        {
            var category = _categories.GetCategoryByID(s.CategoryID);
            var name = category.Name;
            csv.AppendLine(string.Format("{0},{1},{2},{3}", s.SpendAmount, name, Escape(s.Description), s.UTCDateTime.Day));
        }

        var path = Path.Combine(Application.persistentDataPath, "spendings" + monthName + ".csv");
        File.WriteAllText(path, csv.ToString());

        Debug.Log("CSV exported to: " + path);

#if PLATFORM_ANDROID
        var nativeShare = new NativeShare();
        nativeShare.AddFile(path);
        nativeShare.Share();
#endif
    }

    string Escape(string value)
    {
        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
        {
            value = value.Replace("\"", "\"\"");
            return "\"" + value + "\"";
        }

        return value;

    }
}