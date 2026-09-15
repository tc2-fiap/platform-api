namespace FiapGames.Platform.Api.Application.Abstractions;

public interface IDeploymentRestarter
{
    Task RestartAsync(string deploymentName, CancellationToken cancellationToken = default);

    Task ScaleAsync(string deploymentName, int replicas, CancellationToken cancellationToken = default);
}
