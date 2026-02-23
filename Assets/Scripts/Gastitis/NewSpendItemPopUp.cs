using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NewSpendItemPopUp : MonoBehaviour
{
    public SpendCategoryLibrary CategoryLibrary;
	public TMP_Dropdown CategorySelector;
    public TMP_InputField DescriptionInput;
    public TMP_InputField AmountInput;
    public Button ConfirmButton;
    public Button CancelButton;

    public bool confirmed;
    public bool cancelled;

    private void Awake()
    {
        confirmed = false;
        cancelled = false;
        ConfirmButton.onClick.AddListener(() =>
        {
            confirmed = true;
        });

        CancelButton.onClick.AddListener(() =>
        {
            cancelled = true;
        });

        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        for (int i = 0; i < CategoryLibrary.Categories.Count; i++)
        {
            options.Add(new TMP_Dropdown.OptionData(CategoryLibrary.Categories[i].CategoryName));
		}
        CategorySelector.AddOptions(options);
	}

    public IEnumerator GetNewSpending(NewSpendingResult result)
    {
        gameObject.SetActive(true);

        DescriptionInput.text = "";
        AmountInput.text = "";
        CategorySelector.value = 0;

		while (!confirmed && !cancelled)
        {
            var isReady = !string.IsNullOrEmpty(DescriptionInput.text);
            isReady &= !string.IsNullOrEmpty(AmountInput.text);
            isReady &= CategorySelector.value != -1;

            ConfirmButton.interactable = isReady;

            yield return null;
        }

        if (confirmed)
        {
            var newSpending = new SpendingItem()
            {
                Description = DescriptionInput.text,
                SpendAmount = float.Parse(AmountInput.text),
                Category = CategorySelector.value,
                DateTime = System.DateTime.Now
            };
            result.IsCancelled = false;
            result.NewSpending = newSpending;
        }
        else
        {
            result.IsCancelled = true;
            result.NewSpending = null;
        }

        confirmed = false;
        cancelled = false;

		gameObject.SetActive(false);
    }
}


public class NewSpendingResult
{
    public bool IsCancelled;
    public SpendingItem NewSpending;
}
