namespace Registry.PresentationKit.Auth;

/// <summary>
/// Keeps the signed-in session between app starts: localStorage on the web, SecureStorage on mobile.
/// </summary>
public interface ISessionStorage
{
    ValueTask<string?> GetAsync(string key);

    ValueTask SetAsync(string key, string? value);
}

/// <summary>Default when the host registers nothing: the session ends with the app.</summary>
internal sealed class InMemorySessionStorage : ISessionStorage
{
    private readonly Dictionary<string, string> _values = [];

    public ValueTask<string?> GetAsync(string key) => ValueTask.FromResult(_values.GetValueOrDefault(key));

    public ValueTask SetAsync(string key, string? value)
    {
        if (value is null)
        {
            _values.Remove(key);
        }
        else
        {
            _values[key] = value;
        }

        return ValueTask.CompletedTask;
    }
}
