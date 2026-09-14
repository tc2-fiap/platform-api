using FiapGames.Platform.Api.Application.Abstractions;
using k8s;
using k8s.Models;

namespace FiapGames.Platform.Api.Infrastructure.Kubernetes;

public sealed class KubernetesDeploymentRestarter : IDeploymentRestarter
{
    private readonly IKubernetes _client;
    private readonly string _namespace;

    public KubernetesDeploymentRestarter(IKubernetes client, string @namespace)
    {
        _client = client;
        _namespace = @namespace;
    }

    // Same mechanism `kubectl rollout restart` itself uses: patching this
    // annotation changes the pod template, which the Deployment controller
    // treats as a new rollout. No new image is pulled — imagePullPolicy:
    // IfNotPresent means this only helps when the currently-loaded image is
    // already up to date.
    public async Task RestartAsync(string deploymentName, CancellationToken cancellationToken = default)
    {
        var patch = new V1Patch(
            new
            {
                spec = new
                {
                    template = new
                    {
                        metadata = new
                        {
                            annotations = new Dictionary<string, string>
                            {
                                ["kubectl.kubernetes.io/restartedAt"] = DateTime.UtcNow.ToString("o")
                            }
                        }
                    }
                }
            },
            V1Patch.PatchType.MergePatch);

        await _client.AppsV1.PatchNamespacedDeploymentAsync(patch, deploymentName, _namespace, cancellationToken: cancellationToken);
    }
}
