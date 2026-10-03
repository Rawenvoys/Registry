namespace Registry.PresentationKit.Auth;

/// <summary>Polish messages for the ASP.NET Core Identity error codes returned by /auth/register.</summary>
public static class IdentityErrors
{
    public static string Describe(string code, string? fallback) => code switch
    {
        "DuplicateUserName" or "DuplicateEmail" => "Konto z tym adresem e-mail już istnieje.",
        "InvalidEmail" or "InvalidUserName" => "Nieprawidłowy adres e-mail.",
        "PasswordTooShort" => "Hasło jest za krótkie.",
        "PasswordRequiresDigit" => "Hasło musi zawierać cyfrę.",
        "PasswordRequiresLower" => "Hasło musi zawierać małą literę.",
        "PasswordRequiresUpper" => "Hasło musi zawierać wielką literę.",
        "PasswordRequiresNonAlphanumeric" => "Hasło musi zawierać znak specjalny.",
        "PasswordRequiresUniqueChars" => "Hasło musi zawierać więcej różnych znaków.",
        _ => fallback ?? "Nie udało się założyć konta.",
    };
}
