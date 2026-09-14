using FiapGames.Platform.Api.Application.Abstractions;

namespace FiapGames.Platform.Api.Endpoints;

public static class PlatformEndpoints
{
    public static IEndpointRouteBuilder MapPlatformEndpoints(this IEndpointRouteBuilder endpoints, Func<IResult> getVersion)
    {
        var group = endpoints.MapGroup("/api/platform").WithTags("Platform");

        group.MapGet("/admin/pods", async (IPodService service, CancellationToken cancellationToken) =>
        {
            var pods = await service.GetPodsAsync(cancellationToken);
            return Results.Ok(pods);
        }).RequireAuthorization(p => p.RequireRole("Admin"));

        // Admin-dashboard-facing twin of the bare /version (see Program.cs):
        // same handler, reached via the Ingress like any other route in this
        // group instead of only via kubectl port-forward, gated to Admin.
        group.MapGet("/version", getVersion).RequireAuthorization(p => p.RequireRole("Admin"));

        return endpoints;
    }
}
