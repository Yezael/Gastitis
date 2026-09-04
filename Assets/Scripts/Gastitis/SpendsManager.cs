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

    public int _currMonthShowing = 1;
    public int _currCategoryIDSelected = -1;
    public string _currSearchedWord = null;
    private int _lastFetchedExpensesFrame = -1;


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
        _currMonthShowing = _dateTimeProvider.NowUTC.Month;

        var customCategories = await FetchCategoriesFromAPI();
        InitializeCategories(customCategories);

        SpendingItems = new SpendingRepository();
        await FetchMonthSpendingsIfNeeded();

        UIManager.Initialize();

        _exportService = new CsvExportService(CategoryLibrary);
        ChangeCurrentMonthData(_dateTimeProvider.Now.Month);
    }

    public async Task FetchMonthSpendingsIfNeeded()
    {
        if (_lastFetchedExpensesFrame == Time.frameCount) return;
        _lastFetchedExpensesFrame = Time.frameCount;

        var monthItems = await FetchExpensesFromAPI();
        SpendingItems.AddOrModify(monthItems.Items, _currMonthShowing);
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
        _currMonthShowing = newMonth;
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

        SpendingItems.AddOrModify(newItem, _currMonthShowing);
        OnDirty?.Invoke();
        return newItem;
    }

    public async Task<decimal> GetTotalAmount()
    {
        var filter = new ExpenseFilterDTO()
        {
            CategoryID = _currCategoryIDSelected != -1? _currCategoryIDSelected : null,
            Month = _currMonthShowing,
            Year = _dateTimeProvider.NowUTC.Year,
        };
        var summary = await APIManager.GetExpensesSummaryAsync(filter);
        if(summary == null)
        {
            Debug.LogWarning("Failed to retrieve summary from API");
            return -1;
        }

        return summary.TotalAmount;
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

        var newLocalCat = new SpendCategory(result);

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

        var removedFromLocal = SpendingItems.Remove(item, _currMonthShowing);
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
        SpendingItems.AddOrModify(newItem, item.UTCDateTime.Month);

        OnDirty?.Invoke();
        return newItem;
    }

    public void ExportCurrentMonthSpendings(int month, string monthName)
    {
        if (month <= 0) return;

        var monthData = SpendingItems.GetByMonth(month);
        if (monthData == null) return;

        _exportService.Export(monthData, monthName);
    }

    #region API calls
    public async Task<PagedResponseDTO<ExpenseDTO>> FetchExpensesFromAPI()
    {
        var localTime = _dateTimeProvider.Now;
        var utcTime = localTime.ToUniversalTime();

        var filter = new ExpenseFilterDTO()
        {
            Month = _currMonthShowing,
            Year = utcTime.Year,
            CategoryID = _currCategoryIDSelected != -1? _currCategoryIDSelected : null,
            Keyword = _currSearchedWord,
        };

        return await APIManager.GetExpensesAsync(filter);
    }

    public async Task<List<CategoryDTO>> FetchCategoriesFromAPI()
    {
        return await APIManager.GetCategoriesAsync();
    }

    [ContextMenu("Get category Summary from API")]
    public async void GetCategorySummary()
    {
        await APIManager.GetCategorySummaryAsync();
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