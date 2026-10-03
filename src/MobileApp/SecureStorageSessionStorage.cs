using Registry.PresentationKit.Auth;

namespace Registry.MobileApp;

/// <summary>Keeps the session in the platform keychain/keystore between app starts.</summary>
internal sealed class SecureStorageSessionStorage : ISessionStorage
{
	public async ValueTask<string?> GetAsync(string key) => await SecureStorage.Default.GetAsync(key);

	public async ValueTask SetAsync(string key, string? value)
	{
		if (value is null)
		{
			SecureStorage.Default.Remove(key);
		}
		else
		{
			await SecureStorage.Default.SetAsync(key, value);
		}
	}
}
