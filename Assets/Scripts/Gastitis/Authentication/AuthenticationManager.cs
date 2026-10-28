using UnityEngine;

public class AuthenticationManager : MonoBehaviour
{
	public static bool IsAuthenticated = false;

	private const string AuthorizationEndpoint =
		"http://localhost:8080/realms/gastitis/protocol/openid-connect/auth";

	private const string ClientId = "gastitis-Unity";

	private const string AndroidRedirectUri = "com.lasgemasdelgary.gastitis:/oauth2redirect";

	private PendingLogin _pendingLogin;

	public void BeginLogin()
	{
		if (Application.platform != RuntimePlatform.Android)
		{
			Debug.LogWarning(
				"Login currently requires the Android build.");
			return;
		}

		_pendingLogin = PendingLogin.Create(AuthorizationEndpoint, ClientId, AndroidRedirectUri);

		Application.OpenURL(_pendingLogin.AuthorizationUrl);
	}
}