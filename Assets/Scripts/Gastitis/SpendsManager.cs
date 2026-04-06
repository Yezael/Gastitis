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
	public MigrationsManager MigrationManager = new();

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
	public TMP_Dropdown CategorySelectorDropdown;

	public string currentCategorySelected = null;

	public TMP_Text TotalSpendText;

	public Dictionary<int, List<SpendingItem>> SpendingItems = new();
	public List<SpendItemUI> spendItemUIs = new List<SpendItemUI>();

	public int currMonthShowing = 0;
	public string currCategoryFilter = string.Empty;

	bool _isQuitting = false;


	private void Awake()
	{
        if (Instance == null)
		{
			Instance = this;
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

        MigrationManager.MigrateDataIfNeeded(SpendingItems);


        AddSpendingButton.onClick.AddListener(() =>
		{
			StartCoroutine(GetNewSpending());
		});

		AddCategoryButton.onClick.AddListener(() =>
		{
			StartCoroutine(GetNewCategoryName());
		});

		RemoveCategoriesButton.onClick.AddListener(() =>
		{
			StartCoroutine(OpenCategoriesRemoval());
		});

		CategorySelectorDropdown.options.Clear();

		List<TMP_Dropdown.OptionData> options = new();
		var defaultOpt = new TMP_Dropdown.OptionData("NONE");
		options.Add(defaultOpt);

        for (int i = 0; i < CategoryLibrary.Categories.Count; i++)
		{
			var newOpt = new TMP_Dropdown.OptionData(CategoryLibrary.Categories[i].CategoryName);
			options.Add(newOpt);
		}

		CategorySelectorDropdown.AddOptions(options);
		CategorySelectorDropdown.SetValueWithoutNotify(0);


		ExportMonthSpendingsButton.onClick.AddListener(ExportCurrentMonthSpendings);
		MonthSelectorDropdown.onValueChanged.AddListener(OnMonthDropdownChanged);
		CategorySelectorDropdown.onValueChanged.AddListener(OnCategoryDropdownChanged);


		ChangeCurrentMonthData(_dateTimeProvider.Now.Month);
		RefreshUIBaseOnCurrentData();

		NewSpendItemPopUp.gameObject.SetActive(false);
	}


	public void OnMonthDropdownChanged(int newMonthFromDropdown)
	{
		var newMonth = newMonthFromDropdown + 1;
		ChangeCurrentMonthData(newMonth);
		RefreshUIBaseOnCurrentData();
	}

    public void ChangeCurrentMonthDataAndUI(int newMonth)
    {
        ChangeCurrentMonthData(newMonth);
        RefreshUIBaseOnCurrentData();
    }

    public void ChangeCurrentMonthData(int newMonth)
    {
        MonthSelectorDropdown.SetValueWithoutNotify(newMonth - 1);
        currMonthShowing = newMonth;
    }

    public void OnCategoryDropdownChanged(int newCategory)
	{
		//To take into account the "none" filter that is the 0 index of the dropdown
		newCategory--;
		if (newCategory < 0)
		{
			currCategoryFilter = string.Empty;
		}
		else
		{
            currCategoryFilter = CategoryLibrary.Categories[newCategory].CategoryID;
        }

		RefreshUIBaseOnCurrentData();
    }


    public void RefreshUIBaseOnCurrentData()
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
			if (string.IsNullOrEmpty(currCategoryFilter))
			{
                AddSpendingItemUI(newData[i]);
				continue;
            }

			if (newData[i].CategoryID != currCategoryFilter) continue;

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
		var totalSpending = GetTotalSpending(currMonthShowing, currCategoryFilter);
		TotalSpendText.text = NewSpendItemPopUp.ToFormattedNumber(totalSpending);
	}

	public void OnCategoryWasRemoved(string categoryID)
	{
		var defaultID = CategoryLibrary.Categories[0].CategoryID;

		foreach(var itemLists in SpendingItems.Values)
		{
			for (var i = 0; i < itemLists.Count; i++)
			{
				var item = itemLists[i];
				if (item.CategoryID != categoryID) continue;

				item.CategoryID = defaultID;
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

	public float GetTotalSpending(int month, string categoryID)
	{
		float total = 0;

		if (!SpendingItems.ContainsKey(month))
		{
			return total;
		}

		var spendingsFound = SpendingItems[month];

		foreach (var item in spendingsFound)
		{
			if (string.IsNullOrEmpty(categoryID))
			{
				total += item.SpendAmount;
				continue;
			}

			if (item.CategoryID != categoryID) continue;

			total += item.SpendAmount;
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
			var currCategoryName = CategoryLibrary.GetCategoryByID(s.CategoryID).CategoryName;
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