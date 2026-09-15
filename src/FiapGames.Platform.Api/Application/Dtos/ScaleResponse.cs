namespace FiapGames.Platform.Api.Application.Dtos;

public sealed record ScaleResponse(string Service, int Replicas, DateTime UpdatedAtUtc);
