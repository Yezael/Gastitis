using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "NewSpendCategoryLibrary", menuName = "Spend Category Library")]
public class SpendCategoryLibrary : ScriptableObject
{
    
    private static SpendCategoryLibrary _instance;
	public static SpendCategoryLibrary Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<SpendCategoryLibrary>("SpendCategoryLibrary");
            }
            return _instance;

        }
    }


	public List<SpendCategory> Categories;


	public bool AssignNewIDToCat;
	public int catToAssignNewId;

	void OnValidate()
	{
		if (!AssignNewIDToCat) return;

		AssignNewIDToCat = false;
		if (catToAssignNewId < 0)
		{
			for (int i = 0; i < Categories.Count; i++)
			{
				var curr = Categories[i];
				curr.CategoryID = Guid.NewGuid().ToString();
			}
			catToAssignNewId = -1;
			return;
		}

		var cat = Categories[catToAssignNewId];
		cat.CategoryID = Guid.NewGuid().ToString();
		catToAssignNewId = -1;
	}


	public SpendCategory GetCategoryByName(string name)
    {
        return Categories.Find(cat => cat.CategoryName == name);
	}

	public SpendCategory GetCategoryByID(string id)
	{
		return Categories.Find(cat => cat.CategoryID == id);
	}
}
