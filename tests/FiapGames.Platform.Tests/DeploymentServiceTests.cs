using FiapGames.Platform.Api.Application.Abstractions;
using FiapGames.Platform.Api.Application.Services;
using FiapGames.Shared.Kernel.Results;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace FiapGames.Platform.Tests;

public class DeploymentServiceTests
{
    private readonly IDeploymentRestarter _restarter = Substitute.For<IDeploymentRestarter>();
    private readonly DeploymentService _sut;

    public DeploymentServiceTests()
    {
        var logger = Substitute.For<ILogger<DeploymentService>>();
        _sut = new DeploymentService(_restarter, logger);
    }

    [Theory]
    [InlineData("users-api")]
    [InlineData("catalog-api")]
    [InlineData("orders-api")]
    [InlineData("payments-api")]
    [InlineData("notifications-api")]
    [InlineData("platform-api")]
    [InlineData("frontend")]
    public async Task RestartAsync_WithAllowlistedName_RestartsAndSucceeds(string name)
    {
        var result = await _sut.RestartAsync(name);

        Assert.True(result.IsSuccess);
        Assert.Equal(name, result.Value.Service);
        await _restarter.Received(1).RestartAsync(name, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("postgres")]
    [InlineData("rabbitmq")]
    [InlineData("../users-api")]
    [InlineData("")]
    public async Task RestartAsync_WithUnknownName_FailsWithoutCallingRestarter(string name)
    {
        var result = await _sut.RestartAsync(name);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        await _restarter.DidNotReceiveWithAnyArgs().RestartAsync(default!, default);
    }

    [Theory]
    [InlineData("users-api")]
    [InlineData("catalog-api")]
    [InlineData("orders-api")]
    [InlineData("payments-api")]
    [InlineData("notifications-api")]
    [InlineData("frontend")]
    public async Task StopAsync_WithAllowlistedName_ScalesToZeroAndSucceeds(string name)
    {
        var result = await _sut.StopAsync(name);

        Assert.True(result.IsSuccess);
        Assert.Equal(name, result.Value.Service);
        Assert.Equal(0, result.Value.Replicas);
        await _restarter.Received(1).ScaleAsync(name, 0, Arg.Any<CancellationToken>());
    }

    // platform-api is otherwise a normal allowlisted name (Restart allows
    // it) — Stop specifically has to reject it, since it's what serves this
    // endpoint and stopping it would brick the admin API with no recovery
    // path but kubectl.
    [Fact]
    public async Task StopAsync_WithPlatformApi_FailsWithoutCallingRestarter()
    {
        var result = await _sut.StopAsync("platform-api");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        await _restarter.DidNotReceiveWithAnyArgs().ScaleAsync(default!, default, default);
    }

    [Theory]
    [InlineData("postgres")]
    [InlineData("rabbitmq")]
    [InlineData("")]
    public async Task StopAsync_WithUnknownName_FailsWithoutCallingRestarter(string name)
    {
        var result = await _sut.StopAsync(name);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        await _restarter.DidNotReceiveWithAnyArgs().ScaleAsync(default!, default, default);
    }

    [Theory]
    [InlineData("users-api")]
    [InlineData("platform-api")]
    [InlineData("frontend")]
    public async Task StartAsync_WithAllowlistedName_ScalesToOneAndSucceeds(string name)
    {
        var result = await _sut.StartAsync(name);

        Assert.True(result.IsSuccess);
        Assert.Equal(name, result.Value.Service);
        Assert.Equal(1, result.Value.Replicas);
        await _restarter.Received(1).ScaleAsync(name, 1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_WithUnknownName_FailsWithoutCallingRestarter()
    {
        var result = await _sut.StartAsync("postgres");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        await _restarter.DidNotReceiveWithAnyArgs().ScaleAsync(default!, default, default);
    }
}
