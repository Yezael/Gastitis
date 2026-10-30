
using System.Threading.Tasks;

public interface IAccessTokenProvider
{
	Task<string> GetAccessTokenAsync();
	long SessionVersion { get; }
}
