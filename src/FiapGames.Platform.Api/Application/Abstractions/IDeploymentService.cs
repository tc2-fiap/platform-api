using FiapGames.Shared.Kernel.Results;

namespace FiapGames.Platform.Api.Application.Abstractions;

public interface IDeploymentService
{
    Task<Result> RestartAsync(string deploymentName, CancellationToken cancellationToken = default);
}
