[English](README.en-US.md) · **Português**

# FIAP Games — Platform API

Introspecção do cluster para o painel admin "Saúde do Sistema" — o sexto serviço de backend, adicionado especificamente porque listar pods do Kubernetes não pertence a nenhum dos cinco serviços de domínio (users/catalog/orders/payments/notifications). Sem banco de dados, sem schema, sem mensageria — é um proxy autenticado e sem estado sobre a API do Kubernetes.

## Rodando isolado

```bash
cp .env.example .env
docker compose up --build
```

`/health` e `/version` funcionam isolados. `GET /api/platform/admin/pods` **não** funciona — ele chama `KubernetesClientConfiguration.InClusterConfig()`, que só funciona rodando de verdade dentro de um pod do cluster (lê o token do ServiceAccount montado). Não há um jeito real de testar esse endpoint específico localmente; teste contra um cluster `kind` de verdade.

## Rodando como parte do sistema

Implantado pelo chart Helm do [`orchestration`](https://github.com/tc2-fiap/orchestration) junto com os outros cinco serviços de backend e o frontend. Alcançado através do Ingress compartilhado em `/api/platform/*`. Precisa de seu próprio `ServiceAccount` + `Role` namespaced (`get`/`list`/`watch` em `pods`, só no namespace `fiap-games` — sem `ClusterRole`, já que cada `Pod` já carrega o próprio node em `spec.nodeName`) — veja `k8s/templates/rbac.yaml`, os únicos recursos RBAC em todo o sistema.

## O que tem aqui

- `GET /api/platform/admin/pods` — só admin. Lista todo pod no namespace `fiap-games`: nome, aplicação (da própria label `app` do pod), namespace, node, fase, contagem de containers prontos, contagem de reinícios, horário de início.
- Os cinco serviços de backend existentes expõem cada um seu próprio `GET /api/<prefixo>/admin/status` (SHA do build + horário do build) — este serviço **não** agrega isso; o frontend compõe os cinco diretamente, mais a lista de pods deste serviço, numa única visão do painel. Veja `../documentation/spec/notes.md`.

## Testar

```bash
cd tests/FiapGames.Platform.Tests && dotnet test
```

## Documentação

Arquitetura completa, contratos de eventos e o registro de decisões de todo o projeto vivem no repositório `documentation` — [`github.com/tc2-fiap/documentation`](https://github.com/tc2-fiap/documentation).
