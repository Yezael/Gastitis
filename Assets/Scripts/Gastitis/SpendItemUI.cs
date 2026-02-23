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
    public Button RemoveSpendingBtn;

    public Action<SpendItemUI> OnWantsToRemoveSpending;

    private void Awake()
    {
        RemoveSpendingBtn.onClick.AddListener(WantsToRemoveSpending);
	}

	public void SetData(SpendingItem item)
    {
        SpendingItemData = item;
        ItemDescriptionText.text = item.Description;
        ItemAmountText.text = item.SpendAmount.ToString("C");
        ItemCategoryNameText.text = SpendsManager.Instance.CategoryLibrary.Categories[item.Category].CategoryName;
    }

    private void WantsToRemoveSpending()
    {
        OnWantsToRemoveSpending?.Invoke(this);
	}
}
