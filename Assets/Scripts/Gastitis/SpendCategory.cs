using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SpendCategory
{
    public string Name;
    public int Id = -1;
    public bool IsSystemCategory;


	public SpendCategory()
    {
        Id = -1;
        Name = "NO NAME";
        IsSystemCategory = false;
    }

    public SpendCategory(CategoryDTO categoryData)
    {
        Id = categoryData.Id;
        Name = categoryData.Name;
		IsSystemCategory = categoryData.IsSystemCategory;
	}

    public SpendCategory(SpendCategory copyFrom)
    {
        Id = copyFrom.Id; 
        Name = copyFrom.Name;
		IsSystemCategory = copyFrom.IsSystemCategory;
	}
}
