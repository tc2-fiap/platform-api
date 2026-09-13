using FiapGames.Platform.Api.Application.Abstractions;
using FiapGames.Platform.Api.Application.Dtos;
using Microsoft.Extensions.Logging;

namespace FiapGames.Platform.Api.Application.Services;

public sealed class PodService : IPodService
{
    private readonly IPodReader _reader;
    private readonly ILogger<PodService> _logger;

    public PodService(IPodReader reader, ILogger<PodService> logger)
    {
        _reader = reader;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PodResponse>> GetPodsAsync(CancellationToken cancellationToken = default)
    {
        var pods = await _reader.ListPodsAsync(cancellationToken);

        _logger.LogInformation("Listed {Count} pods", pods.Count);

        return pods;
    }
}
