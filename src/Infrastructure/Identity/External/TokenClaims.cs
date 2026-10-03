namespace Registry.Infrastructure.Identity.External;

internal static class TokenClaims
{
    public static object? Get(IDictionary<string, object> claims, string name) 
        => claims.TryGetValue(name, out var value) ? value : null;

    public static string? GetString(IDictionary<string, object> claims, string name) => Get(claims, name) as string;

    /// <summary>Providers send boolean claims either as JSON booleans or as "true" strings.</summary>
    public static bool IsTrue(object? value) => value switch
    {
        bool b => b,
        string s => bool.TryParse(s, out var parsed) && parsed,
        _ => false,
    };
}
