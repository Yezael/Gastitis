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
    public SpendCategory GetCategoryByName(string name)
    {
        return Categories.Find(cat => cat.CategoryName == name);
	}
}
