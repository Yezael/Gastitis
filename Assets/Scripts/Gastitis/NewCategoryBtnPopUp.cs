using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NewCategoryBtnPopUp : MonoBehaviour
{
	public SpendCategoryLibrary CategoryLibrary;
	public TMP_InputField CategoryNameInput;
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
	}

	public IEnumerator GetNewCategoryInfo(NewCategoryResult result)
	{
		gameObject.SetActive(true);

		CategoryNameInput.text = "";

		while (!confirmed && !cancelled)
		{
			var isReady = !string.IsNullOrEmpty(CategoryNameInput.text);
			ConfirmButton.interactable = isReady;
			yield return null;
		}

		if (confirmed)
		{
			result.IsCancelled = false;
			result.NewCategoryName = CategoryNameInput.text;
		}
		else
		{
			result.IsCancelled = true;
			result.NewCategoryName = null;
		}

		confirmed = false;
		cancelled = false;

		gameObject.SetActive(false);
	}
}
