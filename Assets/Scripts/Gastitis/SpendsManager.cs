using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using System;
using System.Text;
using System.IO;
using NativeShareNamespace;

public class SpendsManager : MonoBehaviour
{
	public static SpendsManager Instance;

	public ICurrentDateTimeProvider _dateTimeProvider;

	public NewSpendItemPopUp NewSpendItemPopUp;
	public NewCategoryBtnPopUp newCategoryPopUp;
	public RemoveCategoriesPopUp RemoveCategoriesPopUp;
	public SpendCategoryLibrary CategoryLibrary;
	public Button AddSpendingButton;
	public Button AddCategoryButton;
	public Button RemoveCategoriesButton;
	public Button ExportMonthSpendingsButton;
	public SpendItemUI SpendingButtonUIProtitype;
	public Transform SpendingsListContentParent;
	public TMP_Dropdown MonthSelectorDropdown;

	public TMP_Text TotalSpendText;

	public Dictionary<int, List<SpendingItem>> SpendingItems;
	public List<SpendItemUI> spendItemUIs = new List<SpendItemUI>();

	public int currMonthShowing = 0;

	bool _isQuitting = false;


	private void Awake()
	{
		if(Instance == null)
		{
			Instance = this;
			SpendingItems = new Dictionary<int, List<SpendingItem>>();
			return;
		}

		if (Instance != this)
		{
			Destroy(gameObject);
		}
	}

    private void Start()
    {
		_dateTimeProvider = new SystemDateTimeProvider();
		Initialize();
	}

	public void Initialize()
	{
		Instance = this;
		SpendingItems = GetDataFromLocalDisk();

		var customCategories = GetCategoriesDatasFromLocalDisk();
		if(customCategories != null)
		{
			CategoryLibrary.Categories = customCategories;
		}


		AddSpendingButton.onClick.AddListener(() =>
		{
			//StartCoroutine(GetNewSpending());
		});

		AddCategoryButton.onClick.AddListener(() =>
		{
			StartCoroutine(GetNewCategoryName());
		});

		RemoveCategoriesButton.onClick.AddListener(() =>
		{
			StartCoroutine(OpenCategoriesRemoval());
		});


		ExportMonthSpendingsButton.onClick.AddListener(ExportCurrentMonthSpendings);
		MonthSelectorDropdown.onValueChanged.AddListener(OnMonthDropdownChanged);


		ChangeCurrentMonthData(_dateTimeProvider.Now.Month);
		ChangeCurrentMonthUIBasedOnData();

		NewSpendItemPopUp.gameObject.SetActive(false);
	}


	public void OnMonthDropdownChanged(int newMonthFromDropdown)
	{
		var newMonth = newMonthFromDropdown + 1;
		ChangeCurrentMonthData(newMonth);
		ChangeCurrentMonthUIBasedOnData();
	}

	public void ChangeCurrentMonthDataAndUI(int newMonth)
	{
		ChangeCurrentMonthData(newMonth);
		ChangeCurrentMonthUIBasedOnData();
	}

	public void ChangeCurrentMonthData(int newMonth)
	{
		MonthSelectorDropdown.SetValueWithoutNotify(newMonth - 1);
		currMonthShowing = newMonth;
	}

	public void ChangeCurrentMonthUIBasedOnData()
	{
		RefreshTotalSpendings();
		RefreshVisibleElements();
	}

	void RefreshVisibleElements()
	{
		for(int i = 0; i < spendItemUIs.Count; i++)
		{
			Destroy(spendItemUIs[i].gameObject);
		}

		spendItemUIs.Clear();

		if (!SpendingItems.TryGetValue(currMonthShowing, out var newData)) return;

		for(int i = 0;i < newData.Count; i++)
		{
			AddSpendingItemUI(newData[i]);
		}
	}

	private IEnumerator GetNewSpending()
	{
		NewSpendingResult result = new NewSpendingResult();
		yield return NewSpendItemPopUp.GetNewSpending(result);
		if (result.IsCancelled)
		{
			yield break;
		}
		AddSpendingItem(result.NewSpending);
	}


	private IEnumerator OpenCategoriesRemoval()
	{
		yield return RemoveCategoriesPopUp.Execute(CategoryLibrary);
	}

	private IEnumerator GetNewCategoryName()
	{
		NewCategoryResult result = new NewCategoryResult();
		yield return newCategoryPopUp.GetNewCategoryInfo(result);
		if (result.IsCancelled)
		{
			yield break;
		}
		AddCategory(result.NewCategoryName);
	}

	public void AddSpendingItem(SpendingItem item)
	{
		var currMonth = currMonthShowing;
		if (SpendingItems.ContainsKey(currMonth))
		{
			SpendingItems[currMonth].Add(item);
		}
		else
		{
			SpendingItems.Add(currMonth, new List<SpendingItem>() { item });
		}

		AddSpendingItemUI(item);

		RefreshTotalSpendings();
	}

	public void AddCategory(string newName)
	{
		var allCats = CategoryLibrary.Categories;

		foreach (var cat in allCats)
		{
			//Category already exist
			if (cat.CategoryName == newName) return;
		}

		var newCat = new SpendCategory();
		newCat.CategoryName = newName;


		CategoryLibrary.Categories.Add(newCat);

	}

	void AddSpendingItemUI(SpendingItem item)
	{
		var newUI = Instantiate(SpendingButtonUIProtitype, SpendingsListContentParent);
		newUI.SetData(item);
		newUI.gameObject.SetActive(true);
		newUI.OnWantsToRemoveSpending += RemoveSpendingItem;
		spendItemUIs.Add(newUI);

	}

	void RefreshTotalSpendings()
	{
		var totalSpending = GetTotalSpending(currMonthShowing);
		TotalSpendText.text = NewSpendItemPopUp.ToFormattedNumber(totalSpending);
	}

	public void OnCategoryWasRemoved(string categoryID)
	{
		var categoryIdx = CategoryLibrary.Categories.FindIndex(x => x.CategoryID == categoryID);

		foreach(var itemLists in SpendingItems.Values)
		{
			for (var i = 0; i < itemLists.Count; i++)
			{
				var item = itemLists[i];
				if (item.Category != categoryIdx) continue;

				item.Category = 0;
			}
		}

		RefreshVisibleElements();
	}



	public void RemoveSpendingItem(SpendItemUI itemUI)
	{
		var item = itemUI.SpendingItemData;

		if(!SpendingItems.TryGetValue(currMonthShowing, out var spendingsFound))
		{
			Debug.LogError("No items found for the month of the item to remove");
			return;
		}

		spendingsFound.Remove(item);
		spendItemUIs.Remove(itemUI);

		Destroy(itemUI.gameObject);

		RefreshTotalSpendings();

	}

	public void ModifyExistentSpendingItem(SpendingItem item)
	{
		var itemsMonth = currMonthShowing;
		if (!SpendingItems.TryGetValue(itemsMonth, out var spendingsFound))
		{
			Debug.LogError("No items found for the month of the item to modify");
			return;
		}

		var oldItem = spendingsFound.FindIndex(x => x.Description == item.Description);
		if (oldItem == -1)
		{
			Debug.LogError("Item not found to modify");
			return;
		}

		spendingsFound[oldItem] = item;

		RefreshTotalSpendings();
	}

	public float GetTotalSpending(int month, SpendCategory category = null)
	{
		float total = 0;

		if (!SpendingItems.ContainsKey(month))
		{
			return total;
		}

		var spendingsFound = SpendingItems[month];

		var categoryIndex = CategoryLibrary.Categories.IndexOf(category);

		foreach (var item in spendingsFound)
		{
			if (category == null)
			{
				total += item.SpendAmount;
				continue;
			}

			if (item.Category == categoryIndex)
			{
				total += item.SpendAmount;
			}
		}
		return total;
	}

	public void SaveData()
	{
		var dataToSerialize = new SerializableData(SpendingItems);
		var json = JsonConvert.SerializeObject(dataToSerialize);
		PlayerPrefs.SetString("DATA", json);

		var categoriesJson = JsonConvert.SerializeObject(CategoryLibrary.Categories);
		PlayerPrefs.SetString("CATEGORIESDATAS", categoriesJson);

		PlayerPrefs.Save();
	}

	public Dictionary<int, List<SpendingItem>> GetDataFromLocalDisk()
	{
		var stringData = PlayerPrefs.GetString("DATA");
		if(string.IsNullOrEmpty(stringData))
		{
			return new Dictionary<int, List<SpendingItem>>();
		}

		var data = JsonConvert.DeserializeObject<SerializableData>(stringData);
		return data.SpendingItems;
	}

	public List<SpendCategory> GetCategoriesDatasFromLocalDisk()
	{
		var stringData = PlayerPrefs.GetString("CATEGORIESDATAS");
		if (string.IsNullOrEmpty(stringData))
		{
			return null; 
		}

		var data = JsonConvert.DeserializeObject<List<SpendCategory>>(stringData);
		return data;
	}


	void OnApplicationPause(bool pause)
	{
		if (pause && !_isQuitting)
			SaveData();
	}

	void OnApplicationFocus(bool focus)
	{
		if (!focus && !_isQuitting)
			SaveData();
	}

	void OnApplicationQuit()
	{
		_isQuitting = true;
		SaveData();
	}

	public void ExportCurrentMonthSpendings()
	{
		var monthName = MonthSelectorDropdown.options[currMonthShowing - 1].text;
		var monthData = SpendingItems[currMonthShowing];
		ExportToCSV(monthData, monthName);
	}

	public void ExportToCSV(List<SpendingItem> spendings, string month)
	{
		StringBuilder csv = new StringBuilder();

		// Header
		csv.AppendLine("Amount,Category,Description,Day");

		foreach (var s in spendings)
		{
			var currCategoryName = CategoryLibrary.Categories[s.Category].CategoryName;
			csv.AppendLine($"{s.SpendAmount},{currCategoryName},{Escape(s.Description)},{s.DateTime.Day}");
		}

		string path = Path.Combine(Application.persistentDataPath, "spendings" + month + ".csv");

		File.WriteAllText(path, csv.ToString());

		Debug.Log("CSV exported to: " + path);

#if PLATFORM_ANDROID
		var nativeShare = new NativeShare();
		nativeShare.AddFile(path);

		nativeShare.Share();
#endif
	}

	string Escape(string value)
	{
		if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
		{
			value = value.Replace("\"", "\"\"");
			return $"\"{value}\"";
		}
		return value;
	}
}

[Serializable]
public class SerializableData
{
	public Dictionary<int, List<SpendingItem>> SpendingItems;

	public SerializableData() { }

	public SerializableData(Dictionary<int, List<SpendingItem>> items)
	{ 
		SpendingItems = items;
	}

}


public interface ICurrentDateTimeProvider
{
	DateTime Now { get; }
}

public class SystemDateTimeProvider : ICurrentDateTimeProvider
{
	public DateTime Now => DateTime.Now;
}

public class TestDateTimeProvider : ICurrentDateTimeProvider
{
	public DateTime Now { get; set; } = new DateTime(2026, 02, 16);
}