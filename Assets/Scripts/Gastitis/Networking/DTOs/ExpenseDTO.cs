using Codice.Client.Common.FsNodeReaders;
using System;
using System.Collections.Generic;

[Serializable]
public class ExpenseDTO
{
    public int Id;
    public decimal Value;
    public string Description;
    public int categoryId;
    public DateTime Date;

    public ExpenseDTO()
    {

    }

    public ExpenseDTO(SpendingItem item)
    {
        Id = item.Id;
        Value = item.SpendAmount;
        Description = item.Description;
        categoryId = item.CategoryID;
        Date = item.UTCDateTime;
    }

    public override string ToString()
    {
        var str = string.Empty;
        str += "Id: " + Id + "\n";
        str += "Value: " + Value + "\n";
        str += "description: " + Description + "\n";
        str += "categoryID: " + categoryId + "\n";
        str += "Date: " + Date + "\n";
        return str;
    }
}

[Serializable]
public class UpdateExpenseDTO
{
    public decimal Value;
    public string Description;
    public int CategoryId;

    public UpdateExpenseDTO(SpendingItem item)
    {
        Value = item.SpendAmount;
        Description = item.Description;
        CategoryId = item.CategoryID;
    }

    public UpdateExpenseDTO(ExpenseDTO item)
    {
        Value = item.Value;
        Description = item.Description;
        CategoryId = item.categoryId;
    }
}


[Serializable]
public class ExpensesSummaryDTO
{
    public decimal TotalAmount;
    public int ExpensesCount;

    public override string ToString()
    {
        var str = string.Empty;
        str += "Total: " + TotalAmount + "\n";
        str += "Count: " + ExpensesCount + "\n";
        return str;
    }
}

[Serializable]
public class ExpensesCategorySummaryResponseDTO
{
    public int CategoryId;
    public string CategoryName;
    public decimal TotalAmount;
    public int ExpenseCount;

    public override string ToString()
    {
        var str = string.Empty;
        str += "CategoryId: " + CategoryId + "\n";
        str += "CategoryName: " + CategoryName + "\n";
        str += "TotalAmount: " + TotalAmount + "\n";
        str += "ExpenseCount: " + ExpenseCount + "\n";
        return str;
    }
}

[Serializable]
public class PagedResponseDTO<T>
{
    public List<T> Items;
    public int Page;
    public int PageSize;
    public int TotalCount;
    public int TotalPages;

    public override string ToString()
    {
        var str = string.Empty;
        if( Items != null)
        {
            for (int i = 0; i < Items.Count; i++)
            {
                str += $"Item at: {i} {Items[i].ToString()}" + "\n";
            }
        }

        str += "Page:" + Page + "\n";
        str += "PageSize:" + PageSize + "\n";
        str += "TotalCount:" + TotalCount + "\n";
        str += "TotalPages:" + TotalPages + "\n";

        return str;
    }

}

public class ExpenseFilterDTO
{
    public int? Year;
    public int? Month;
    public int? CategoryID;
    public string Keyword;

    public int Page = 1;
    public int PageSize = 20;
}

public class ExpenseSortingDTO
{
    public SortBy SortBy;
    public SortDirection SortDirection;
}

public enum SortBy
{
    Date = 0,
    Value = 1,
}

public enum SortDirection
{
    Desc = 0,
    Asc = 1
}


