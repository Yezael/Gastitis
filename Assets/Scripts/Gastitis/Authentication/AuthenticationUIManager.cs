using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AuthenticationUIManager : MonoBehaviour
{
    [SerializeField] GameObject _mainObj;
    [SerializeField] Button _logInBtn;
    [SerializeField] AuthenticationManager _authenticationManager;

    public bool _wantsToLogIn = false;

    public void Init()
    {
        _wantsToLogIn = false;
        _logInBtn.onClick.AddListener(() => _wantsToLogIn = true);
    }

    public IEnumerator OpenLogInFlow(LogInTryResult results)
    {
        results.Succeded = false;
        _wantsToLogIn = false;
        _mainObj.SetActive(true);

        while (!results.IsCancelled && !_authenticationManager.IsAuthenticated())
        {
            if (_wantsToLogIn)
            {
                _wantsToLogIn = false;
                _authenticationManager.BeginLogin();
            }
            yield return null;
        }

        results.Succeded = !results.IsCancelled && _authenticationManager.IsAuthenticated();

        _mainObj.SetActive(false);
    }
}
