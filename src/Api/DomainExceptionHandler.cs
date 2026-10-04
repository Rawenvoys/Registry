using Microsoft.AspNetCore.Diagnostics;
using Registry.Domain;

namespace Registry.Api;

/// <summary>Turns broken business rules into problem details the clients can show.</summary>
internal sealed class DomainExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not DomainException domain)
        {
            return false;
        }

        context.Response.StatusCode = domain.Code switch
        {
            "forbidden" => StatusCodes.Status403Forbidden,
            "already_member" or "last_owner" or "name_taken" => StatusCodes.Status409Conflict,
            "member_not_found" or "invitation_not_found" => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status400BadRequest,
        };

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = { Title = domain.Code, Detail = domain.Message, Status = context.Response.StatusCode },
        });
    }
}
