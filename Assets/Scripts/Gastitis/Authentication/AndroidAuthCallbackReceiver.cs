using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AndroidAuthCallbackReceiver : MonoBehaviour
{
	private void Awake()
	{
		Application.deepLinkActivated += HandleDeeplink;

		if (!string.IsNullOrEmpty(Application.absoluteURL))
		{
			HandleDeeplink(Application.absoluteURL);
		}
	}

	private void OnDestroy()
	{
		Application.deepLinkActivated -= HandleDeeplink;
	}

	private void HandleDeeplink(string url)
	{
		Debug.Log("Entered Handle deep link method");
		if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return;
		if (!string.Equals(uri.Scheme, "com.lasgemasdelgary.gastitis", StringComparison.OrdinalIgnoreCase)) return;
		if (!string.IsNullOrEmpty(uri.Authority)) return;
		if (!string.Equals(uri.AbsolutePath,"/oauth2redirect", StringComparison.Ordinal)) return;
		if (!string.IsNullOrEmpty(uri.Fragment)) return;

		Debug.Log("Android authentication callback received");

	}

}
