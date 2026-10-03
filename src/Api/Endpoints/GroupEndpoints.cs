using System.Security.Claims;
using Registry.Application.Groups;
using Registry.Contracts.Groups;

namespace Registry.Api.Endpoints;

public static class GroupEndpoints
{
    public static IEndpointRouteBuilder MapGroupEndpoints(this IEndpointRouteBuilder app)
    {
        var groups = app.MapGroup("/groups").WithTags("Groups").RequireAuthorization();

        groups.MapGet("/", async (ClaimsPrincipal user, GroupService service, CancellationToken ct) =>
            TypedResults.Ok(await service.ListAsync(user.Id(), ct)));

        groups.MapPost("/", async (CreateGroupRequest request, ClaimsPrincipal user, GroupService service, CancellationToken ct) =>
        {
            var group = await service.CreateAsync(user.Id(), request, ct);
            return TypedResults.Created($"/groups/{group.Id}", group);
        });

        groups.MapGet("/{groupId:guid}", async (Guid groupId, ClaimsPrincipal user, GroupService service, CancellationToken ct) =>
            await service.GetAsync(user.Id(), groupId, ct) is { } group ? Results.Ok(group) : Results.NotFound());

        groups.MapPost("/{groupId:guid}/locations", async (Guid groupId, LocationRequest request, ClaimsPrincipal user, GroupService service, CancellationToken ct) =>
            await service.AddLocationAsync(user.Id(), groupId, request, ct) is { } location
                ? Results.Created($"/groups/{groupId}/locations/{location.Id}", location)
                : Results.NotFound());

        groups.MapPost("/{groupId:guid}/invitations", async (Guid groupId, InvitationRequest request, ClaimsPrincipal user, GroupService service, CancellationToken ct) =>
            await service.InviteAsync(user.Id(), groupId, request, ct) is { } invitation ? Results.Ok(invitation) : Results.NotFound());

        app.MapPost("/invitations/{code}/accept", async (string code, ClaimsPrincipal user, GroupService service, CancellationToken ct) =>
                await service.AcceptInvitationAsync(user.Id(), user.FindFirstValue(ClaimTypes.Email), code, ct) is { } group
                    ? Results.Ok(group)
                    : Results.NotFound())
            .WithTags("Groups")
            .RequireAuthorization();

        return app;
    }

    private static Guid Id(this ClaimsPrincipal user) => Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
