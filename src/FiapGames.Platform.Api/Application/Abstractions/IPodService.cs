using FiapGames.Platform.Api.Application.Dtos;

namespace FiapGames.Platform.Api.Application.Abstractions;

public interface IPodService
{
    Task<IReadOnlyList<PodResponse>> GetPodsAsync(CancellationToken cancellationToken = default);
}
