namespace Registry.PresentationKit.Components;

/// <summary>Lets screens tell the side menu that groups or catalogs changed without navigating away.</summary>
public sealed class NavigationState
{
    public event Action? Changed;

    public void Refresh() => Changed?.Invoke();
}
