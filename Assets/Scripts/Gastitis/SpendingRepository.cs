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

    public void AddOrModify(SpendingItem item, int month)
    {
        if (!All.ContainsKey(month))
        {
            All.Add(month, new List<SpendingItem>());
        }

        InternalAddOrModifyExpense(All[month], item);
    }

    public void AddOrModify(ExpenseDTO item, int month)
    {
        if (!All.ContainsKey(month))
        {
            All.Add(month, new List<SpendingItem>());
        }

        InternalAddOrModifyExpense(All[month], item);
    }

    public void AddOrModify(List<ExpenseDTO> cloudItems, int month)
    {
        if (!All.ContainsKey(month))
        {
            All.Add(month, new List<SpendingItem>());
        }

        var monthData = All[month];

        InternalAddOrModifyExpense(monthData, cloudItems);
    }

    void InternalAddOrModifyExpense(List<SpendingItem> monthData, ExpenseDTO data)
    {
        var foundPrevious = monthData.Find(x => x.Id == data.Id);
        if (foundPrevious != null)
        {
            foundPrevious.CopyFrom(data);
            return;
        }

        var newItem = new SpendingItem(data);
        monthData.Add(newItem);
    }

    void InternalAddOrModifyExpense(List<SpendingItem> monthData, SpendingItem data)
    {
        var foundPrevious = monthData.Find(x => x.Id == data.Id);
        if (foundPrevious != null)
        {
            foundPrevious.CopyFrom(data);
            return;
        }

        var newItem = new SpendingItem(data);
        monthData.Add(newItem);
    }

    void InternalAddOrModifyExpense(List<SpendingItem> monthData, List<ExpenseDTO> cloudItems)
    {
        for (int i = 0; i < cloudItems.Count; i++)
        {
            var foundPrevious = monthData.Find(x => x.Id == cloudItems[i].Id);
            if (foundPrevious != null)
            {
                foundPrevious.CopyFrom(cloudItems[i]);
                continue;
            }

            var newItem = new SpendingItem(cloudItems[i]);
            monthData.Add(newItem);
        }
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