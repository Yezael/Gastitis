using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CategoryItemUI : MonoBehaviour
{
    public SpendCategory Data;
    public TMP_Text CategoryName;
    public Button RemoveSpendingBtn;

    public Func<CategoryItemUI, Task> OnWantsToRemoveCategory;

    private void Awake()
    {
        RemoveSpendingBtn.onClick.AddListener(WantsToRemoveCategory);
	}

	public void SetData(SpendCategory item)
    {
        Data = item;
        CategoryName.text = item.Name;
        RemoveSpendingBtn.interactable = true;
	}

    private void WantsToRemoveCategory()
    {
        OnWantsToRemoveCategory?.Invoke(this);
	}

    public void DisableDeleteBtn()
    {
        RemoveSpendingBtn.interactable = false;
    }
}
