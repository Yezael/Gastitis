using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine.Networking;

public class KeycloakTokenClient
{
	private readonly string _tokenEndpoint;
	private readonly string _clientId;

	public KeycloakTokenClient(string tokenEndpoint, string clientId)
	{
		_tokenEndpoint = tokenEndpoint;
		_clientId = clientId;
	}

	public Task<TokenResponse> ExchangeCodeAsync(string code, PendingLogin pendingLogin)
	{
		var fields = new Dictionary<string, string>
		{
			["grant_type"] = "authorization_code",
			["client_id"] = _clientId,
			["code"] = code,
			["redirect_uri"] = pendingLogin.RedirectUri,
			["code_verifier"] = pendingLogin.CodeVerifier
		};

		return SendTokenRequestAsync(fields);
	}

	public Task<TokenResponse> RefreshAsync(string refreshToken)
	{
		if (string.IsNullOrWhiteSpace(refreshToken))
		{
			throw new ArgumentException("A refresh token is required.", nameof(refreshToken));
		}

		var fields = new Dictionary<string, string>
		{
			["grant_type"] = "refresh_token",
			["client_id"] = _clientId,
			["refresh_token"] = refreshToken
		};

		return SendTokenRequestAsync(fields);
	}

	private async Task<TokenResponse> SendTokenRequestAsync(Dictionary<string, string> fields)
	{
		using (var request = UnityWebRequest.Post(_tokenEndpoint, fields))
		{
			request.timeout = 30;
			request.redirectLimit = 0;

			await SendRequestAsync(request);

			if (request.result != UnityWebRequest.Result.Success || request.responseCode != 200)
			{
				throw new TokenRequestException(request.responseCode, IsInvalidGrantResponse(request));
			}

			TokenResponse tokens;

			try
			{
				tokens = JsonConvert.DeserializeObject<TokenResponse>(request.downloadHandler.text);
			}
			catch (JsonException)
			{
				throw new InvalidOperationException("Token endpoint returned an invalid JSON response.");
			}

			if (tokens == null)
			{
				throw new InvalidOperationException("Token endpoint returned an invalid token response.");
			}
			if (string.IsNullOrWhiteSpace(tokens.AccessToken))
			{
				throw new InvalidOperationException("Token endpoint returned an invalid token response.");
			}
			if (!string.Equals(tokens.TokenType, "Bearer", StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidOperationException("Token endpoint returned an invalid token response.");
			}
			if (tokens.ExpiresIn <= 0)
			{
				throw new InvalidOperationException("Token endpoint returned an invalid token response.");
			}
			return tokens;
		}
	}

	private static Task SendRequestAsync(UnityWebRequest request)
	{
		var completion = new TaskCompletionSource<bool>();
		var operation = request.SendWebRequest();

		operation.completed += _ => completion.TrySetResult(true);

		if (operation.isDone)
		{
			completion.TrySetResult(true);
		}

		return completion.Task;
	}

	private static bool IsInvalidGrantResponse(UnityWebRequest request)
	{
		if (request.responseCode != 400) return false;

		try
		{
			var response = JsonConvert.DeserializeObject<TokenErrorResponse>(request.downloadHandler.text);
			return string.Equals(response?.Error, "invalid_grant", StringComparison.Ordinal);
		}
		catch (JsonException)
		{
			return false;
		}
	}
}


public class TokenRequestException : InvalidOperationException
{
	public long HttpStatusCode { get; }
	public bool IsInvalidGrant { get; }

	public TokenRequestException(long httpStatusCode, bool isInvalidGrant)
		: base($"Token request failed. HTTP {httpStatusCode}.")
	{
		HttpStatusCode = httpStatusCode;
		IsInvalidGrant = isInvalidGrant;
	}
}

public class TokenErrorResponse
{
	[JsonProperty("error")]
	public string Error { get; set; }
}