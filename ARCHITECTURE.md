# Architecture — AFC Demo

This document describes the **target architecture**, how it is **introduced stage‑by‑stage**, the
**subscription/environment topology**, **CI/CD**, the **AI/data flows**, and the mapping to the
**Azure Well‑Architected Framework (WAF)**.

Diagrams are authored as **Mermaid** (rendered by GitHub) so they can be regenerated/versioned as
code. Static exports (if needed) live in [`docs/diagrams/`](./docs/diagrams/).

---

## 1. Context (C4 — Level 1)

```mermaid
flowchart TB
    fan["Fan (web / installed PWA)"]:::user
    admin["Content admin"]:::user

    subgraph afc["AFC Demo System"]
        web["Web PWA (Blazor WASM)"]
        api["Core API"]
        ai["AI services (agents / search / ratings)"]
        adminportal["Admin portal"]
    end

    footballData["3rd-party football data\n(football-data.org / API-Football)"]:::ext
    b2c["Azure AD B2C\n(Google / Apple / X)"]:::ext
    foundry["Microsoft Foundry\n(models)"]:::ext
    push["Push / notification channel"]:::ext

    fan --> web
    admin --> adminportal
    web --> api
    adminportal --> api
    api --> ai
    api --> footballData
    web --> b2c
    ai --> foundry
    api --> push

    classDef user fill:#EF0107,stroke:#7a0106,color:#fff;
    classDef ext fill:#eef,stroke:#557;
```

---

## 2. Target-state architecture (all stages complete)

```mermaid
flowchart LR
    subgraph edge["Edge"]
        swa["Azure Static Web Apps\n(afc-web) — Blazor WASM PWA"]
    end

    subgraph gw["Gateway"]
        apim["Azure API Management\n(afc-apim)"]
    end

    subgraph compute["Compute"]
        coreapi["Core API\n(Container App: afc-api)"]
        ingest["Ingestion Functions\n(afc-fn-ingest, timer)"]
        ratingsfn["Ratings Job\n(afc-fn-ratings)"]
        transferfn["Transfer-News Agent\n(afc-fn-transfer, daily)"]
        adminportal["Admin Portal\n(Container App: afc-admin)"]
    end

    subgraph data["Data & messaging"]
        cosmos["Cosmos DB\n(afc-cosmos)"]
        events["Event Hubs\n(afc-events)"]
        search["Azure AI Search\n(afc-search)"]
        appcfg["App Configuration\n(afc-appconfig) + Feature Flags"]
        kv["Key Vault\n(afc-kv)"]
    end

    subgraph ai["AI"]
        sk["Semantic Kernel\n(in compute)"]
        foundry["Microsoft Foundry\n(models)"]
    end

    subgraph obs["Cross-cutting"]
        appi["App Insights / Log Analytics"]
        b2c["Azure AD B2C"]
    end

    ext["Football data provider\n(IFootballDataProvider)"]:::ext

    swa --> apim --> coreapi
    adminportal --> apim
    swa --> b2c
    coreapi --> cosmos
    coreapi --> appcfg
    coreapi --> kv
    coreapi --> search
    coreapi --> ext
    ingest --> ext
    ingest --> events --> coreapi
    ratingsfn --> sk --> foundry
    ratingsfn --> cosmos
    transferfn --> sk
    transferfn --> cosmos
    search --> cosmos
    coreapi --> appi
    ingest --> appi

    classDef ext fill:#eef,stroke:#557;
```

**Design intent**

- **APIM** fronts all callable APIs (Stage 8) for governance, throttling, product keys, and a stable
  contract for web + admin + future native iOS.
- **Core API** runs as a **Container App** (scales to zero, revisions for blue/green).
- **Event‑driven ingestion**: timer Functions poll the provider, publish fixture/lineup events to
  **Event Hubs**; the API projects them into Cosmos and triggers notifications.
- **AI features** are isolated Functions using **Semantic Kernel** over **Foundry** models, writing
  results to **Cosmos** and indexing into **AI Search**.
- **Config & secrets** are externalized to **App Configuration** (flags) and **Key Vault** (secrets),
  accessed via **managed identity**.

---

## 3. Staged architecture evolution

Infrastructure is introduced **only at the stage that needs it**. Every stage keeps a testable
baseline.

### Stage 1 — Local simple site (no cloud)

```mermaid
flowchart LR
    dev["Developer"] --> apphost[".NET Aspire AppHost"]
    subgraph local["Local (Aspire-orchestrated)"]
        webL["Blazor WASM PWA"]
        apiL["Minimal Core API"]
        cosmosL["Cosmos DB emulator (optional)"]
    end
    apphost --> webL
    apphost --> apiL
    apphost --> cosmosL
    apiL --> provider["football-data.org (free)\nor mock provider"]
    webL --> apiL
```

Testable: unit/component tests + Playwright e2e against the local Aspire run.

### Stage 2 — Deploy simple site to Azure

```mermaid
flowchart LR
    gh["GitHub Actions"] --> swa["Static Web Apps (afc-web)"]
    gh --> api2["Core API (Container App)"]
    swa --> api2
    api2 --> appcfg2["App Configuration (flags)"]
    api2 --> provider2["football-data.org (free)"]
    api2 --> appi2["App Insights"]
```

Adds: Bicep IaC, resource groups per environment, App Configuration, monitoring, CI/CD + manual + e2e.

### Stage 3 — Accounts & auth

```mermaid
flowchart LR
    web3["Web PWA"] --> b2c3["Azure AD B2C\n(Google/Apple/X)"]
    web3 --> api3["Core API"]
    api3 --> cosmos3["Cosmos DB\n(users, opt-ins)"]
    api3 --> appcfg3["App Configuration"]
```

### Stage 4 — Lineup notifications (event‑driven; API‑Football)

```mermaid
flowchart LR
    ingest4["Ingestion Function (timer)"] --> apif["API-Football (paid)"]
    ingest4 --> events4["Event Hubs"]
    events4 --> api4["Core API"]
    api4 --> cosmos4["Cosmos DB"]
    api4 --> push4["Push channel (opt-in users)"]
```

### Stage 5–7 — AI features

```mermaid
flowchart LR
    subgraph s5["Stage 5 · Player ratings"]
      ratings["Ratings Function"] --> sk5["Semantic Kernel"] --> foundry5["Foundry"]
      ratings --> cosmos5["Cosmos (ratings)"]
    end
    subgraph s6["Stage 6 · Transfer news"]
      transfer["Daily Agent Function"] --> sk6["Semantic Kernel"] --> foundry6["Foundry"]
      transfer --> cosmos6["Cosmos (news)"]
    end
    subgraph s7["Stage 7 · Search"]
      search7["Azure AI Search"] --> cosmos7["Cosmos (indexed)"]
      api7["Core API"] --> search7
    end
```

### Stage 8 — Governance & admin

```mermaid
flowchart LR
    web8["Web / Admin / (future iOS)"] --> apim8["API Management"]
    apim8 --> api8["Core API"]
    admin8["Admin Portal (Container App)"] --> apim8
    apim8 --> kv8["Key Vault"]
```

---

## 4. Subscription & environment topology (3‑subscription model)

```mermaid
flowchart TB
    subgraph platformSub["Subscription: afc-platform"]
        acr["Container Registry\n(afc-acr)"]
        sharedsearch["Shared AI Search / Foundry\n(non-prod pooling)"]
        ghrunners["Self-hosted runner infra\n(runner group: afc-runners)"]
        obsplat["Central Log Analytics"]
        dns["DNS / Front Door (future)"]
    end

    subgraph devSub["Subscription: afc-dev"]
        rgdev["rg-afc-dev\n(all app resources, dev SKUs)"]
    end

    subgraph prodSub["Subscription: afc-prod"]
        rgprod["rg-afc-prod\n(all app resources, prod SKUs)"]
    end

    acr --> rgdev
    acr --> rgprod
    ghrunners --> rgdev
    ghrunners --> rgprod
    obsplat --> rgdev
    obsplat --> rgprod
```

**Guidance**

- **`afc-platform`** — shared/expensive/long‑lived: ACR, central monitoring, runner infrastructure,
  DNS, and pooled AI capacity for non‑prod. Managed by the platform team; changes are rare and
  reviewed.
- **`afc-dev`** — full app stack at low‑cost SKUs; target of PR/CI deployments. Ephemeral‑friendly.
- **`afc-prod`** — hardened, prod SKUs, restricted access, deploy only via protected `prod`
  environment with required reviewers.
- **Region:** single US region to start — **East US 2** recommended (broad Foundry/model + service
  availability). All resources tagged with region for future multi‑region expansion.
- **Identity:** each environment uses **workload identity federation** (OIDC) from GitHub → Azure;
  no long‑lived cloud credentials in GitHub.

---

## 5. CI/CD flow

```mermaid
flowchart LR
    pr["Pull Request"] --> ci["CI: build + unit/component + lint"]
    ci --> preview["Preview deploy (SWA preview / dev)"]
    preview --> e2e["e2e (Playwright) + smoke"]
    e2e --> merge["Merge to main"]
    merge --> cddev["CD → afc-dev"]
    cddev --> gate{"Manual approval\n(prod environment)"}
    gate --> cdprod["CD → afc-prod"]
    manual["workflow_dispatch\n(manual test / targeted deploy)"] --> cddev
```

See [docs/CI_CD.md](./docs/CI_CD.md) for workflow skeletons, runner groups, and environments.

---

## 6. Feature‑flag strategy

- Backed by **Azure App Configuration** feature manager; evaluated with `Microsoft.FeatureManagement`
  in API and web.
- Flags: `trophy-screen` (**ON**), `lineup-notification` (**ON**), `player-ratings` (OFF),
  `transfer-news` (OFF), `search` (OFF), `premium-tier` (OFF).
- Flags support **targeting filters** (e.g., roll a feature to premium users or a % audience) and are
  changed **without redeploying**. Local dev uses a JSON fallback so the site runs offline.

---

## 7. Azure Well‑Architected Framework mapping

| Pillar | How the design addresses it |
| --- | --- |
| **Reliability** | Container Apps revisions + health probes; event‑driven ingestion decoupled via Event Hubs; Cosmos DB SLA + retry policies; staged rollout keeps a known‑good baseline; provider abstraction avoids single 3rd‑party lock. |
| **Security** | Azure AD B2C for identity; managed identities everywhere (no keys in code); Key Vault for secrets; APIM for gateway policy, throttling, and product keys; least‑privilege RBAC per subscription; see [SECURITY.md](./SECURITY.md). |
| **Cost Optimization** | Scale‑to‑zero Container Apps + Functions consumption; free/low SKUs in `afc-dev`; pooled non‑prod AI capacity in `afc-platform`; feature flags avoid shipping idle compute; free football data tier until paid data is required. |
| **Operational Excellence** | Everything as code (Bicep + Mermaid + workflows); per‑stage checklists; App Insights/Log Analytics; agentic GitHub for routine PRs; changelog + ADRs. |
| **Performance Efficiency** | CDN‑delivered static PWA; async ingestion; AI Search for fast retrieval; Cosmos partitioning by competition/season; Foundry model selection per workload. |

> The AI workloads additionally follow the **WAF for AI** guidance: model/version pinning, prompt &
> output logging (privacy‑filtered), grounding via AI Search, and human‑in‑the‑loop for user‑facing
> ratings/news.

---

## 8. Data model (high level)

```mermaid
erDiagram
    USER ||--o{ RATING : submits
    USER {
      string id PK
      string b2cSubject
      bool   lineupOptIn
      string tier
    }
    COMPETITION ||--o{ FIXTURE : contains
    FIXTURE ||--o{ RATING : has
    FIXTURE ||--o{ LINEUP : has
    RATING {
      string id PK
      string fixtureId FK
      string playerId
      int    userScore
      float  aiScore
      string source
    }
    TRANSFER_ITEM {
      string id PK
      date   publishedOn
      string summary
      float  confidence
    }
```

- **Cosmos containers** partitioned for scale: `users` (/id), `fixtures` (/competitionId),
  `ratings` (/fixtureId), `transfers` (/publishedOn), `content-index` for search projection.

---

## 9. Naming & tagging standards

**Resource naming (kebab-case):** `afc-<workload>-<env>`

| Resource | Example (dev) |
| --- | --- |
| Resource group | `rg-afc-dev` |
| Static Web App | `afc-web-dev` |
| Core API (Container App) | `afc-api-dev` |
| Function apps | `afc-fn-ingest-dev`, `afc-fn-ratings-dev`, `afc-fn-transfer-dev` |
| Cosmos DB | `afc-cosmos-dev` |
| Event Hubs namespace | `afc-events-dev` |
| API Management | `afc-apim-dev` |
| AI Search | `afc-search-dev` |
| App Configuration | `afc-appconfig-dev` |
| Key Vault | `afc-kv-dev` |
| Container Registry (platform) | `afcacrplatform` (registry names are alphanumeric) |

**Required tags:** `app=afc`, `env=dev|prod|platform`, `owner=<team>`, `costCenter=<id>`,
`region=eastus2`, `managedBy=bicep`.

**GitHub (kebab-case):** repos/branches/workflows/labels use kebab-case
(e.g. `ci.yml`, `cd-dev.yml`, `e2e.yml`, branch `feat/trophy-screen`). Markdown files use
`UPPERCASE_WITH_UNDERSCORES`.

---

## 10. Key decisions

- **Football data provider** — start free (football-data.org), upgrade to API-Football when lineups /
  match stats / transfers are needed. See
  [ADR‑0001](./docs/ADR/0001-football-data-provider.md).
- **Frontend = Blazor WASM PWA** — aligns with C# stack, installable on iOS as a stopgap before the
  deferred native app, and is a natural fit for Static Web Apps.
- **iOS native deferred** — responsive PWA first; APNs/native path documented in
  [ROADMAP.md](./ROADMAP.md).
