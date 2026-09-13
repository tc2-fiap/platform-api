using FiapGames.Platform.Api.Application.Abstractions;
using FiapGames.Platform.Api.Application.Dtos;
using FiapGames.Platform.Api.Application.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace FiapGames.Platform.Tests;

public class PodServiceTests
{
    private readonly IPodReader _reader = Substitute.For<IPodReader>();
    private readonly PodService _sut;

    public PodServiceTests()
    {
        var logger = Substitute.For<ILogger<PodService>>();
        _sut = new PodService(_reader, logger);
    }

    [Fact]
    public async Task GetPodsAsync_ReturnsWhatTheReaderReturns()
    {
        var pods = new List<PodResponse>
        {
            new("catalog-api-abc123", "catalog-api", "fiap-games", "kind-worker", "Running", 1, 1, 0, DateTime.UtcNow)
        };
        _reader.ListPodsAsync(Arg.Any<CancellationToken>()).Returns(pods);

        var result = await _sut.GetPodsAsync();

        Assert.Single(result);
        Assert.Equal("catalog-api", result[0].Application);
    }

    [Fact]
    public async Task GetPodsAsync_WhenNoPods_ReturnsEmptyList()
    {
        _reader.ListPodsAsync(Arg.Any<CancellationToken>()).Returns(new List<PodResponse>());

        var result = await _sut.GetPodsAsync();

        Assert.Empty(result);
    }
}
