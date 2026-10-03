using System.Security.Claims;
using Registry.Application.Catalogs;
using Registry.Application.Groups;
using Registry.Contracts.Catalogs;
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

        groups.MapPut("/{groupId:guid}", async (Guid groupId, UpdateGroupRequest request, ClaimsPrincipal user, GroupService service, CancellationToken ct) =>
            await service.UpdateAsync(user.Id(), groupId, request, ct) is { } group ? Results.Ok(group) : Results.NotFound());

        groups.MapDelete("/{groupId:guid}", async (Guid groupId, ClaimsPrincipal user, GroupService service, CancellationToken ct) =>
            await service.DeleteAsync(user.Id(), groupId, ct) ? Results.NoContent() : Results.NotFound());

        groups.MapPut("/{groupId:guid}/members/{memberId:guid}", async (Guid groupId, Guid memberId, ChangeRoleRequest request, ClaimsPrincipal user, GroupService service, CancellationToken ct) =>
            await service.ChangeRoleAsync(user.Id(), groupId, memberId, request, ct) ? Results.NoContent() : Results.NotFound());

        // Removing yourself means leaving the group.
        groups.MapDelete("/{groupId:guid}/members/{memberId:guid}", async (Guid groupId, Guid memberId, ClaimsPrincipal user, GroupService service, CancellationToken ct) =>
            await service.RemoveMemberAsync(user.Id(), groupId, memberId, ct) ? Results.NoContent() : Results.NotFound());

        groups.MapPost("/{groupId:guid}/locations", async (Guid groupId, LocationRequest request, ClaimsPrincipal user, GroupService service, CancellationToken ct) =>
            await service.AddLocationAsync(user.Id(), groupId, request, ct) is { } location
                ? Results.Created($"/groups/{groupId}/locations/{location.Id}", location)
                : Results.NotFound());

        groups.MapGet("/{groupId:guid}/invitations", async (Guid groupId, ClaimsPrincipal user, GroupService service, CancellationToken ct) =>
            await service.ListInvitationsAsync(user.Id(), groupId, ct) is { } invitations ? Results.Ok(invitations) : Results.NotFound());

        groups.MapPost("/{groupId:guid}/invitations", async (Guid groupId, InvitationRequest request, ClaimsPrincipal user, GroupService service, CancellationToken ct) =>
            await service.InviteAsync(user.Id(), groupId, request, ct) is { } invitation ? Results.Ok(invitation) : Results.NotFound());

        groups.MapDelete("/{groupId:guid}/invitations/{code}", async (Guid groupId, string code, ClaimsPrincipal user, GroupService service, CancellationToken ct) =>
            await service.RevokeInvitationAsync(user.Id(), groupId, code, ct) ? Results.NoContent() : Results.NotFound());

        groups.MapGet("/{groupId:guid}/catalogs", async (Guid groupId, ClaimsPrincipal user, CatalogService service, CancellationToken ct) =>
            await service.ListAsync(user.Id(), groupId, ct) is { } catalogs ? Results.Ok(catalogs) : Results.NotFound());

        groups.MapPost("/{groupId:guid}/catalogs", async (Guid groupId, CatalogRequest request, ClaimsPrincipal user, CatalogService service, CancellationToken ct) =>
            await service.CreateAsync(user.Id(), groupId, request, ct) is { } catalog
                ? Results.Created($"/groups/{groupId}/catalogs/{catalog.Id}", catalog)
                : Results.NotFound());

        groups.MapGet("/{groupId:guid}/catalogs/{catalogId:guid}", async (Guid groupId, Guid catalogId, ClaimsPrincipal user, CatalogService service, CancellationToken ct) =>
            await service.GetAsync(user.Id(), groupId, catalogId, ct) is { } catalog ? Results.Ok(catalog) : Results.NotFound());

        groups.MapPut("/{groupId:guid}/catalogs/{catalogId:guid}", async (Guid groupId, Guid catalogId, CatalogRequest request, ClaimsPrincipal user, CatalogService service, CancellationToken ct) =>
            await service.RenameAsync(user.Id(), groupId, catalogId, request, ct) is { } catalog ? Results.Ok(catalog) : Results.NotFound());

        groups.MapDelete("/{groupId:guid}/catalogs/{catalogId:guid}", async (Guid groupId, Guid catalogId, ClaimsPrincipal user, CatalogService service, CancellationToken ct) =>
            await service.DeleteAsync(user.Id(), groupId, catalogId, ct) ? Results.NoContent() : Results.NotFound());

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
