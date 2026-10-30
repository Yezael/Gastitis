using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using UnityEngine;

public class AuthenticationManager : MonoBehaviour, IAccessTokenProvider
{
	private const string AuthorizationEndpoint = "http://localhost:8080/realms/gastitis/protocol/openid-connect/auth";
	private const string ClientId = "gastitis-unity";
	private const string AndroidRedirectUri = "com.lasgemasdelgary.gastitis:/oauth2redirect";
	private PendingLogin _pendingLogin;
	private const string TokenEndpoint = "http://localhost:8080/realms/gastitis/protocol/openid-connect/token";
	private readonly KeycloakTokenClient _tokenClient = new KeycloakTokenClient(TokenEndpoint, ClientId);
	private bool _isExchangingCode;
	private TokenResponse _tokens;
	private double _accessTokenUsableUntil;
	private Task<string> _refreshTask;
	public long SessionVersion { get; private set; }

	public event Action OnSessionWasInvalidated;

	private void InvalidateSession()
	{
		SessionVersion++;
		_tokens = null;
		_accessTokenUsableUntil = 0;
		OnSessionWasInvalidated?.Invoke();
	}


	public void BeginLogin()
	{
		if (_isExchangingCode || _refreshTask != null) return;

		if (Application.platform != RuntimePlatform.Android)
		{
			Debug.LogWarning(
				"Login currently requires the Android build.");
			return;
		}

		var newLoginSession = PendingLogin.Create(AuthorizationEndpoint, ClientId, AndroidRedirectUri);

		InvalidateSession();
		_pendingLogin = newLoginSession;

		Application.OpenURL(_pendingLogin.AuthorizationUrl);
	}

	public async Task HandleAuthorizationCallbackAsync(Uri callbackUri)
	{
		if (_pendingLogin == null)
		{
			Debug.LogWarning("Callback rejected: no pending login.");
			return;
		}

		if (!TryReadQuery(callbackUri.Query, out var parameters))
		{
			Debug.LogWarning("Callback rejected: invalid query parameters.");
			return;
		}

		if (!parameters.TryGetValue("state", out var state))
		{
			Debug.LogWarning("Callback rejected: No incomming state found.");
			return;
		}
		if (!string.Equals(state, _pendingLogin.State, StringComparison.Ordinal))
		{
			Debug.LogWarning("Callback rejected: state mismatch.");
			return;
		}

		if (!parameters.TryGetValue("iss", out var issuer))
		{
			Debug.LogWarning("Callback rejected: No incomming issuer found.");
			return;
		}
		if(!string.Equals(issuer,"http://localhost:8080/realms/gastitis",StringComparison.Ordinal))
		{
			Debug.LogWarning("Callback rejected: unexpected issuer.");
			return;
		}

		bool hasCode = parameters.TryGetValue("code", out var authenticationExchangeCode);
		bool hasError = parameters.TryGetValue("error", out var error);

		if (hasCode == hasError || (hasCode && string.IsNullOrWhiteSpace(authenticationExchangeCode)) || (hasError && string.IsNullOrWhiteSpace(error)))
		{
			Debug.LogWarning("Callback rejected: invalid authorization response.");
			return;
		}

		var acceptedLogin = _pendingLogin;
		_pendingLogin = null;

		if (hasError)
		{
			Debug.LogWarning("Authorization failed or was declined.");
			return;
		}

		_isExchangingCode = true;

		try
		{
			double requestStartedAt = Time.realtimeSinceStartupAsDouble;

			var tokens = await _tokenClient.ExchangeCodeAsync(authenticationExchangeCode, acceptedLogin);

			if (this == null) return;

			StoreTokens(tokens, requestStartedAt);

			Debug.Log($"Token exchange succeeded. Access token lifetime: {tokens.ExpiresIn} seconds.");
		}
		catch (InvalidOperationException exception)
		{
			if (this == null) return;

			Debug.LogWarning(exception.Message);
		}
		finally
		{
			_isExchangingCode = false;
		}
	}

	private static bool TryReadQuery(string query, out Dictionary<string, string> parameters)
	{
		parameters = new Dictionary<string, string>(StringComparer.Ordinal);

		if (string.IsNullOrEmpty(query)) return false;

		string queryText = query[0] == '?' ? query.Substring(1) : query;

		foreach (string part in queryText.Split('&'))
		{
			if (string.IsNullOrEmpty(part)) continue;

			int separatorIndex = part.IndexOf('=');

			if (separatorIndex <= 0) return false;

			string name = WebUtility.UrlDecode(part.Substring(0, separatorIndex));
			string value = WebUtility.UrlDecode(part.Substring(separatorIndex + 1));

			if (string.IsNullOrEmpty(name) || parameters.ContainsKey(name)) return false;

			parameters.Add(name, value);
		}

		return true;
	}

	private void StoreTokens(TokenResponse tokens, double requestStartedAt)
	{
		_tokens = tokens;
		double safetyMarginSeconds = Math.Min(30d, tokens.ExpiresIn * 0.1d);

		_accessTokenUsableUntil = requestStartedAt + tokens.ExpiresIn - safetyMarginSeconds;
	}

	/// <summary>
	/// do we currently have a locally usable access token?
	/// </summary>
	public bool IsAuthenticated()
	{
		if (_tokens == null) return false;
		if (string.IsNullOrWhiteSpace(_tokens.AccessToken)) return false;
		if (Time.realtimeSinceStartupAsDouble >= _accessTokenUsableUntil) return false;

		return true;
	}

	/// <summary>
	/// do we have a usable access token or credentials with which to attempt refresh?
	/// </summary>
	public bool HasSession()
	{
		if (_tokens == null) return false;

		if (IsAuthenticated()) return true;
		if (!string.IsNullOrWhiteSpace(_tokens.RefreshToken)) return true;

		return false;
	}

	public bool TryGetAccessToken(out string accessToken)
	{
		accessToken = null;

		if (!IsAuthenticated()) return false;

		accessToken = _tokens.AccessToken;
		return true;
	}

	public async Task<string> GetAccessTokenAsync()
	{
		if (this == null) throw new OperationCanceledException("The authentication manager was destroyed");

		if (TryGetAccessToken(out var accessToken)) return accessToken;

		if (_pendingLogin != null || _isExchangingCode) throw new InvalidOperationException("Interactive login has not completed");

		var refreshTask = _refreshTask;

		if (refreshTask == null)
		{
			if (_tokens == null || string.IsNullOrWhiteSpace(_tokens.RefreshToken))
			{
				throw new InvalidOperationException("Login is required to obtain an access token");
			}
			refreshTask = RefreshAccessTokenAsync();
			_refreshTask = refreshTask;
		}

		try
		{
			return await refreshTask;
		}
		finally
		{
			if(ReferenceEquals(_refreshTask, refreshTask))
			{
				_refreshTask = null;
			}
		}
	}

	private async Task<string> RefreshAccessTokenAsync()
	{
		var previousTokens = _tokens;
		double requestStartedAt = Time.realtimeSinceStartupAsDouble;

		TokenResponse refreshedTokens;

		try
		{
			refreshedTokens = await _tokenClient.RefreshAsync(previousTokens.RefreshToken);
		}
		catch (TokenRequestException exception) when (exception.IsInvalidGrant)
		{
			if (this != null && ReferenceEquals(_tokens, previousTokens))
			{
				InvalidateSession();
			}
			throw;
		}

		if (this == null) throw new OperationCanceledException("The authentication manager was destroyed");

		if (!ReferenceEquals(_tokens, previousTokens)) throw new InvalidOperationException("The session changed while refreshing.");

		if (string.IsNullOrEmpty(refreshedTokens.RefreshToken))
		{
			refreshedTokens.RefreshToken = previousTokens.RefreshToken;
		}

		StoreTokens(refreshedTokens, requestStartedAt);

		if (!TryGetAccessToken(out var accessToken)) throw new InvalidOperationException("The refreshed access token is no longer usable.");

		Debug.Log("Access token refreshed.");

		return accessToken;
	}
}