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
}
