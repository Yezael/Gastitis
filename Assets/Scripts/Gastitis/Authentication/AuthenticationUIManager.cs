using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AuthenticationUIManager : MonoBehaviour
{
    [SerializeField] GameObject _mainObj;
    [SerializeField] Button _logInBtn;
    [SerializeField] AuthenticationManager _authenticationManager;

    public bool _WantsToLogIn = false;

    public void Init()
    {
        _WantsToLogIn = false;
        _logInBtn.onClick.AddListener(() => _WantsToLogIn = true);
    }

    public IEnumerator OpenLogInFlow(LogInTryResult results)
    {
        _mainObj.SetActive(true);
        while (!_WantsToLogIn)
        {
            yield return null;
        }

        _authenticationManager.BeginLogin();

        _WantsToLogIn = false;
        _mainObj.SetActive(false);
    }
}
