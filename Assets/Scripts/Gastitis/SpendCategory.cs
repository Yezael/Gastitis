using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SpendCategory
{
    public string Name;
    public int Id = -1;

    
    public SpendCategory()
    {
        Id = -1;
        Name = "NO NAME";
    }

    public SpendCategory(CategoryDTO categoryData)
    {
        Id = categoryData.Id;
        Name = categoryData.Name;
    }

    public SpendCategory(SpendCategory copyFrom)
    {
        Id = copyFrom.Id; 
        Name = copyFrom.Name;
    }
}
