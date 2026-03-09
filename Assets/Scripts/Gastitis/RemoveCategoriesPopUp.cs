using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RemoveCategoriesPopUp : MonoBehaviour
{
	public SpendCategoryLibrary CategoryLibrary;
	public Button ClosePopUpBtn;

	public bool wantsToExitPopUp;

	public List<CategoryItemUI> CategoryItems;
	public CategoryItemUI CategoryItemPrototype;
	public Transform CategoriesContentParent;


	private void Awake()
	{
		ClosePopUpBtn.onClick.AddListener(() =>
		{
			wantsToExitPopUp = true;
		});

		CategoryItems = new List<CategoryItemUI>();
		CategoryItemPrototype.gameObject.SetActive(false);
	}

	public void OnWantsToRemoveCategory(CategoryItemUI toRemove)
	{
		SpendsManager.Instance.OnCategoryWasRemoved(toRemove.Data.CategoryID);
		var idx = CategoryLibrary.Categories.FindIndex(x => x.CategoryID == toRemove.Data.CategoryID);
		CategoryLibrary.Categories.RemoveAt(idx);
		CategoryItems.Remove(toRemove);
		GameObject.Destroy(toRemove.gameObject);
		RefreshCategoryItems();
	}

	public IEnumerator Execute(SpendCategoryLibrary library)
	{
		gameObject.SetActive(true);
		CategoryLibrary = library;
		RefreshCategoryItems();
		wantsToExitPopUp = false;

		while (!wantsToExitPopUp)
		{
			yield return null;
		}

		gameObject.SetActive(false);
	}

	void RefreshCategoryItems()
	{
		for (int i = 0; i < CategoryItems.Count; i++)
		{
			Destroy(CategoryItems[i].gameObject);
		}

		CategoryItems.Clear();


		var datas = CategoryLibrary.Categories;
		for (int i = 0; i < datas.Count; i++)
		{
			AddCategoryUI(datas[i]);
			CategoryItems[i].gameObject.SetActive(true);
			CategoryItems[i].RemoveSpendingBtn.gameObject.SetActive(i != 0);
		}

	}

	void AddCategoryUI(SpendCategory categoryData)
	{
		var newItem = Instantiate(CategoryItemPrototype, CategoriesContentParent);
		CategoryItems.Add(newItem);
		newItem.SetData(categoryData);
		newItem.OnWantsToRemoveCategory += OnWantsToRemoveCategory;
	}
}
