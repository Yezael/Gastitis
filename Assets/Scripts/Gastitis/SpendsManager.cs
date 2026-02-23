using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using System;

public class SpendsManager : MonoBehaviour
{
	public static SpendsManager Instance;

	public ICurrentDateTimeProvider _dateTimeProvider;

	public NewSpendItemPopUp NewSpendItemPopUp;
	public SpendCategoryLibrary CategoryLibrary;
	public Button AddSpendingButton;
	public SpendItemUI SpendingButtonUIProtitype;
	public Transform SpendingsListContentParent;
	public TMP_Dropdown MonthSelectorDropdown;

	public TMP_Text TotalSpendText;

	public Dictionary<int, List<SpendingItem>> SpendingItems;
	public List<SpendItemUI> spendItemUIs = new List<SpendItemUI>();

	public int currMonthShowing = 0;

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


		AddSpendingButton.onClick.AddListener(() =>
		{
			StartCoroutine(GetNewSpending());
		});

		MonthSelectorDropdown.onValueChanged.AddListener(ChangeCurrentMonthDataAndUI);


		ChangeCurrentMonthData(_dateTimeProvider.Now.Month);
		ChangeCurrentMonthUIBasedOnData();

		NewSpendItemPopUp.gameObject.SetActive(false);
	}

	public void ChangeCurrentMonthDataAndUI(int newMonth)
	{
		ChangeCurrentMonthData(newMonth);
		ChangeCurrentMonthUIBasedOnData();
	}

	public void ChangeCurrentMonthData(int newMonth)
	{
		MonthSelectorDropdown.SetValueWithoutNotify(newMonth);
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

	public void AddSpendingItem(SpendingItem item)
	{
		var currMonth = _dateTimeProvider.Now.Month - 1;
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
		TotalSpendText.text = totalSpending.ToString("C");
	}



	public void RemoveSpendingItem(SpendItemUI itemUI)
	{
		var item = itemUI.SpendingItemData;

		var itemsMonth = item.DateTime.Month;
		if(!SpendingItems.TryGetValue(itemsMonth, out var spendingsFound))
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
		var itemsMonth = item.DateTime.Month;
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

    private void OnApplicationQuit()
    {
		SaveData();
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