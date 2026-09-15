using FiapGames.Platform.Api.Application.Abstractions;
using FiapGames.Platform.Api.Application.Dtos;
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

    public async Task<Result<RestartResponse>> RestartAsync(string deploymentName, CancellationToken cancellationToken = default)
    {
        if (!AllowedDeployments.Contains(deploymentName))
        {
            _logger.LogWarning("Restart rejected for unknown deployment {DeploymentName}", deploymentName);
            return Result.Failure<RestartResponse>(Error.Validation($"'{deploymentName}' is not a known deployment."));
        }

        await _restarter.RestartAsync(deploymentName, cancellationToken);

        _logger.LogInformation("Restarted deployment {DeploymentName}", deploymentName);

        return Result.Success(new RestartResponse(deploymentName, DateTime.UtcNow));
    }

    // platform-api is deliberately excluded here, on top of the shared
    // AllowedDeployments check below — it's what serves this very endpoint,
    // so stopping it would brick the admin API with no way to Start it back
    // up except kubectl by hand. Restart (above) still allows it: a rolling
    // restart never drops replicas to 0, so it can't self-lock the same way.
    public Task<Result<ScaleResponse>> StopAsync(string deploymentName, CancellationToken cancellationToken = default)
    {
        if (deploymentName == "platform-api")
        {
            _logger.LogWarning("Stop rejected for platform-api — would brick the admin API with no recovery path but kubectl");
            return Task.FromResult(Result.Failure<ScaleResponse>(Error.Validation("platform-api cannot be stopped from this page.")));
        }

        return ScaleAsync(deploymentName, replicas: 0, cancellationToken);
    }

    public Task<Result<ScaleResponse>> StartAsync(string deploymentName, CancellationToken cancellationToken = default) =>
        ScaleAsync(deploymentName, replicas: 1, cancellationToken);

    private async Task<Result<ScaleResponse>> ScaleAsync(string deploymentName, int replicas, CancellationToken cancellationToken)
    {
        if (!AllowedDeployments.Contains(deploymentName))
        {
            _logger.LogWarning("Scale to {Replicas} rejected for unknown deployment {DeploymentName}", replicas, deploymentName);
            return Result.Failure<ScaleResponse>(Error.Validation($"'{deploymentName}' is not a known deployment."));
        }

        await _restarter.ScaleAsync(deploymentName, replicas, cancellationToken);

        _logger.LogInformation("Scaled deployment {DeploymentName} to {Replicas} replicas", deploymentName, replicas);

        return Result.Success(new ScaleResponse(deploymentName, replicas, DateTime.UtcNow));
    }
}
