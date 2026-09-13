using FiapGames.Platform.Api.Application.Abstractions;
using FiapGames.Platform.Api.Application.Dtos;
using k8s;
using k8s.Models;

namespace FiapGames.Platform.Api.Infrastructure.Kubernetes;

public sealed class KubernetesPodReader : IPodReader
{
    private readonly IKubernetes _client;
    private readonly string _namespace;

    public KubernetesPodReader(IKubernetes client, string @namespace)
    {
        _client = client;
        _namespace = @namespace;
    }

    public async Task<IReadOnlyList<PodResponse>> ListPodsAsync(CancellationToken cancellationToken = default)
    {
        var pods = await _client.CoreV1.ListNamespacedPodAsync(_namespace, cancellationToken: cancellationToken);
        return pods.Items.Select(ToResponse).ToList();
    }

    private static PodResponse ToResponse(V1Pod pod)
    {
        var containerStatuses = pod.Status?.ContainerStatuses;
        var application = pod.Metadata.Labels is not null && pod.Metadata.Labels.TryGetValue("app", out var appLabel)
            ? appLabel
            : "unknown";

        return new PodResponse(
            Name: pod.Metadata.Name,
            Application: application,
            Namespace: pod.Metadata.NamespaceProperty,
            Node: pod.Spec?.NodeName,
            Phase: pod.Status?.Phase ?? "Unknown",
            ReadyContainers: containerStatuses?.Count(c => c.Ready) ?? 0,
            TotalContainers: containerStatuses?.Count ?? 0,
            RestartCount: containerStatuses?.Sum(c => c.RestartCount) ?? 0,
            StartTimeUtc: pod.Status?.StartTime);
    }
}
