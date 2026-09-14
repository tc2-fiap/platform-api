namespace FiapGames.Platform.Api.Application.Abstractions;

public interface IDeploymentRestarter
{
    Task RestartAsync(string deploymentName, CancellationToken cancellationToken = default);
}
