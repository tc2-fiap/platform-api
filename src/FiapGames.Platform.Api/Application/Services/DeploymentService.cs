using FiapGames.Platform.Api.Application.Abstractions;
using FiapGames.Shared.Kernel.Results;
using Microsoft.Extensions.Logging;

namespace FiapGames.Platform.Api.Application.Services;

public sealed class DeploymentService : IDeploymentService
{
    // Defense in depth alongside the RBAC `resourceNames` restriction on
    // platform-api's ServiceAccount (k8s/templates/rbac.yaml) — that's what
    // actually stops a request from ever reaching postgres/rabbitmq, but a
    // clean 400 for a typo'd/unknown name is better than letting an
    // arbitrary string reach the Kubernetes API and surface as a raw
    // Forbidden/NotFound from there.
    private static readonly HashSet<string> AllowedDeployments = new(StringComparer.Ordinal)
    {
        "users-api", "catalog-api", "orders-api", "payments-api", "notifications-api", "platform-api", "frontend"
    };

    private readonly IDeploymentRestarter _restarter;
    private readonly ILogger<DeploymentService> _logger;

    public DeploymentService(IDeploymentRestarter restarter, ILogger<DeploymentService> logger)
    {
        _restarter = restarter;
        _logger = logger;
    }

    public async Task<Result> RestartAsync(string deploymentName, CancellationToken cancellationToken = default)
    {
        if (!AllowedDeployments.Contains(deploymentName))
        {
            _logger.LogWarning("Restart rejected for unknown deployment {DeploymentName}", deploymentName);
            return Result.Failure(Error.Validation($"'{deploymentName}' is not a known deployment."));
        }

        await _restarter.RestartAsync(deploymentName, cancellationToken);

        _logger.LogInformation("Restarted deployment {DeploymentName}", deploymentName);

        return Result.Success();
    }
}
