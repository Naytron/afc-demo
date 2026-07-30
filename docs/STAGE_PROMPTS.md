# Stage Prompts — AFC Demo

Scoped, testable **kickoff prompts** — one per stage — to hand to the Copilot coding agent (or to guide
your own work). Run them **in order**, **one PR per stage**, using the loop in
[EXECUTION_PLAYBOOK.md](./EXECUTION_PLAYBOOK.md). **Stage 1** lives in
[BOOTSTRAP_PROMPT.md](./BOOTSTRAP_PROMPT.md).

**Global rules for every stage** (assume these even where a prompt doesn't repeat them):

- **Stay in scope.** Build only the current stage. Do not pull work forward from later stages.
- **Keep the baseline green.** All prior stages' exit criteria still pass. New features ship behind a
  flag that is **OFF** by default (only `trophy-screen` and `lineup-notification` are ON).
- **Conventions.** C#/.NET (latest LTS); kebab‑case for files/resources/GitHub, markdown files
  `UPPERCASE_WITH_UNDERSCORES`; containers where they make sense; provider access stays behind
  `IFootballDataProvider`.
- **Prove it.** Satisfy the stage's **test gates** in [EXECUTION_PLAYBOOK.md](./EXECUTION_PLAYBOOK.md#4-test-gates)
  and its **exit criteria** in [DELIVERY_PLAN.md](../DELIVERY_PLAN.md). Update
  [CHANGELOG.md](../CHANGELOG.md) and tick the stage checklist.
- **Read first.** `README.md`, `ARCHITECTURE.md`, `DELIVERY_PLAN.md`, `SECURITY.md`, `docs/CI_CD.md`,
  and this file before starting.

---

## Stage 2 — Deploy the simple site to Azure

**Prereq:** Stage 1 merged and green. **Introduces:** Static Web Apps, Container Apps, App
Configuration, App Insights, ACR, Bicep, and the deploy workflows.

> **You are executing Stage 2 of AFC Demo: deploy the existing Stage 1 site to Azure without changing
> app behavior.** Read `ARCHITECTURE.md` (subscription topology, naming §9), `docs/CI_CD.md`, and
> `SETUP.md`.
>
> **Scope**
> - **Bicep IaC** under `infra/` provisioning per environment: Azure **Static Web Apps** (`afc-web`),
>   **Container App** for `afc-api` (+ its Container Apps environment), **App Configuration**
>   (feature flags), **Application Insights**/Log Analytics, and use of the shared **ACR** in
>   `afc-platform`. Parameterize by environment (`dev`, `prod`); single region **East US 2**.
> - Resource names follow `ARCHITECTURE.md#9-naming--tagging-standards` (e.g. `rg-afc-dev`,
>   `afc-web-dev`, `afc-api-dev`, `afcacrplatform`). Apply the standard tags.
> - **GitHub OIDC** workload‑identity federation to `afc-dev` and `afc-prod` (no stored secrets).
> - Turn the documented skeletons in `docs/CI_CD.md` into **live workflows**: `cd-dev.yml`,
>   `cd-prod.yml` (required‑reviewer approval), `e2e.yml`, and `manual-test.yml`; keep `ci.yml`.
>   Pin actions to commit SHAs, least‑privilege `permissions`, `concurrency`, `id-token: write` only
>   where OIDC is used.
> - **PR preview environments**; run Playwright e2e + a smoke script against the deployed preview.
> - Move feature‑flag source of truth to **App Configuration**, keeping the local JSON fallback.
>
> **Do not** add auth, Cosmos, AI, Event Hubs, or APIM. No new user‑facing features.
>
> **Tests / DoD**
> - Merge to `main` deploys to `afc-dev`; a tagged release deploys to `afc-prod` after approval.
> - Deployed **smoke + e2e** are green; flags read from App Config; rollback (revert PR / redeploy prior
>   image) is documented in the PR.
> - Meets [DELIVERY_PLAN Stage 2](../DELIVERY_PLAN.md#stage-2--deploy-simple-site-to-azure) exit criteria.

---

## Stage 3 — Accounts & auth

**Prereq:** Stage 2 live. **Introduces:** Azure AD B2C, Cosmos DB.

> **You are executing Stage 3 of AFC Demo: add accounts and social sign‑in.** Read `SECURITY.md`
> (identity) and `ARCHITECTURE.md` (data model).
>
> **Scope**
> - **Azure AD B2C** tenant + user flows (sign‑up/sign‑in, profile edit, password reset) with social
>   IdPs **Google, Apple, X**. Provision via Bicep/config; store secrets in **Key Vault**.
> - Make the Stage 1 sign‑in affordance **real**: sign up / sign in from the **home screen** (single
>   simple screen). Anonymous users still see the full trophy screen.
> - **Cosmos DB** `users` container: profile + `lineupOptIn` boolean. Provision dev/prod.
> - Protect the API: validate B2C tokens; authorize account‑only endpoints; anonymous read of
>   trophy/standings remains open.
> - Add an **account menu** for signed‑in users (placeholder items gated by their flags).
>
> **Do not** build notifications, AI, search, or APIM.
>
> **Tests / DoD**
> - Integration tests with the Cosmos emulator; e2e covering **anonymous** and **authenticated** paths
>   for each social IdP; opt‑in persists.
> - Meets [DELIVERY_PLAN Stage 3](../DELIVERY_PLAN.md#stage-3--accounts--auth) exit criteria.

---

## Stage 4 — Lineup notifications

**Prereq:** Stage 3 live. **Introduces:** Event Hubs, ingestion Functions, push channel, provider swap
to API-Football. **Flag:** `lineup-notification` (default **ON**).

> **You are executing Stage 4 of AFC Demo: notify opted‑in accounts the moment starting lineups are
> posted.** Read `docs/ADR/0001-football-data-provider.md` and `ARCHITECTURE.md`.
>
> **Scope**
> - Add a **FootballApiProvider** (API-Football) implementing `GetLineups`/`GetMatchStats` behind the
>   existing `IFootballDataProvider`; select by config. Keep the mock/football-data.org providers for
>   local/no‑secret runs.
> - **Timer‑triggered ingestion Function** polls for fixtures/lineups and publishes `fixture` and
>   `lineup` events to **Azure Event Hubs**.
> - Core API consumes events, projects them to **Cosmos**, and triggers a **push** to opted‑in users
>   (web push now; note APNs for future native — see `ROADMAP.md`).
> - Cache/throttle upstream calls (respect rate limits; APIM comes in Stage 8).
>
> **Do not** build ratings, news, search, admin, or APIM.
>
> **Tests / DoD**
> - Integration test: a simulated lineup event → notification delivered to an opted‑in test account.
> - Flag on/off proven (G7); trophy screen still green.
> - Meets [DELIVERY_PLAN Stage 4](../DELIVERY_PLAN.md#stage-4--lineup-notifications-default-on) exit criteria.

---

## Stage 5 — Player ratings

**Prereq:** Stage 4 live. **Introduces:** Microsoft Foundry, Semantic Kernel. **Flag:** `player-ratings`
(default **OFF**).

> **You are executing Stage 5 of AFC Demo: AI‑estimated + user player ratings.** Read `AGENTS.md`
> (Player Ratings agent, skills/functions) and `SECURITY.md` (WAF‑for‑AI).
>
> **Scope**
> - **Ratings Function** using **Semantic Kernel** + **Microsoft Foundry** to estimate per‑player
>   ratings from post‑match statistics; implement the plugin/functions defined in `AGENTS.md`.
> - Let signed‑in users submit their own ratings; store in **Cosmos** `ratings`.
> - Blend/display AI + user ratings with clear labelling; apply guardrails and a human‑in‑the‑loop note.
> - Gate everything behind `player-ratings` (OFF); support **targeting** (testers/premium).
>
> **Do not** build news, search, admin, or APIM.
>
> **Tests / DoD**
> - Unit/integration for the SK functions (grounded, deterministic where possible) and Cosmos
>   persistence; e2e with the flag on **and** off (off = feature fully absent).
> - Meets [DELIVERY_PLAN Stage 5](../DELIVERY_PLAN.md#stage-5--player-ratings-flagged) exit criteria.

---

## Stage 6 — Transfer news

**Prereq:** Stage 5 live. **Introduces:** scheduled agent Function; optional agentic content‑review PRs.
**Flag:** `transfer-news` (default **OFF**).

> **You are executing Stage 6 of AFC Demo: a daily AI agent that aggregates Arsenal transfer
> rumours/news.** Read `AGENTS.md` (Transfer News agent) and the agentic patterns in `docs/CI_CD.md`.
>
> **Scope**
> - **Daily scheduled Function** (Semantic Kernel agent) that aggregates transfer items, dedupes, and
>   scores **source + confidence**; store in **Cosmos** `transfers`.
> - Surface items in the app when `transfer-news` is on.
> - Optional: wire the **agentic GitHub pattern** where the job opens a content‑review PR for human
>   approval before publishing (per `docs/CI_CD.md`).
>
> **Do not** build search, admin, or APIM.
>
> **Tests / DoD**
> - Test a scheduled run producing deduped, confidence‑scored items; grounding reduces hallucination;
>   flag on/off proven.
> - Meets [DELIVERY_PLAN Stage 6](../DELIVERY_PLAN.md#stage-6--transfer-news-flagged) exit criteria.

---

## Stage 7 — Search

**Prereq:** Stage 6 live. **Introduces:** Azure AI Search. **Flag:** `search` (default **OFF**).

> **You are executing Stage 7 of AFC Demo: AI Search over Arsenal data.** Read `AGENTS.md` (Search
> agent) and `ARCHITECTURE.md` (data flows).
>
> **Scope**
> - **Azure AI Search** index projected from Cosmos (fixtures, results, ratings, stats) with an indexer
>   or push pipeline.
> - Search **API + UI**; relevance tuning; use the index as **grounding** for AI answers.
> - Gate behind `search` (OFF).
>
> **Do not** build admin or APIM.
>
> **Tests / DoD**
> - Relevance tests over a seeded corpus (results/players/ratings/stats return relevant hits); flag
>   on/off proven.
> - Meets [DELIVERY_PLAN Stage 7](../DELIVERY_PLAN.md#stage-7--search-flagged) exit criteria.

---

## Stage 8 — Governance & admin

**Prereq:** Stage 7 live. **Introduces:** API Management, admin portal, Key Vault/WAF hardening.

> **You are executing Stage 8 of AFC Demo: put governance in front of the platform and add an admin
> portal.** Read `ARCHITECTURE.md` and `SECURITY.md`.
>
> **Scope**
> - **Azure API Management** in front of the Core API: products, subscription keys, **rate limiting**,
>   and policies; route all clients through APIM.
> - **Admin portal** (Container App) for content/data CRUD (fixtures, ratings, transfers) behind
>   **admin RBAC**.
> - **Key Vault** hardening, a **WAF** security/cost review, alerting and dashboards.
>
> **Do not** add new end‑user features.
>
> **Tests / DoD**
> - APIM policy tests (throttling, keys); admin CRUD e2e with RBAC; clients verified to call **via
>   APIM**; WAF review actioned.
> - Meets [DELIVERY_PLAN Stage 8](../DELIVERY_PLAN.md#stage-8--governance--admin) exit criteria.

---

## Stage 9 — Mobile polish

**Prereq:** Stage 8 live. **Introduces:** PWA hardening; documents native iOS path.

> **You are executing Stage 9 of AFC Demo: harden the mobile/PWA experience.** Read `ROADMAP.md`
> (native iOS deferral) and `ARCHITECTURE.md`.
>
> **Scope**
> - PWA **install/offline/responsive** hardening, validated on **iOS Safari**.
> - Document the **native iOS** plan (MAUI vs Swift) and the **APNs** push migration from web push.
>
> **Do not** start native app development in this stage.
>
> **Tests / DoD**
> - Installable, responsive experience validated on iOS viewports; e2e for offline/install flows; native
>   path documented.
> - Meets [DELIVERY_PLAN Stage 9](../DELIVERY_PLAN.md#stage-9--mobile-polish) exit criteria.

---

## After Stage 9

The demo now spans the full Azure/Azure AI surface. Track post‑MVP items (e.g. **premium‑tier**
features, native iOS build, multi‑region) via [ROADMAP.md](../ROADMAP.md) and new ADRs under
[docs/ADR/](./ADR/), continuing the same one‑PR‑per‑increment, flag‑gated, test‑gated loop.
