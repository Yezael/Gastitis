using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
	[SerializeField] private SpendsManager _spendsManager;
	[SerializeField] private Transform _spendPoolParent;

	public AuthenticationUIManager AuthenticationManagerUI;
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

    public TMP_Text TotalSpendText;
    public TMP_InputField SearchInputField;

    public List<SpendItemUI> spendItemUIs = new List<SpendItemUI>();

    private string _currSearchFilter => _spendsManager._currSearchedWord;

    private IEnumerator _currRefreshItemsRoutine = null;
    private LogInTryResult _currLoginTry;
    private long _currSummaryRequestVersion;
    private Pool<SpendItemUI> _spendItemsPool;

    public void Initialize()
    {
        AuthenticationManagerUI.Init();
        SetupUiListeners();
		_spendItemsPool = new Pool<SpendItemUI>(SpendingButtonUIProtitype, _spendPoolParent, 3);
        ClearSessionUI();
	}

    public void StartValidSessionUI()
    {
		BuildCategoryDropdownOptions();
		NewSpendItemPopUp.gameObject.SetActive(false);
		RefreshUIBaseOnCurrentData();
        SetSessionControlsInteractable(true);
	}

	private void OnDestroy()
    {
        if (_spendsManager != null)
        {
            _spendsManager.OnDirty -= OnSpendsManagerDirty;
        }
    }


    void SetupUiListeners()
    {
        AddSpendingButton.onClick.AddListener(OnAddSpendingClicked);
        AddCategoryButton.onClick.AddListener(OnAddCategoryClicked);
        RemoveCategoriesButton.onClick.AddListener(OnRemoveCategoriesClicked);
        MonthSelectorDropdown.onValueChanged.AddListener(OnMonthDropdownChanged);
        CategorySelectorDropdown.onValueChanged.AddListener(OnCategoryDropdownChanged);
        ExportMonthSpendingsButton.onClick.AddListener(OnExportClicked);

        SearchInputField.onValueChanged.AddListener(OnSearchInputChanged);
		_spendsManager.OnDirty += OnSpendsManagerDirty;
	}


	void BuildCategoryDropdownOptions()
    {
        CategorySelectorDropdown.options.Clear();

        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        var defaultOpt = new TMP_Dropdown.OptionData("NONE");
        options.Add(defaultOpt);

        for (int i = 0; i < CategoryLibrary.Categories.Count; i++)
        {
            var newOpt = new TMP_Dropdown.OptionData(CategoryLibrary.Categories[i].Name);
            options.Add(newOpt);
        }

        CategorySelectorDropdown.AddOptions(options);
        CategorySelectorDropdown.SetValueWithoutNotify(0);
    }

    void OnAddSpendingClicked()
    {
        StartCoroutine(GetNewSpending());
    }

    void OnAddCategoryClicked()
    {
        StartCoroutine(GetNewCategoryName());
    }

    void OnRemoveCategoriesClicked()
    {
        StartCoroutine(OpenCategoriesRemoval());
    }

    public IEnumerator StartLogInSequece(LogInTryResult result = null)
    {
		if (result == null)
		{
			result = new LogInTryResult();
		}

		if (_currLoginTry != null)
        {
            var currTry = _currLoginTry;
            while (currTry == _currLoginTry) yield return null;

			
			result.Succeded = currTry.Succeded;
            result.IsCancelled = currTry.IsCancelled;
            yield break;
		}

        _currLoginTry = result;

        try
        {
            yield return AuthenticationManagerUI.OpenLogInFlow(result);
        }
        finally
        {
            _currLoginTry = null;
        }
    }

    IEnumerator GetNewSpending()
    {
        NewSpendingResult result = new NewSpendingResult();
        yield return NewSpendItemPopUp.GetNewSpending(result);
        if (result.IsCancelled) yield break;

        var task = _spendsManager.AddSpendingItem(result.NewSpending);
        while (!task.IsCompleted)
        {
            yield return null;
        }
    }

    IEnumerator EditSpendingFlow(SpendingItem spendData)
    {
        var previousValue = spendData.SpendAmount;

        NewSpendingResult result = new NewSpendingResult();
        yield return NewSpendItemPopUp.ModifyOrCreateSpending(spendData, result);
        if (result.IsCancelled)
        {
            yield break;
        }

        var task = _spendsManager.ModifyExistentSpendingItem(result.NewSpending);

        while(!task.IsCompleted)
        {
            yield return null;
        }

        if(task.IsCanceled || task.IsFaulted)
        {
            yield break;
        }

        var itemUi = GetSpendingByDataId(task.Result.Id);
		// Update UI item to reflect new data
		itemUi.SetData(task.Result);

        var newValue = itemUi.SpendingItemData.SpendAmount;

        if(newValue != previousValue)
        {
            RefreshTotalSpendings();
        }
    }

    SpendItemUI GetSpendingByDataId(int dataId)
    {
        for (int i = 0; i < spendItemUIs.Count; i++)
        {
            if (spendItemUIs[i].SpendingItemData.Id != dataId) continue;
            return spendItemUIs[i];
        }

        return null;
    }

    IEnumerator OpenCategoriesRemoval()
    {
        yield return RemoveCategoriesPopUp.Execute(CategoryLibrary);
        // After removal categories may have changed: rebuild dropdown and notify manager
        BuildCategoryDropdownOptions();
        HandleCategoryRemovalEffects();
    }

    IEnumerator GetNewCategoryName()
    {
        NewCategoryResult result = new NewCategoryResult();
        yield return newCategoryPopUp.GetNewCategoryInfo(result);
        if (result.IsCancelled) yield break;

        var task = _spendsManager.AddCategory(result.NewCategoryName);
        while (!task.IsCompleted)
        {
            yield return null;
        }
        BuildCategoryDropdownOptions();
    }

    void HandleCategoryRemovalEffects()
    {
        // If categories changed, ensure items with removed categories map to default.
        if (CategoryLibrary.Categories.Count == 0) return;

        var defaultID = CategoryLibrary.Categories[0].Id;
        foreach (var itemLists in _spendsManager.SpendingItems.All.Values)
        {
            for (var i = 0; i < itemLists.Count; i++)
            {
                var item = itemLists[i];
                bool found = false;
                for (var c = 0; c < CategoryLibrary.Categories.Count; c++)
                {
                    if (CategoryLibrary.Categories[c].Id == item.CategoryID)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    item.CategoryID = defaultID;
                }
            }
        }

        _spendsManager.OnDirty?.Invoke();
    }

    void OnMonthDropdownChanged(int newMonthFromDropdown)
    {
        var newMonth = newMonthFromDropdown + 1;
        _spendsManager.ChangeCurrentMonthDataWithoutNotify(newMonth);
        RefreshUIBaseOnCurrentData();
    }

    void OnCategoryDropdownChanged(int newCategory)
    {
        newCategory--;
        if (newCategory < 0)
        {
            _spendsManager._currCategoryIDSelected = -1;
        }
        else
        {
            _spendsManager._currCategoryIDSelected = CategoryLibrary.Categories[newCategory].Id;
        }

        RefreshUIBaseOnCurrentData();
    }

    void OnSearchInputChanged(string newValue)
    {
        var searchQuery = newValue;
        if (searchQuery == string.Empty)
        {
            searchQuery = null;
        }
        else
        {
            searchQuery = searchQuery.Trim();
        }
        _spendsManager._currSearchedWord = searchQuery;
        RefreshUIBaseOnCurrentData();
    }


    void RefreshUIBaseOnCurrentData()
    {
        if (!SpendsManager.Instance.AuthenticationManager.HasSession())
        {
            StartCoroutine(StartLogInSequece());
            return;
		}

        RefreshTotalSpendings();
        StartRefreshingvisibleItems();
        MonthSelectorDropdown.SetValueWithoutNotify(_spendsManager._currMonthShowing - 1);
        if(_currSearchFilter != SearchInputField.text)
        {
            SearchInputField.SetTextWithoutNotify(_currSearchFilter);
        }
    }

    void StartRefreshingvisibleItems()
    {
        if(_currRefreshItemsRoutine != null)
        {
            StopCoroutine(_currRefreshItemsRoutine);
        }

        _currRefreshItemsRoutine = RefreshVisibleElements();
        StartCoroutine(_currRefreshItemsRoutine);
    }

    IEnumerator RefreshVisibleElements()
    {
        var refreshMonthDataTask = _spendsManager.FetchMonthSpendingsIfNeeded();

        while (!refreshMonthDataTask.IsCompleted) yield return null;

        if (refreshMonthDataTask.IsCanceled) yield break;

		if (refreshMonthDataTask.IsFaulted)
		{
			var exception = refreshMonthDataTask.Exception.GetBaseException();

			if (exception is TokenRequestException tokenException
				&& tokenException.IsInvalidGrant)
			{
				Debug.LogWarning("Session expired. Sign in again.");
			}
			else
			{
				Debug.LogWarning("Could not load expenses. Try again.");
			}

			yield break;
		}

		var newData = _spendsManager.SpendingItems.GetByMonth(_spendsManager._currMonthShowing);
        if (newData == null)
        {
			Debug.LogWarning($"Null data from month {_spendsManager._currMonthShowing}");
			yield break;
        }

        ClearAllSpendingUis();

		for (int i = 0; i < newData.Count; i++)
        {
            var item = newData[i];

            // Category filter
            if (_spendsManager._currCategoryIDSelected != -1 && item.CategoryID != _spendsManager._currCategoryIDSelected)
            {
                continue;
            }

            // Search filter (partial, case-insensitive)
            if (!string.IsNullOrEmpty(_currSearchFilter))
            {
                var desc = item.Description ?? string.Empty;
                if (desc.IndexOf(_currSearchFilter, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }
            }

            AddSpendingItemUI(item);
        }
    }

    void AddSpendingItemUI(SpendingItem item)
    {
        var newUI = _spendItemsPool.Get();
        newUI.transform.SetParent(SpendingsListContentParent);
        newUI.SetData(item);
        newUI.gameObject.SetActive(true);
        newUI.OnWantsToRemoveSpending += OnSpendingUIRequestsRemove;
        newUI.OnWantsToEditSpending += OnSpendingUIRequestsEdit;
        spendItemUIs.Add(newUI);
    }

    void OnSpendingUIRequestsRemove(SpendItemUI itemUI)
    {
        StartCoroutine(RemoveSpendingItem(itemUI));
    }

    IEnumerator RemoveSpendingItem(SpendItemUI itemUI)
    {
        var item = itemUI.SpendingItemData;
        var task = _spendsManager.RemoveSpendingItem(item);
        while (!task.IsCompleted)
        {
            yield return null;
        }

        if (task.Result == false) yield break;

        spendItemUIs.Remove(itemUI);
        ReleaseItemUI(itemUI);

        RefreshTotalSpendings();
    }

    void OnSpendingUIRequestsEdit(SpendItemUI itemUI)
    {
        StartCoroutine(EditSpendingFlow(itemUI.SpendingItemData));
    }

    async void RefreshTotalSpendings()
    {
        _currSummaryRequestVersion++;

        TotalSpendText.text = "...";
        var expenseSessionVersion = _spendsManager.CurrSessionDataVersion;
        var summarySessionVersion = _currSummaryRequestVersion;

        decimal total = -1;
        try
        {
            total = await _spendsManager.GetTotalAmount();
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (InvalidOperationException exception)
        {
            if (!SummaryRetrievalIsValid(expenseSessionVersion, summarySessionVersion)) return;
            bool requiresLogin = exception is TokenRequestException tokenException && tokenException.IsInvalidGrant;

            TotalSpendText.text = requiresLogin ? "Sign in required" : "Unavailable";
            return;
        }

		if (!SummaryRetrievalIsValid(expenseSessionVersion, summarySessionVersion)) return;

		if (total == -1)
        {
            TotalSpendText.text = "Undefined";
            return;
        }

        TotalSpendText.text = NewSpendItemPopUp.ToFormattedNumber(total);
    }

    bool SummaryRetrievalIsValid(long expenseSessionVersion, long summarySessionVersion)
    {
		if (this == null) return false;
		if (_spendsManager == null) return false;
		if (expenseSessionVersion != _spendsManager.CurrSessionDataVersion) return false;
		if (summarySessionVersion != _currSummaryRequestVersion) return false;

        return true;
	}

    public void ClearSessionUI()
    {
        _currSummaryRequestVersion++;
		if (_currRefreshItemsRoutine != null)
		{
			StopCoroutine(_currRefreshItemsRoutine);
			_currRefreshItemsRoutine = null;
		}
		ClearAllSpendingUis();

		CategorySelectorDropdown.ClearOptions();
        SearchInputField.SetTextWithoutNotify(string.Empty);
        TotalSpendText.SetText("Sign in required");

        SetSessionControlsInteractable(false);
    }

	private void SetSessionControlsInteractable(bool interactable)
    {
		AddCategoryButton.interactable = interactable;
		AddSpendingButton.interactable = interactable;
		RemoveCategoriesButton.interactable = interactable;
		ExportMonthSpendingsButton.interactable = interactable;

		MonthSelectorDropdown.interactable = interactable;
		CategorySelectorDropdown.interactable = interactable;
		SearchInputField.interactable = interactable;
	}

	void ClearAllSpendingUis()
    {
		for (int i = 0; i < spendItemUIs.Count; i++)
		{
            ReleaseItemUI(spendItemUIs[i]);
		}
		spendItemUIs.Clear();
	}

    void ReleaseItemUI(SpendItemUI item)
    {
		item.OnWantsToRemoveSpending -= OnSpendingUIRequestsRemove;
		item.OnWantsToEditSpending -= OnSpendingUIRequestsEdit;
        _spendItemsPool.Release(item);
	}

	void OnExportClicked()
    {
        var monthName = MonthSelectorDropdown.options[_spendsManager._currMonthShowing - 1].text;
        _spendsManager.ExportCurrentMonthSpendings(_spendsManager._currMonthShowing, monthName);
    }

    void OnSpendsManagerDirty()
    {
        RefreshUIBaseOnCurrentData();
    }
}