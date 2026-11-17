using System;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Collections;

public class SpendsManager : MonoBehaviour
{
    [SerializeField]
    private AuthenticationManager _authenticationManager;

    public AuthenticationManager AuthenticationManager => _authenticationManager;
	public long CurrSessionDataVersion => _currSessionDataVersion;

	public static SpendsManager Instance;

    public GastitisAPIManager APIManager { get; private set; }
    public SpendCategoryLibrary CategoryLibrary;
    public UIManager UIManager;

    public ICurrentDateTimeProvider _dateTimeProvider;

    public SpendingRepository SpendingItems = new SpendingRepository();

    public int _currMonthShowing = 1;
    public int _currCategoryIDSelected = -1;
    public string _currSearchedWord = null;
    private int _lastFetchedExpensesFrame = -1;
	private bool _initializationComplete;
	private Coroutine _sessionRecoveryRoutine;

	private long _currSessionDataVersion;

    private IExportService _exportService;

    public Action OnDirty;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            APIManager = new GastitisAPIManager(_authenticationManager);
            return;
        }

        if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private async void Start()
    {
        await Initialize();
    }

    public async Task Initialize()
    {
		_initializationComplete = false;
		AuthenticationManager.OnSessionWasInvalidated += HandleSessionInvalidated;

		UIManager.Initialize();
        if (!AuthenticationManager.IsAuthenticated())
        {
            var result = new LogInTryResult();
            StartCoroutine(UIManager.StartLogInSequece(result));
            while(!result.IsCancelled && !result.Succeded)
            {
                await Task.Delay(10);
            }

            if (!result.Succeded) return;
        }

        _dateTimeProvider = new SystemDateTimeProvider();
        _exportService = new CsvExportService(CategoryLibrary);
        await LoadSessionDataAsync();
        _initializationComplete = true;
    }


    void HandleSessionInvalidated()
    {
        ClearSessionData();

        if (!_initializationComplete) return;
        if (_sessionRecoveryRoutine != null) return;

        _sessionRecoveryRoutine = StartCoroutine(RecoverSessionAsync());

	}

    IEnumerator RecoverSessionAsync()
    {
        try
        {
			var result = new LogInTryResult();
			yield return UIManager.StartLogInSequece(result);
			if (result.IsCancelled || !result.Succeded)
			{
				Debug.LogWarning("User cancelled login or failed to log in");
				yield break;
			}

			var loadTask = LoadSessionDataAsync();
			while (!loadTask.IsCompleted)
			{
				yield return null;
			}
			if (loadTask.IsFaulted || loadTask.IsCanceled)
			{
				Debug.LogError("Failed to load session data after login");
				yield break;
			}
		}
		finally
		{
			_sessionRecoveryRoutine = null;
		}
	}


	private void ThrowOnSessionMissmatch(long incommingVersion)
	{
		if (!IsSameSession(incommingVersion))
		{
			throw new OperationCanceledException("The session data changed while the operation was running.");
		}
	}

	public bool IsSameSession(long incommingVersion)
	{
        if (this == null) return false;
        if(incommingVersion != _currSessionDataVersion) return false;
        return true;
	}

	private void ClearSessionData()
    {
        _currSessionDataVersion++;
		CategoryLibrary.Categories.Clear();
		SpendingItems.Clear();

		_lastFetchedExpensesFrame = -1;
		_currCategoryIDSelected = -1;
		_currSearchedWord = null;
        UIManager.ClearSessionUI();
	}

    public async Task LoadSessionDataAsync()
    {
		ClearSessionData();

        var sessionversion = _currSessionDataVersion;

		_currMonthShowing = _dateTimeProvider.NowUTC.Month;

		var customCategories = await FetchCategoriesFromAPI();
        ThrowOnSessionMissmatch(sessionversion);
		InitializeCategories(customCategories);

		await FetchMonthSpendingsIfNeeded();
		ThrowOnSessionMissmatch(sessionversion);

		Debug.Log("Correctly fetched spending items and categories");
		UIManager.StartValidSessionUI();
	}

	public async Task FetchMonthSpendingsIfNeeded()
	{
		if (_lastFetchedExpensesFrame == Time.frameCount) return;

		_lastFetchedExpensesFrame = Time.frameCount;

		long sessionVersion = _currSessionDataVersion;
		int requestedMonth = _currMonthShowing;

		var monthItems = await FetchExpensesFromAPI();
		ThrowOnSessionMissmatch(sessionVersion);

		SpendingItems.SetMonthSpendings(monthItems.Items, requestedMonth);
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

    public async Task AddSpendingItem(SpendingItem item)
    {
        var dto = new ExpenseDTO(item);

        var currentSessionVersion = _currSessionDataVersion;
		var result = await APIManager.PostExpenseAsync(dto);
        ThrowOnSessionMissmatch(currentSessionVersion);
        if(result == null)
        {
            Debug.LogError("Error while creating item");
            return;
        }

        OnDirty?.Invoke();
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
        var newCatDTO = new CategoryDTO() { Name = newName };

        var currentSessionVersion = _currSessionDataVersion;
		var result = await APIManager.PostCategoryAsync(newCatDTO);
		ThrowOnSessionMissmatch(currentSessionVersion);

		if (result == null)
        {
            Debug.LogError("Error while creating category with name: " + newName);
            return null;
		}

        var newLocalCat = new SpendCategory(result);

        CategoryLibrary.Categories.Add(newLocalCat);
        OnDirty?.Invoke();

        return newLocalCat;
    }

    public async Task<bool> RemoveSpendingItem(SpendingItem item)
    {
        var currentSessionVersion = _currSessionDataVersion;
        var originalMonth = item.UTCDateTime.Month;

		var result = await APIManager.DeleteExpenseAsync(item.Id);
		ThrowOnSessionMissmatch(currentSessionVersion);

		if (result == false)
        {
            Debug.LogError("Error while deleting spending Item with id: " + item.Id);
            return false;
        }

        OnDirty?.Invoke();
        return true;
    }

    public async Task<bool> RemoveCategoryItemAsync(SpendCategory category)
    {
        var currentSessionVersion = _currSessionDataVersion;

		var result = await APIManager.DeleteCategoryAsync(category.Id);
		ThrowOnSessionMissmatch(currentSessionVersion);

		if (result == false)
        {
            Debug.LogError("Error while deleting category with id: " + category.Id);
            return false;
        }

        var removedFromLocal = CategoryLibrary.Categories.Remove(category);
        if (!removedFromLocal)
        {
            Debug.LogWarning("Category deleted from Database but not found in local library");
        }

        OnDirty?.Invoke();
        return true;
    }

    public async Task ModifyExistentSpendingItem(SpendingItem item)
    {
        var updateRequest = new UpdateExpenseDTO(item);
        var currentSessionVersion = _currSessionDataVersion;
        var originalMonth = item.UTCDateTime.Month;

		var postResult = await APIManager.PutExpenseAsync(item.Id, updateRequest);
		ThrowOnSessionMissmatch(currentSessionVersion);

		if (postResult == null)
        {
            Debug.LogError("Failed to post item with id: " + item.Id);
            return;
        }

        OnDirty?.Invoke();
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