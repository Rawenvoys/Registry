using Microsoft.JSInterop;
using Registry.PresentationKit.Auth;

namespace Registry.WebApp;

/// <summary>Keeps the session in the browser's localStorage, so a page reload does not sign the user out.</summary>
internal sealed class LocalStorageSessionStorage(IJSRuntime js) : ISessionStorage
{
    public ValueTask<string?> GetAsync(string key) => js.InvokeAsync<string?>("localStorage.getItem", key);

    public ValueTask SetAsync(string key, string? value) => value is null
        ? js.InvokeVoidAsync("localStorage.removeItem", key)
        : js.InvokeVoidAsync("localStorage.setItem", key, value);
}
