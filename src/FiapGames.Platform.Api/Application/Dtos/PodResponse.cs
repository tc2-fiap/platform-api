namespace FiapGames.Platform.Api.Application.Dtos;

public sealed record PodResponse(
    string Name,
    string Application,
    string Namespace,
    string? Node,
    string Phase,
    int ReadyContainers,
    int TotalContainers,
    int RestartCount,
    DateTime? StartTimeUtc);
