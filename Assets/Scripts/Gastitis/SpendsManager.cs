using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public class SpendsManager : MonoBehaviour
{
    public static SpendsManager Instance;
    public MigrationsManager MigrationManager = new MigrationsManager();
    public SpendCategoryLibrary CategoryLibrary;
    public UIManager UIManager;

    public ICurrentDateTimeProvider _dateTimeProvider;

    public SpendingRepository SpendingItems = new SpendingRepository();

    public int currMonthShowing = 0;
    public string currCategoryFilter = string.Empty;

    bool _isQuitting = false;

    private IExportService _exportService;

    public Action OnDirty;

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
        Initialize();
    }

    public void Initialize()
    {
        _dateTimeProvider = new SystemDateTimeProvider();
        var data = GetDataFromLocalDisk();
        SpendingItems = new SpendingRepository(data);

        var customCategories = GetCategoriesDatasFromLocalDisk();
        if (customCategories != null)
        {
            CategoryLibrary.Categories = customCategories;
        }

        MigrationManager.MigrateDataIfNeeded(SpendingItems.All);
        UIManager.Initialize();

        _exportService = new CsvExportService(CategoryLibrary);
        ChangeCurrentMonthData(_dateTimeProvider.Now.Month);
    }

    public void ChangeCurrentMonthData(int newMonth)
    {
        ChangeCurrentMonthDataWithoutNotify(newMonth);
        OnDirty?.Invoke();
    }

    public void ChangeCurrentMonthDataWithoutNotify(int newMonth)
    {
        currMonthShowing = newMonth;
    }

    public void AddSpendingItem(SpendingItem item)
    {
        SpendingItems.Add(item, currMonthShowing);
        OnDirty?.Invoke();
    }

    public void AddCategory(string newName, List<SpendCategory> categories)
    {
        for (var i = 0; i < categories.Count; i++)
        {
            var cat = categories[i];
            if (cat.CategoryName == newName) return;
        }

        var newCat = new SpendCategory();
        newCat.CategoryName = newName;

        categories.Add(newCat);
        OnDirty?.Invoke();
    }

    public bool RemoveSpendingItem(SpendingItem item)
    {
        var removed = SpendingItems.Remove(item, currMonthShowing);
        if (!removed)
        {
            Debug.LogError("No items found for the month of the item to remove");
            return false;
        }

        OnDirty?.Invoke();
        return true;
    }

    public void ModifyExistentSpendingItem(SpendingItem item)
    {
        var modified = SpendingItems.Modify(item, currMonthShowing);
        if (!modified)
        {
            Debug.LogError("Item not found to modify");
            return;
        }

        OnDirty?.Invoke();
    }

    public float GetTotalSpending(int month, string categoryID)
    {
        return SpendingItems.GetTotal(month, categoryID);
    }

    public void SaveData()
    {
        var dataToSerialize = new SerializableData(SpendingItems.All);
        var json = JsonConvert.SerializeObject(dataToSerialize);
        PlayerPrefs.SetString("DATA", json);

        var categoriesJson = JsonConvert.SerializeObject(CategoryLibrary.Categories);
        PlayerPrefs.SetString("CATEGORIESDATAS", categoriesJson);

        PlayerPrefs.Save();
    }

    public Dictionary<int, List<SpendingItem>> GetDataFromLocalDisk()
    {
        var stringData = PlayerPrefs.GetString("DATA");
        if (string.IsNullOrEmpty(stringData))
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
        {
            SaveData();
        }
    }

    void OnApplicationFocus(bool focus)
    {
#if !UNITY_EDITOR
        if (!focus && !_isQuitting)
        {
            SaveData();
        }
#endif
    }

    void OnApplicationQuit()
    {
        _isQuitting = true;
        SaveData();
    }

    public void ExportCurrentMonthSpendings(int month, string monthName)
    {
        if (month <= 0) return;

        var monthData = SpendingItems.GetByMonth(month);
        if (monthData == null) return;

        _exportService.Export(monthData, monthName);
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