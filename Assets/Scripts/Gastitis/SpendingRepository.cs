using System;
using System.Collections.Generic;
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

    public bool Modify(SpendingItem item, int month)
    {
        if (!All.TryGetValue(month, out var list))
        {
            return false;
        }

        var index = list.FindIndex(x => x.DateTime == item.DateTime);
        if (index == -1)
        {
            return false;
        }

        list[index] = item;
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

    public float GetTotal(int month, string categoryId)
    {
        float total = 0f;
        if (!All.TryGetValue(month, out var list))
        {
            return total;
        }

        foreach (var item in list)
        {
            if (string.IsNullOrEmpty(categoryId))
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