**English** · [Português](README.pt-BR.md)

# FIAP Games — Platform API

Cluster introspection for the admin "System Health" dashboard — the sixth backend service, added specifically because listing Kubernetes pods doesn't belong to any of the five domain services (users/catalog/orders/payments/notifications). No database, no schema, no messaging — it's a stateless, authenticated proxy over the Kubernetes API.

## Run standalone

```bash
cp .env.example .env
docker compose up --build
```

`/health` and `/version` work standalone. `GET /api/platform/admin/pods` does **not** — it calls `KubernetesClientConfiguration.InClusterConfig()`, which only works when actually running inside a cluster pod (it reads the mounted ServiceAccount token). There's no meaningful local-dev story for the pods endpoint itself; test it against a real `kind` cluster.

## Run as part of the system

Deployed by the [`orchestration`](https://github.com/tc2-fiap/orchestration) Helm chart alongside the other five backend services and the frontend. Reached through the shared Ingress at `/api/platform/*`. Needs its own `ServiceAccount` + namespaced `Role` (`get`/`list`/`watch` on `pods`, in the `fiap-games` namespace only — no `ClusterRole`, since each `Pod` already carries its own scheduled node in `spec.nodeName`) — see `k8s/templates/rbac.yaml`, the only RBAC resources anywhere in this system.

## What's here

- `GET /api/platform/admin/pods` — Admin-only. Lists every pod in the `fiap-games` namespace: name, application (from the pod's own `app` label), namespace, node, phase, ready-container count, restart count, start time.
- The five existing backend services each expose their own `GET /api/<prefix>/admin/status` (build SHA + build time) — this service does **not** aggregate those; the frontend composes all five directly, plus this service's pod list, into one dashboard view. See `../documentation/spec/notes.md`.

## Test

```bash
cd tests/FiapGames.Platform.Tests && dotnet test
```

## Documentation

Full architecture, event contracts, and the project-wide decision record live in the `documentation` repo — [`github.com/tc2-fiap/documentation`](https://github.com/tc2-fiap/documentation).
