namespace Registry.Domain;

/// <summary>A business rule was broken. The message is safe to show to the user.</summary>
public sealed class DomainException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
