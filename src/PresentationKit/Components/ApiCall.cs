using Refit;
using Registry.PresentationKit.Auth;

namespace Registry.PresentationKit.Components;

/// <summary>Runs an API call from a screen and turns failures into a message for the user.</summary>
public static class ApiCall
{
    /// <returns>Null when the call succeeded, otherwise the message to show.</returns>
    public static async Task<string?> RunAsync(Func<Task> call, string? notFound = null)
    {
        try
        {
            await call();
            return null;
        }
        catch (Exception e) when (e is ApiExceptionBase or HttpRequestException)
        {
            var problem = ApiProblem.From(e as ApiExceptionBase);
            return problem switch
            {
                { IsNetworkError: true } => "Brak połączenia z serwerem. Spróbuj ponownie.",
                { Status: 404 } when notFound is not null => notFound,
                { Status: 403 } when problem.Detail is null => "Nie masz uprawnień do tej operacji.",
                _ => problem.Detail ?? problem.Errors.Values.SelectMany(v => v).FirstOrDefault() ?? "Coś poszło nie tak. Spróbuj ponownie.",
            };
        }
    }
}
