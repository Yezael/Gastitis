using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class NewSpendItemPopUp : MonoBehaviour
{
    public SpendCategoryLibrary CategoryLibrary;
    public TMP_Dropdown CategorySelector;
    public TMP_InputField DescriptionInput;
    public Button StartEdittingAmountBtn;
    public TMP_InputField AmountInput;
    public TMP_Text AmountDisplayer;
    public Button ConfirmButton;
    public Button CancelButton;

    public bool confirmed;
    public bool cancelled;

    private void Awake()
    {
        confirmed = false;
        cancelled = false;
        ConfirmButton.onClick.AddListener(OnConfirmClicked);
        CancelButton.onClick.AddListener(OnCancelClicked);

        AmountInput.onEndEdit.AddListener(FormatInput);

        StartEdittingAmountBtn.onClick.AddListener(OnStartEditingNumber);
    }

    private void OnConfirmClicked()
    {
        confirmed = true;
    }

    private void OnCancelClicked()
    {
        cancelled = true;
    }

    private void OnStartEditingNumber()
    {
        StartEdittingAmountBtn.gameObject.SetActive(false);
        AmountInput.Select();
        AmountDisplayer.gameObject.SetActive(false);

        var number = ToPlainNumber(AmountInput.text);
        AmountInput.SetTextWithoutNotify(number.ToString());
    }

    private void FormatInput(string value)
    {
        AmountDisplayer.gameObject.SetActive(true);

        // Extract digits only
        decimal number = ToPlainNumber(value);

        if (number == -1)
        {
            AmountDisplayer.text = "";
            return;
        }

        string formatted = ToFormattedNumber(number);
        AmountInput.text = formatted;

        StartEdittingAmountBtn.gameObject.SetActive(true);
        AmountDisplayer.text = formatted;
    }

    public static decimal ToPlainNumber(string text)
    {
        string digitsOnly = Regex.Replace(text, @"[^\d]", "");
        if (!decimal.TryParse(digitsOnly, out decimal number)) return 0;

        if (string.IsNullOrEmpty(digitsOnly))
        {
            return -1;
        }

        return number;
    }

    public static string ToFormattedNumber(decimal number)
    {
        return number.ToString("#,0", CultureInfo.InvariantCulture) + "$";
    }

    public IEnumerator GetNewSpending(NewSpendingResult result)
    {
        yield return ModifyOrCreateSpending(null, result);
    }

    public IEnumerator ModifyOrCreateSpending(SpendingItem existing, NewSpendingResult result)
    {
        gameObject.SetActive(true);

        CategorySelector.ClearOptions();

        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        for (int i = 0; i < CategoryLibrary.Categories.Count; i++)
        {
            options.Add(new TMP_Dropdown.OptionData(CategoryLibrary.Categories[i].Name));
        }
        CategorySelector.AddOptions(options);

        if (existing == null)
        {
            DescriptionInput.text = "";
            AmountInput.text = "";
            CategorySelector.value = 0;
            AmountDisplayer.SetText("0$");
        }
        else
        {
            // Prefill fields with existing data
            DescriptionInput.text = existing.Description;
            AmountInput.text = ToFormattedNumber(existing.SpendAmount);
            AmountDisplayer.SetText(ToFormattedNumber(existing.SpendAmount));

            // Select category index that matches existing.CategoryID
            int selectedIdx = 0;
            for (int i = 0; i < CategoryLibrary.Categories.Count; i++)
            {
                if (CategoryLibrary.Categories[i].Id == existing.CategoryID)
                {
                    selectedIdx = i;
                    break;
                }
            }

            CategorySelector.value = selectedIdx;
        }

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
            var newValue = ToPlainNumber(AmountInput.text);

            var categoryIdxSelected = CategorySelector.value;
            var categoryPicked = SpendsManager.Instance.CategoryLibrary.Categories[categoryIdxSelected];

            var newSpending = new SpendingItem()
            {
                Description = DescriptionInput.text,
                SpendAmount = newValue,
                CategoryID = categoryPicked.Id,
                UTCDateTime = existing == null ? System.DateTime.UtcNow : existing.UTCDateTime,
                Id = existing == null ? -1 : existing.Id
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

public class NewCategoryResult
{
    public bool IsCancelled;
    public string NewCategoryName;
}
