namespace FiapGames.Platform.Api.Application.Dtos;

public sealed record RestartResponse(string Service, DateTime RestartedAtUtc);
