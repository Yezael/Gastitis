using System;

[Serializable]
public class SpendingItem
{
    public int Id;

    public decimal SpendAmount;
    public string Description;

    public int CategoryID;
    public DateTime UTCDateTime;

    public SpendingItem() { }

    public SpendingItem(ExpenseDTO data)
    {
        CopyFrom(data);
    }
    public SpendingItem(SpendingItem data)
    {
        CopyFrom(data);
    }

    public void CopyFrom(ExpenseDTO data)
    {
        Id = data.Id;
        SpendAmount = data.Value;
        Description = data.Description;
        CategoryID = data.categoryId;
        UTCDateTime = data.Date;
    }
    public void CopyFrom(SpendingItem data)
    {
        Id = data.Id;
        SpendAmount = data.SpendAmount;
        Description = data.Description;
        CategoryID = data.CategoryID;
        UTCDateTime = data.UTCDateTime;
    }
}
