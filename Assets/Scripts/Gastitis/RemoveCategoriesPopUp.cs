using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
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

	public bool IsOpen => gameObject.activeSelf;



	private void Awake()
	{
		ClosePopUpBtn.onClick.AddListener(() => wantsToExitPopUp = true);

		CategoryItems = new List<CategoryItemUI>();
		CategoryItemPrototype.gameObject.SetActive(false);
	}

	public async Task OnWantsToRemoveCategory(CategoryItemUI toRemove)
	{
		toRemove.DisableDeleteBtn();
		await SpendsManager.Instance.RemoveCategoryItemAsync(toRemove.Data);
	}

	public void ForcePopUpExit()
	{
		wantsToExitPopUp = true;
		gameObject.SetActive(false);
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

	public void RefreshCategoryItems()
	{
		for (int i = 0; i < CategoryItems.Count; i++)
		{
			CategoryItems[i].OnWantsToRemoveCategory -= OnWantsToRemoveCategory;
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
