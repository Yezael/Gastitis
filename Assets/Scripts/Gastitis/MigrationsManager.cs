using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MigrationsManager
{
    public void MigrateDataIfNeeded(Dictionary<int, List<SpendingItem>> items)
    {
        foreach (var monthItemsPair in items)
        {
            var monthItems = monthItemsPair.Value;
            MigrateDataIfNeeded(monthItems);
        }
    }

    private void MigrateDataIfNeeded(List<SpendingItem> item)
    {
        for (int i = 0; i < item.Count; i++)
        {
            var currItem = item[i];
            MigrateDataIfNeeded(currItem);
        }
    }

    private void MigrateDataIfNeeded(SpendingItem item)
    {
        while(item.DataVersion < SpendingItem.LatestDataVersion)
        {
            MigrateDataFromVersion(item);
            item.DataVersion++;
        }
    }

    private void MigrateDataFromVersion(SpendingItem item)
    {
    }
}
