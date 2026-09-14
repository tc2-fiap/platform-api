using FiapGames.Platform.Api.Application.Abstractions;
using FiapGames.Shared.Infrastructure.Extensions;

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

        // Triggers a rolling restart of one of the seven allowlisted
        // Deployments (DeploymentService validates the name; the
        // ServiceAccount's RBAC resourceNames restriction is the real
        // enforcement — see k8s/templates/rbac.yaml). No new image is
        // pulled; only useful when the currently-loaded image is already
        // up to date. Fires the restart and returns — the rollout itself
        // is asynchronous.
        group.MapPost("/admin/services/{name}/restart", async (string name, IDeploymentService service, CancellationToken cancellationToken) =>
        {
            var result = await service.RestartAsync(name, cancellationToken);
            return result.ToHttpResult();
        }).RequireAuthorization(p => p.RequireRole("Admin"));

        return endpoints;
    }
}
