<!--
  Naming standard: markdown files use UPPERCASE_WITH_UNDERSCORES.
  All GitHub/Azure resource names use kebab-case.
-->

# AFC Demo — Arsenal Fan App

> A reference/demo application for showcasing **Azure** and **Azure AI** technologies end‑to‑end,
> built as an Arsenal Football Club (AFC) fan experience for **web and mobile (iOS)**.

[![Stage](https://img.shields.io/badge/stage-0%20·%20spec-blue)](./DELIVERY_PLAN.md)
[![Well-Architected](https://img.shields.io/badge/Azure-Well--Architected-0078D4)](./ARCHITECTURE.md)
[![License](https://img.shields.io/badge/license-MIT-green)](#license)

---

## What this is

`afc-demo` is a **staged, testable** reference application. It is delivered as a specification
package first (this repository), then built in numbered stages where **each stage keeps a
green/testable baseline** and introduces cloud infrastructure and features **only when they are
needed** — not all up front.

The app itself is an Arsenal fan site: a clean "Trophy" home screen showing the next/live fixture and
Arsenal's standing in each competition, with progressively richer, AI‑powered features gated behind
feature flags.

> **This repository currently contains the specification only** (docs, diagrams, and a bootstrap
> prompt). Application code is generated from [`docs/BOOTSTRAP_PROMPT.md`](./docs/BOOTSTRAP_PROMPT.md)
> starting at Stage 1.

## Documentation map

| Document | Purpose |
| --- | --- |
| [ARCHITECTURE.md](./ARCHITECTURE.md) | Target + per‑stage architecture, diagrams, WAF mapping, subscription model |
| [DELIVERY_PLAN.md](./DELIVERY_PLAN.md) | Stages 0–9, milestones, and per‑stage checklists |
| [ROADMAP.md](./ROADMAP.md) | Feature roadmap and feature‑flag rollout schedule |
| [SETUP.md](./SETUP.md) | Local development (.NET Aspire) + per‑environment setup |
| [SECURITY.md](./SECURITY.md) | Identity (B2C), secrets, threat model, WAF security pillar |
| [AGENTS.md](./AGENTS.md) | AI agent descriptions + Semantic Kernel skills/functions |
| [CHANGELOG.md](./CHANGELOG.md) | Keep‑a‑Changelog history |
| [docs/EXECUTION_PLAYBOOK.md](./docs/EXECUTION_PLAYBOOK.md) | How to drive each stage: per‑stage loop, test gates, one PR per stage |
| [docs/STAGE_PROMPTS.md](./docs/STAGE_PROMPTS.md) | Scoped, testable kickoff prompt per stage (Stages 2–9) |
| [docs/BOOTSTRAP_PROMPT.md](./docs/BOOTSTRAP_PROMPT.md) | The engineered prompt that generates Stage 1 |
| [docs/CI_CD.md](./docs/CI_CD.md) | GitHub Enterprise CI/CD strategy + workflow skeletons |
| [docs/ADR/0001-football-data-provider.md](./docs/ADR/0001-football-data-provider.md) | 3rd‑party football data API decision |

## Feature summary

| Feature | Default | Stage | Notes |
| --- | --- | --- | --- |
| **Trophy standing screen** (home) | **ON** | 1 | Next/live fixture + standing per competition; clickable trophies → competition detail |
| **Lineup notification** | **ON** | 4 | Push notification the moment starting lineups drop (opt‑in) |
| Accounts & social login | ON | 3 | Sign up / sign in with Google, Apple, X via Azure AD B2C |
| Player ratings | Flagged OFF | 5 | AI‑estimated ratings post‑match + user ratings stored in Cosmos DB |
| Transfer news | Flagged OFF | 6 | Daily AI agent aggregates Arsenal transfer rumours/news |
| Search | Flagged OFF | 7 | AI Search across fixtures, results, ratings, and stats |
| Premium account tier | Flagged OFF | Future | Unlocks additional features (post‑MVP) |

All features are hidden behind **feature flags** except the two default‑ON features above.

## Technology stack

**App & runtime**
- **C#** / .NET, **.NET Aspire** for local orchestration and service composition
- **Blazor WebAssembly PWA** frontend on **Azure Static Web Apps** (installable, responsive; native
  iOS deferred — see [ROADMAP.md](./ROADMAP.md))
- **Azure Functions** (data ingestion, scheduled AI jobs) and **Azure Container Apps** (long‑running
  / stateful services) — containers where they make sense

**Data & integration**
- **Azure Cosmos DB** (user profiles, ratings, aggregated content)
- **Azure Event Hubs** (fixture/lineup event stream)
- **Azure API Management** (API gateway, governance, rate limiting) — introduced Stage 8
- 3rd‑party football data via an `IFootballDataProvider` abstraction (**football-data.org** →
  **API-Football**, see the [ADR](./docs/ADR/0001-football-data-provider.md))

**AI**
- **Microsoft Foundry** (model hosting/inference) + **Semantic Kernel** (agents, skills/functions)
- **Azure AI Search** (retrieval)

**Platform & delivery**
- **Azure AI Well‑Architected Framework** alignment across all 5 pillars
- **3 subscriptions**: `platform`, `dev`, `prod`; single US region to start (**East US 2** suggested)
- **Feature flags** via Azure App Configuration + `Microsoft.FeatureManagement`
- **GitHub Enterprise**: runner groups, environments, CI + manual + e2e workflows, agentic features

## Quickstart

Application code does not exist yet — it is generated at Stage 1. To begin:

1. Read [ARCHITECTURE.md](./ARCHITECTURE.md) and [DELIVERY_PLAN.md](./DELIVERY_PLAN.md).
2. Follow [SETUP.md](./SETUP.md) to prepare local prerequisites (.NET SDK, Aspire workload, Docker).
3. Read [docs/EXECUTION_PLAYBOOK.md](./docs/EXECUTION_PLAYBOOK.md) for the per‑stage build loop and test gates.
4. Use [docs/BOOTSTRAP_PROMPT.md](./docs/BOOTSTRAP_PROMPT.md) to generate the Stage 1 local site, then
   [docs/STAGE_PROMPTS.md](./docs/STAGE_PROMPTS.md) for Stages 2–9 (one PR per stage).
5. Verify the Stage 0/Stage 1 checklists in [DELIVERY_PLAN.md](./DELIVERY_PLAN.md).

## Repository layout

```text
afc-demo/
├─ README.md                 ├─ ARCHITECTURE.md        ├─ SECURITY.md
├─ ROADMAP.md                ├─ SETUP.md               ├─ CHANGELOG.md
├─ DELIVERY_PLAN.md          ├─ AGENTS.md              ├─ .gitignore
├─ .github/
│  ├─ PULL_REQUEST_TEMPLATE.md
│  └─ workflows/             # CI / CD / manual / e2e (skeletons — see docs/CI_CD.md)
└─ docs/
   ├─ EXECUTION_PLAYBOOK.md  # per‑stage build loop + test gates
   ├─ STAGE_PROMPTS.md       # kickoff prompt per stage (2–9)
   ├─ BOOTSTRAP_PROMPT.md    # Stage 1 kickoff prompt
   ├─ CI_CD.md
   ├─ diagrams/
   └─ ADR/0001-football-data-provider.md
```

## Naming conventions

- **GitHub & Azure resources:** `kebab-case` (e.g. `afc-web-dev`, `afc-api-prod`, `rg-afc-platform`).
- **Markdown files:** `UPPERCASE_WITH_UNDERSCORES` (e.g. `README.md`, `DELIVERY_PLAN.md`).
- See [ARCHITECTURE.md](./ARCHITECTURE.md#9-naming--tagging-standards) for the full resource naming and
  tagging scheme.

## License

MIT (demo/reference use). Arsenal FC trademarks, crests, and imagery are property of their respective
owners and are **not** included; this demo uses generic silhouettes/placeholders only.
