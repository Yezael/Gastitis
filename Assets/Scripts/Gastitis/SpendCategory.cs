using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SpendCategory
{
    public string CategoryName;
    public string CategoryID = Guid.NewGuid().ToString();

    
    public SpendCategory()
    {
        CategoryID = Guid.NewGuid().ToString();
        CategoryName = "NO NAME";
    }

    public SpendCategory(SpendCategory copyFrom)
    {
        CategoryID = copyFrom.CategoryID; 
        CategoryName = copyFrom.CategoryName;
    }
}
