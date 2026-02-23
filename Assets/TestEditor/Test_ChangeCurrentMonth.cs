using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class Test_ChangeCurrentMonth
{
	SpendsManager _spendManagerPrefab;
	SpendsManager _spendManagerInstance;

	public int[] MonthsToTest = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 24 };

	[SetUp]
	public void Setup()
	{
		_spendManagerPrefab = AssetDatabase.LoadAssetAtPath<SpendsManager>("Assets/Prefabs/SpendManager.prefab");
	}


	// A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
	// `yield return null;` to skip a frame.
	[UnityTest]
	public IEnumerator ChangeCurrentMonth_SetPreviousMonth()
	{
		_spendManagerInstance = GameObject.Instantiate(_spendManagerPrefab);

		yield return null;

		var dateTimeProvider = new TestDateTimeProvider();
		_spendManagerInstance._dateTimeProvider = dateTimeProvider;
		_spendManagerInstance.Initialize();

		_spendManagerInstance.ChangeCurrentMonthData(2);

		Assert.AreEqual(2, _spendManagerInstance.currMonthShowing);

		GameObject.DestroyImmediate(_spendManagerInstance.gameObject);
	}

	[TearDown]
	public void TearDown()
	{
		if (_spendManagerInstance != null)
		{
			GameObject.DestroyImmediate(_spendManagerInstance.gameObject);
		}
		_spendManagerPrefab = null;
	}
}


