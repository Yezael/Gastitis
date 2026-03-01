using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CategoryItemUI : MonoBehaviour
{
    public SpendCategory Data;
    public TMP_Text CategoryName;
    public Button RemoveSpendingBtn;

    public Action<CategoryItemUI> OnWantsToRemoveCategory;

    private void Awake()
    {
        RemoveSpendingBtn.onClick.AddListener(WantsToRemoveCategory);
	}

	public void SetData(SpendCategory item)
    {
        Data = item;
        CategoryName.text = item.CategoryName;
    }

    private void WantsToRemoveCategory()
    {
        OnWantsToRemoveCategory?.Invoke(this);
	}
}
