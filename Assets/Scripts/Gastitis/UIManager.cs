using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField]
    private SpendsManager _spendsManager;


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

    private string _currSearchFilter = string.Empty;

    public void Initialize()
    {
        BuildCategoryDropdownOptions();
        SetupUiListeners();

        NewSpendItemPopUp.gameObject.SetActive(false);

        _spendsManager.OnDirty += OnSpendsManagerDirty;

        RefreshUIBaseOnCurrentData();
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
    }

    void BuildCategoryDropdownOptions()
    {
        CategorySelectorDropdown.options.Clear();

        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        var defaultOpt = new TMP_Dropdown.OptionData("NONE");
        options.Add(defaultOpt);

        for (int i = 0; i < CategoryLibrary.Categories.Count; i++)
        {
            var newOpt = new TMP_Dropdown.OptionData(CategoryLibrary.Categories[i].CategoryName);
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

    IEnumerator GetNewSpending()
    {
        NewSpendingResult result = new NewSpendingResult();
        yield return NewSpendItemPopUp.GetNewSpending(result);
        if (result.IsCancelled) yield break;

        _spendsManager.AddSpendingItem(result.NewSpending);
    }

    IEnumerator EditSpendingFlow(SpendItemUI itemUI)
    {
        NewSpendingResult result = new NewSpendingResult();
        yield return NewSpendItemPopUp.ModifyOrCreateSpending(itemUI.SpendingItemData, result);
        if (result.IsCancelled)
        {
            yield break;
        }

        _spendsManager.ModifyExistentSpendingItem(result.NewSpending);

        // Update UI item to reflect new data
        itemUI.SetData(result.NewSpending);

        RefreshTotalSpendings();
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

        _spendsManager.AddCategory(result.NewCategoryName, CategoryLibrary.Categories);
        BuildCategoryDropdownOptions();
    }

    void HandleCategoryRemovalEffects()
    {
        // If categories changed, ensure items with removed categories map to default.
        if (CategoryLibrary.Categories.Count == 0) return;

        var defaultID = CategoryLibrary.Categories[0].CategoryID;
        foreach (var itemLists in _spendsManager.SpendingItems.All.Values)
        {
            for (var i = 0; i < itemLists.Count; i++)
            {
                var item = itemLists[i];
                bool found = false;
                for (var c = 0; c < CategoryLibrary.Categories.Count; c++)
                {
                    if (CategoryLibrary.Categories[c].CategoryID == item.CategoryID)
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
            _spendsManager.currCategoryFilter = string.Empty;
        }
        else
        {
            _spendsManager.currCategoryFilter = CategoryLibrary.Categories[newCategory].CategoryID;
        }

        RefreshUIBaseOnCurrentData();
    }

    void OnSearchInputChanged(string newValue)
    {
        _currSearchFilter = (newValue ?? string.Empty).Trim();
        RefreshVisibleElements();
        RefreshTotalSpendings();
    }

    void RefreshUIBaseOnCurrentData()
    {
        RefreshTotalSpendings();
        RefreshVisibleElements();
        MonthSelectorDropdown.SetValueWithoutNotify(_spendsManager.currMonthShowing - 1);
    }

    void RefreshVisibleElements()
    {
        for (int i = 0; i < spendItemUIs.Count; i++)
        {
            Destroy(spendItemUIs[i].gameObject);
        }

        spendItemUIs.Clear();

        var newData = _spendsManager.SpendingItems.GetByMonth(_spendsManager.currMonthShowing);
        if (newData == null) return;

        for (int i = 0; i < newData.Count; i++)
        {
            var item = newData[i];

            // Category filter
            if (!string.IsNullOrEmpty(_spendsManager.currCategoryFilter) && item.CategoryID != _spendsManager.currCategoryFilter)
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
        var newUI = Instantiate(SpendingButtonUIProtitype, SpendingsListContentParent);
        newUI.SetData(item);
        newUI.gameObject.SetActive(true);
        newUI.OnWantsToRemoveSpending += OnSpendingUIRequestsRemove;
        newUI.OnWantsToEditSpending += OnSpendingUIRequestsEdit;
        spendItemUIs.Add(newUI);
    }

    void OnSpendingUIRequestsRemove(SpendItemUI itemUI)
    {
        var item = itemUI.SpendingItemData;
        var removed = _spendsManager.RemoveSpendingItem(item);
        if (!removed) return;

        spendItemUIs.Remove(itemUI);
        itemUI.OnWantsToRemoveSpending -= OnSpendingUIRequestsRemove;
        itemUI.OnWantsToEditSpending -= OnSpendingUIRequestsEdit;
        Destroy(itemUI.gameObject);

        RefreshTotalSpendings();
    }

    void OnSpendingUIRequestsEdit(SpendItemUI itemUI)
    {
        StartCoroutine(EditSpendingFlow(itemUI));
    }

    void RefreshTotalSpendings()
    {
        float total = 0f;
        var monthData = _spendsManager.SpendingItems.GetByMonth(_spendsManager.currMonthShowing);

        for (int i = 0; i < monthData.Count; i++)
        {
            var item = monthData[i];

            // Apply category filter
            if (!string.IsNullOrEmpty(_spendsManager.currCategoryFilter) 
                && item.CategoryID != _spendsManager.currCategoryFilter) continue;

            // Apply search filter
            if (!string.IsNullOrEmpty(_currSearchFilter))
            {
                var desc = item.Description ?? string.Empty;
                if (desc.IndexOf(_currSearchFilter, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }
            }

            total += item.SpendAmount;
        }

        TotalSpendText.text = NewSpendItemPopUp.ToFormattedNumber(total);
    }

    void OnExportClicked()
    {
        var monthName = MonthSelectorDropdown.options[_spendsManager.currMonthShowing - 1].text;
        _spendsManager.ExportCurrentMonthSpendings(_spendsManager.currMonthShowing, monthName);
    }

    void OnSpendsManagerDirty()
    {
        RefreshUIBaseOnCurrentData();
    }
}