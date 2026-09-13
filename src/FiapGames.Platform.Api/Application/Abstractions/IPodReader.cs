using FiapGames.Platform.Api.Application.Dtos;

namespace FiapGames.Platform.Api.Application.Abstractions;

public interface IPodReader
{
    Task<IReadOnlyList<PodResponse>> ListPodsAsync(CancellationToken cancellationToken = default);
}
