using System.Text.Json;
using Refit;

namespace Registry.PresentationKit.Auth;

/// <summary>The parts of an API problem response (RFC 7807) the screens show to the user.</summary>
public sealed record ApiProblem(int Status, string? Title, string? Detail, IReadOnlyDictionary<string, string[]> Errors)
{
    /// <summary>Status 0 means the request never got an answer, e.g. no connection.</summary>
    public bool IsNetworkError => Status == 0;

    public static ApiProblem From(ApiExceptionBase? exception) => exception is ApiException api
        ? Parse((int)api.StatusCode, api.Content)
        : new ApiProblem(0, null, null, new Dictionary<string, string[]>());

    public static ApiProblem Parse(int status, string? content)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(content))
        {
            return new ApiProblem(status, null, null, errors);
        }

        try
        {
            using var json = JsonDocument.Parse(content);
            var root = json.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return new ApiProblem(status, null, null, errors);
            }

            if (root.TryGetProperty("errors", out var errorsElement) && errorsElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var error in errorsElement.EnumerateObject())
                {
                    errors[error.Name] = error.Value.ValueKind == JsonValueKind.Array
                        ? error.Value.EnumerateArray().Select(e => e.GetString() ?? string.Empty).ToArray()
                        : [error.Value.ToString()];
                }
            }

            return new ApiProblem(status, GetString(root, "title"), GetString(root, "detail"), errors);
        }
        catch (JsonException)
        {
            return new ApiProblem(status, null, null, errors);
        }
    }

    private static string? GetString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
