using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class Test_ClickOnCreateNewSpendingItem
{
  
    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    [UnityTest]
    public IEnumerator Test_ClickOnCreateNewSpendingItemWithEnumeratorPasses()
    {
        yield return SceneManager.LoadSceneAsync("SampleScene");
        yield return null;

        var uiManager = SpendsManager.Instance.UIManager;

        Assert.IsNotNull(uiManager);

        uiManager.AddSpendingButton.onClick.Invoke();
        yield return null;

        Assert.IsTrue(uiManager.NewSpendItemPopUp.gameObject.activeSelf);
	}

    [TearDown]
    public void TearDown()
    {
        SceneManager.UnloadSceneAsync("SampleScene");
    }
}
