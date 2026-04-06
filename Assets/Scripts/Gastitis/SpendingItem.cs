using JetBrains.Annotations;
using System;

[Serializable]
public class SpendingItem
{
    public static int LatestDataVersion = 1;
    public int DataVersion = 0;

    public float SpendAmount;
    public string Description;

    public string CategoryID;
    public DateTime DateTime;
}
