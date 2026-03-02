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
        ConfirmButton.onClick.AddListener(() =>
        {
            confirmed = true;
        });

        CancelButton.onClick.AddListener(() =>
        {
            cancelled = true;
        });

       

        AmountInput.onEndEdit.AddListener(FormatInput);

        StartEdittingAmountBtn.onClick.AddListener(OnStartEditingNumber);

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
        float number = ToPlainNumber(value);

		if (number == -1)
		{
			AmountDisplayer.text = "";
			return;
		}

        string formatted = ToFormattedNumber((float)number);
		AmountInput.text = formatted;


        StartEdittingAmountBtn.gameObject.SetActive(true);
        AmountDisplayer.text = formatted;
	}

    public static float ToPlainNumber(string text)
    {
		string digitsOnly = Regex.Replace(text, @"[^\d]", "");
		if (!decimal.TryParse(digitsOnly, out decimal number)) return 0;

		if (string.IsNullOrEmpty(digitsOnly))
		{
            return -1;
		}

		return (float) number;
    }

    public static string ToFormattedNumber(float number)
    {
        return number.ToString("#,0", CultureInfo.InvariantCulture) + "$";
	}

	public IEnumerator GetNewSpending(NewSpendingResult result)
    {
		gameObject.SetActive(true);

        CategorySelector.ClearOptions();

		List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
		for (int i = 0; i < CategoryLibrary.Categories.Count; i++)
		{
			options.Add(new TMP_Dropdown.OptionData(CategoryLibrary.Categories[i].CategoryName));
		}
		CategorySelector.AddOptions(options);


        DescriptionInput.text = "";
        AmountInput.text = "";
        CategorySelector.value = 0;
        AmountDisplayer.SetText("0$");

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

            var newSpending = new SpendingItem()
            {
                Description = DescriptionInput.text,
                SpendAmount = float.Parse(newValue.ToString()),
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

public class NewCategoryResult
{
	public bool IsCancelled;
	public string NewCategoryName;
}
