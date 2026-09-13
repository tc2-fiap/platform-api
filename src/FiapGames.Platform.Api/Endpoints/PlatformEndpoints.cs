using FiapGames.Platform.Api.Application.Abstractions;

namespace FiapGames.Platform.Api.Endpoints;

public static class PlatformEndpoints
{
    public static IEndpointRouteBuilder MapPlatformEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/platform").WithTags("Platform");

        group.MapGet("/admin/pods", async (IPodService service, CancellationToken cancellationToken) =>
        {
            var pods = await service.GetPodsAsync(cancellationToken);
            return Results.Ok(pods);
        }).RequireAuthorization(p => p.RequireRole("Admin"));

        return endpoints;
    }
}
