using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Threading.Tasks;
using TMPro;
using System.Runtime.CompilerServices;
using GluonGui.Dialog;

public class SpendsManager : MonoBehaviour
{
    public static SpendsManager Instance;

    public GastitisAPIManager APIManager = new GastitisAPIManager();
    public SpendCategoryLibrary CategoryLibrary;
    public UIManager UIManager;

    public ICurrentDateTimeProvider _dateTimeProvider;

    public SpendingRepository SpendingItems = new SpendingRepository();

    public int currMonthShowing = 0;
    public int currCategoryIDSelected = -1;

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

    public async void Initialize()
    {
        _dateTimeProvider = new SystemDateTimeProvider();

        var customCategories = await FetchCategoriesFromAPI();
        InitializeCategories(customCategories);

        var dataFromCloud = await FetchExpensesFromAPI();
        InitializeSpendingItems(dataFromCloud);

        UIManager.Initialize();

        _exportService = new CsvExportService(CategoryLibrary);
        ChangeCurrentMonthData(_dateTimeProvider.Now.Month);
    }

    void InitializeSpendingItems(PagedResponseDTO<ExpenseDTO> initialData)
    {
        SpendingItems = new SpendingRepository();
        for (int i = 0; i < initialData.Items.Count; i++)
        {
            var newItem = new SpendingItem(initialData.Items[i]);
            SpendingItems.Add(newItem, _dateTimeProvider.NowUTC.Month);
        }
    }

    void InitializeCategories(List<CategoryDTO> initialData)
    {
        CategoryLibrary.Categories = new List<SpendCategory>();
        for (int i = 0; i < initialData.Count; i++)
        {
            var newCategory = new SpendCategory(initialData[i]);
            CategoryLibrary.Categories.Add(newCategory);
        }
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

    public async Task<SpendingItem> AddSpendingItem(SpendingItem item)
    {
        var dto = new ExpenseDTO(item);

        var result = await APIManager.PostExpenseAsync(dto);
        if(result == null)
        {
            Debug.LogError("Error while creating item");
            return null;
        }

        var newItem = new SpendingItem(result);

        SpendingItems.Add(newItem, currMonthShowing);
        OnDirty?.Invoke();
        return newItem;
    }

    public async Task<SpendCategory> AddCategory(string newName)
    {
        var newCatDTO = new CategoryDTO();
        newCatDTO.Name = newName;

        var result = await APIManager.PostCategoryAsync(newCatDTO);
        if(result == null)
        {
            Debug.LogError("Error while creating category with name: " + newName);
        }

        var newLocalCat = new SpendCategory(newCatDTO);

        CategoryLibrary.Categories.Add(newLocalCat);
        OnDirty?.Invoke();

        return newLocalCat;
    }

    public async Task<bool> RemoveSpendingItem(SpendingItem item)
    {
        var result = await APIManager.DeleteExpenseAsync(item.Id);
        if(result == false)
        {
            Debug.LogError("Error while deleting spending Item with id: " + item.Id);
        }

        var removedFromLocal = SpendingItems.Remove(item, currMonthShowing);
        if (!removedFromLocal)
        {
            Debug.LogWarning("No items found for the month of the item to remove");
        }

        OnDirty?.Invoke();
        return true;
    }

    public async Task<SpendingItem> ModifyExistentSpendingItem(SpendingItem item)
    {
        var updateRequest = new UpdateExpenseDTO(item);
        var postResult = await APIManager.PutExpenseAsync(item.Id, updateRequest);

        if(postResult == null)
        {
            Debug.LogError("Failed to post item with id: " + item.Id);
            return null;
        }

        var newItem = new SpendingItem(postResult);
        SpendingItems.RemoveById(item.Id, item.UTCDateTime.Month);
        SpendingItems.Add(newItem, item.UTCDateTime.Month);

        OnDirty?.Invoke();
        return newItem;
    }

    public decimal GetTotalSpending(int month, int categoryID)
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

    #region Networking
    [ContextMenu("Send test expense to API")]
    public async void SendTestExpenseToAPI()
    {
        await APIManager.CreateTestExpenseAsync();
    }

    [ContextMenu("Fetch expenses from API")]
    public async Task<PagedResponseDTO<ExpenseDTO>> FetchExpensesFromAPI()
    {
        var localTime = _dateTimeProvider.Now;
        var utcTime = localTime.ToUniversalTime();

        var filter = new ExpenseFilterDTO()
        {
            Month = utcTime.Month,
            Year = utcTime.Year,
            CategoryID = currCategoryIDSelected != -1? currCategoryIDSelected : null
        };

        return await APIManager.GetExpensesAsync(filter);
    }

    [ContextMenu("Fetch expenses from API filter category 1")]
    public async void FetchExpensesFromAPIFilterCategory1()
    {
        await APIManager.GetExpensesAsync(new ExpenseFilterDTO() { CategoryID = 1});
    }

    [ContextMenu("Fetch expenses from API filter category 2")]
    public async void FetchExpensesFromAPIFilterCategory2()
    {
        await APIManager.GetExpensesAsync(new ExpenseFilterDTO() { CategoryID = 2});
    }

    [ContextMenu("Fetch expenses from API with sorting value")]
    public async void FetchExpensesFromAPIWithSortingValue()
    {
        await APIManager.GetExpensesAsync(sortingRequest: new ExpenseSortingDTO() {
            SortDirection = SortDirection.Asc,
            SortBy = SortBy.Value
        });
    }

    [ContextMenu("Send test Category to API")]
    public async void SendTestCategoryToAPI()
    {
        await APIManager.CreateTestCategoryAsync();
    }

    [ContextMenu("Fetch Categories from API")]
    public async Task<List<CategoryDTO>> FetchCategoriesFromAPI()
    {
        return await APIManager.GetCategoriesAsync();
    }

    [ContextMenu("Get general Summary from API")]
    public async void GetGeneralSumaryFromAPI()
    {
        await APIManager.GetExpensesSummaryAsync();
    }

    #endregion

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
    DateTime NowUTC { get; }
}

public class SystemDateTimeProvider : ICurrentDateTimeProvider
{
    public DateTime Now => DateTime.Now;
    public DateTime NowUTC => DateTime.UtcNow;
}

public class TestDateTimeProvider : ICurrentDateTimeProvider
{
    public DateTime Now { get; set; } = new DateTime(2026, 02, 16);
    public DateTime NowUTC { get; set; } = new DateTime(2026, 02, 16);
}