using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class SpendingRepository
{
    public Dictionary<int, List<SpendingItem>> All { get; private set; }

    public SpendingRepository(Dictionary<int, List<SpendingItem>> initial = null)
    {
        if (initial == null)
        {
            All = new Dictionary<int, List<SpendingItem>>();
            return;
        }

        All = initial;
    }

    public void SetMonthSpendings(List<ExpenseDTO> cloudItems, int month)
    {
        if (!All.ContainsKey(month))
        {
            All.Add(month, new List<SpendingItem>());
        }

        var monthData = All[month];

		monthData.Clear();

		for (int i = 0; i < cloudItems.Count; i++)
		{
			var newItem = new SpendingItem(cloudItems[i]);
			monthData.Add(newItem);
		}
    }

    public List<SpendingItem> GetByMonth(int month)
    {
        if (!All.TryGetValue(month, out var list))
        {
            return new List<SpendingItem>();
        }

        return list;
    }

    public void Clear()
    {
        All.Clear();
    }
}