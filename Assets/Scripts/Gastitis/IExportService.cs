
using System.Collections.Generic;

public interface IExportService
{
    void Export(List<SpendingItem> spendings, string monthName);
}