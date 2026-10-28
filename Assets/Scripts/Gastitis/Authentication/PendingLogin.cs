using System;
using System.Security.Cryptography;
using System.Text;

public sealed class PendingLogin
{
	public string AuthorizationUrl { get; }
	public string RedirectUri { get; }
	/// <summary>
	/// To identify this current atempt, if the coming state doesn't match this one, then we are handling two different attemps
	/// and it should fail.
	/// </summary>
	public string State { get; }
	public string CodeVerifier { get; }

	private PendingLogin(
		string authorizationUrl,
		string redirectUri,
		string state,
		string codeVerifier)
	{
		AuthorizationUrl = authorizationUrl;
		RedirectUri = redirectUri;
		State = state;
		CodeVerifier = codeVerifier;
	}

	public static PendingLogin Create(
		string authorizationEndpoint,
		string clientId,
		string redirectUri)
	{
		string state = CreateRandomValue();
		string codeVerifier = CreateRandomValue();
		string codeChallenge;

		using (var sha256 = SHA256.Create())
		{
			byte[] verifierBytes = Encoding.ASCII.GetBytes(codeVerifier);
			codeChallenge = ToBase64Url(sha256.ComputeHash(verifierBytes));
		}

		string authorizationUrl =
			authorizationEndpoint +
			"?response_type=code" +
			"&client_id=" + Uri.EscapeDataString(clientId) +
			"&redirect_uri=" + Uri.EscapeDataString(redirectUri) +
			"&scope=" + Uri.EscapeDataString("openid gastitis.access") +
			"&state=" + Uri.EscapeDataString(state) +
			"&code_challenge=" + Uri.EscapeDataString(codeChallenge) +
			"&code_challenge_method=S256";

		return new PendingLogin(
			authorizationUrl,
			redirectUri,
			state,
			codeVerifier);
	}

	private static string CreateRandomValue()
	{
		var bytes = new byte[32];

		using (var random = RandomNumberGenerator.Create())
		{
			random.GetBytes(bytes);
		}

		return ToBase64Url(bytes);
	}

	private static string ToBase64Url(byte[] bytes)
	{
		return Convert.ToBase64String(bytes)
			.TrimEnd('=')
			.Replace('+', '-')
			.Replace('/', '_');
	}
}