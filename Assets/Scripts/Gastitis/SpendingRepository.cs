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

    public void Add(SpendingItem item, int month)
    {
        if (!All.ContainsKey(month))
        {
            All.Add(month, new List<SpendingItem>());
        }

        All[month].Add(item);
    }

    public bool Remove(SpendingItem item, int month)
    {
        if (!All.TryGetValue(month, out var list))
        {
            return false;
        }

        return list.Remove(item);
    }
    public bool RemoveById(int itemId, int month)
    {
        if (!All.TryGetValue(month, out var list)) return false;
        var found = list.FindIndex(x => x.Id == itemId);
        if (found == -1) return false;

        list.RemoveAt(found);
        return true;
    }

    public List<SpendingItem> GetByMonth(int month)
    {
        if (!All.TryGetValue(month, out var list))
        {
            return new List<SpendingItem>();
        }

        return list;
    }

    public decimal GetTotal(int month, int categoryId)
    {
        decimal total = 0;
        if (!All.TryGetValue(month, out var list))
        {
            return total;
        }

        foreach (var item in list)
        {
            if (categoryId == -1)
            {
                total += item.SpendAmount;
                continue;
            }

            if (item.CategoryID != categoryId)
            {
                continue;
            }

            total += item.SpendAmount;
        }

        return total;
    }
}