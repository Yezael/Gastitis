
public interface IAccessTokenProvider
{
	bool TryGetAccessToken(out string accessToken);
}
