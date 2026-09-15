using FiapGames.Platform.Api.Application.Dtos;
using FiapGames.Shared.Kernel.Results;

namespace FiapGames.Platform.Api.Application.Abstractions;

public interface IDeploymentService
{
    Task<Result<RestartResponse>> RestartAsync(string deploymentName, CancellationToken cancellationToken = default);

    Task<Result<ScaleResponse>> StopAsync(string deploymentName, CancellationToken cancellationToken = default);

    Task<Result<ScaleResponse>> StartAsync(string deploymentName, CancellationToken cancellationToken = default);
}
