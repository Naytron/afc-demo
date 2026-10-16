# Setup — AFC Demo

How to prepare a machine for local development and how each environment is provisioned. Stage 1 is available locally with mock data. This guide describes the prerequisites for the local
baseline and later cloud stages.

---

## 1. Prerequisites (local)

| Tool | Purpose | Notes |
| --- | --- | --- |
| **.NET SDK** (latest LTS) | Build/run C# app + Aspire | `dotnet --info` to verify |
| **.NET Aspire packages** | Local orchestration | Restored from NuGet with the solution |
| **Docker Desktop** | Containers + Cosmos emulator | Required when a later stage adds container resources |
| **Node.js LTS** | Playwright e2e / tooling | For test tooling |
| **Azure CLI** | Cloud interaction | `az login` |
| **Bicep CLI** | IaC (Stage 2+) | `az bicep install` |
| **GitHub CLI** (optional) | Repo/workflow ops | `gh auth login` |

> A **mock football data provider** is included so Stage 1 runs with **no external keys**.

---

## 2. Run locally (Stage 1+)

```bash
# from the repo root once app code exists
dotnet restore
dotnet run --project ./src/afc-apphost      # .NET Aspire AppHost
```

The Aspire dashboard shows the composed resources (web PWA, Core API, optional Cosmos emulator). Open
the web endpoint it prints to view the **Trophy** home screen.

### Configuration & secrets (local)

- Non‑secret config + feature flags: local `appsettings.Development.json` / JSON flag fallback
  (`trophy-screen` and `lineup-notification` ON).
- Secrets: **user‑secrets** (`dotnet user-secrets`) — never commit real keys.
- To use real data locally, set the provider to football-data.org and supply a key via user‑secrets;
  otherwise the mock provider is used.

### Tests

```bash
dotnet test                       # unit + component
npx playwright test               # e2e (against the local Aspire run or a preview URL)
```

---

## 3. Environments & subscriptions

| Environment | Subscription | Purpose |
| --- | --- | --- |
| Local | — | Aspire on developer machine |
| `dev` | `afc-dev` | CI deploys; low‑cost SKUs |
| `prod` | `afc-prod` | Protected; required reviewers |
| Shared | `afc-platform` | ACR, central monitoring, runner infra, pooled non‑prod AI |

Region: **East US 2** (single region to start). See
[ARCHITECTURE.md](./ARCHITECTURE.md#4-subscription--environment-topology-3subscription-model).

---

## 4. Cloud provisioning (Stage 2+)

Infrastructure is **Bicep** and applied per environment via CI/CD (no manual portal changes for
app resources).

```bash
# Validate the Stage 2 resource-group template locally
az bicep build \
  --file ./infra/main.bicep

# CI performs this deployment after Azure OIDC login
az deployment group create \
  --resource-group rg-afc-dev \
  --template-file ./infra/main.bicep \
  --parameters ./infra/params.dev.bicepparam
```

### One‑time platform setup

1. Create subscriptions/resource groups: `rg-afc-dev`, `rg-afc-prod`, platform RG.
2. Create **ACR** and central Log Analytics in `afc-platform`. The intended registry name is
   `afcacrplatform`; Azure registry names are globally unique, so this deployment uses the
   subscription-suffixed `afcacrplatformebad4aa3` after the intended name was unavailable.
3. Configure **GitHub OIDC federation** (app registrations / federated credentials) for `dev` and
   `prod`; grant deployment Contributor access to the respective app resource group and AcrPush
   access to the shared ACR. The deployment identity also needs User Access Administrator on the ACR
   scope to grant the Container App its AcrPull role.
4. Create GitHub **environments** (`dev`, `prod`) and **runner group** `afc-runners`
   (see [docs/CI_CD.md](./docs/CI_CD.md)).

### GitHub environment configuration

Set these non-secret environment variables in both `dev` and `prod`: `AZURE_CLIENT_ID`,
`AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`, `ACR_NAME`, `ACR_LOGIN_SERVER`, `ACR_RESOURCE_ID`,
`RESOURCE_OWNER`, and `COST_CENTER`. The workflows exchange GitHub's OIDC token for Azure access;
they retrieve the Static Web Apps deployment token at runtime and do not store it in GitHub.

### Deployment and rollback

`cd-dev.yml` deploys every `main` push to `rg-afc-dev`; `cd-prod.yml` deploys a published release
through the protected `prod` environment. Both deploy an immutable API image tagged with the commit
SHA and publish a PWA build configured with the deployed API URL. To roll back, redeploy a prior
commit or release so its immutable image is selected, then redeploy the matching PWA build. Use App
Configuration feature flags as the immediate kill switch for a flag-controlled capability.

### Per‑stage additions

- Stage 3: provision **Azure AD B2C** tenant + user flows + social IdP app registrations
  (Google, Apple, X).
- Stage 4: **Event Hubs** + switch provider secret to **API-Football**.
- Stage 5–7: **Foundry** deployment, **AI Search** service, model access.
- Stage 8: **API Management** + admin portal + Key Vault hardening.

---

## 5. Feature flags

Flags live in **Azure App Configuration** (`afc-appconfig-<env>`) and are changed **without a
redeploy**. Local development falls back to JSON flags so the app runs offline. See
[ARCHITECTURE.md](./ARCHITECTURE.md#6-featureflag-strategy).

---

## 6. Troubleshooting

- **Aspire won't start containers** → ensure Docker Desktop is running.
- **401 from API locally** → B2C not needed for the Trophy screen; only account features require a
  token (Stage 3+).
- **Rate‑limited data** → football-data.org free tier is ~10 req/min; use the mock provider for
  iterative dev.
- **No AI output locally** → Foundry access is a Stage 5+ concern; earlier stages don't require it.
