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
        Id = data.Id;
        SpendAmount = data.Value;
        Description = data.Description;
        CategoryID = data.categoryId;
        UTCDateTime = data.Date;
    }
}
