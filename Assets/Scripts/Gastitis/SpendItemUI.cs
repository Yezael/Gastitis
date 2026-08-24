using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpendItemUI : MonoBehaviour
{
    public SpendingItem SpendingItemData;
    public TMP_Text ItemDescriptionText;
    public TMP_Text ItemAmountText;
    public TMP_Text ItemCategoryNameText;
    public TMP_Text ItemDateText;
    public Button RemoveSpendingBtn;
    public Button EditSpendingBtn;

    public Action<SpendItemUI> OnWantsToRemoveSpending;
    public Action<SpendItemUI> OnWantsToEditSpending;

    private void Awake()
    {
        RemoveSpendingBtn.onClick.AddListener(WantsToRemoveSpending);
        EditSpendingBtn.onClick.AddListener(WantsToEditSpending);
    }

    public void SetData(SpendingItem item)
    {
        SpendingItemData = item;
        ItemDescriptionText.text = item.Description;
        ItemAmountText.text = NewSpendItemPopUp.ToFormattedNumber(item.SpendAmount);

        var catgoryName = SpendsManager.Instance.CategoryLibrary.GetCategoryByID(item.CategoryID).Name;
        ItemCategoryNameText.text = catgoryName;
        ItemDateText.text = item.UTCDateTime.ToString("dd/MM/yyyy");
    }

    private void WantsToRemoveSpending()
    {
        OnWantsToRemoveSpending?.Invoke(this);
    }

    private void WantsToEditSpending()
    {
        OnWantsToEditSpending?.Invoke(this);
    }
}
