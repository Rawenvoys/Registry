namespace Registry.PresentationKit.Auth;

public static class ReturnUrl
{
    /// <summary>Keeps only paths inside the app, so a crafted login link cannot send the user elsewhere.</summary>
    public static string Sanitize(string? returnUrl) =>
        string.IsNullOrEmpty(returnUrl)
        || !returnUrl.StartsWith('/')
        || returnUrl.StartsWith("//", StringComparison.Ordinal)
        || returnUrl.StartsWith("/\\", StringComparison.Ordinal)
            ? "/"
            : returnUrl;
}
