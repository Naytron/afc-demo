# Bootstrap Prompt — AFC Demo (Stage 1 starting point)

This is the **engineered prompt** to hand to a coding agent (e.g., GitHub Copilot / Copilot coding
agent) to generate the **Stage 1** starting point of the AFC Demo. It is intentionally scoped to a
**locally runnable, testable simple site** and points at the spec for everything that comes later.

**How to use:** open this repo with the coding agent, paste the prompt in
[§ The prompt](#the-prompt), and let it scaffold Stage 1. Verify against the **Stage 1 checklist** in
[DELIVERY_PLAN.md](../DELIVERY_PLAN.md#stage-1--local-simple-site). Do **not** let it build cloud infra
or later‑stage features — those are separate stages.

---

## Guardrails for the agent

- **Scope = Stage 1 only.** No Azure resources, no auth, no AI, no Event Hubs, no APIM. Those arrive in
  later stages per [DELIVERY_PLAN.md](../DELIVERY_PLAN.md).
- **Must run locally with no secrets** via a **mock provider**; a football-data.org provider is added
  behind the same interface but is optional locally.
- **Keep the baseline green:** unit/component + Playwright e2e must pass locally and in `ci.yml`.
- **Honor conventions:** kebab-case for files/resources/GitHub; C#/.NET (latest LTS); Blazor WASM PWA;
  .NET Aspire AppHost; feature flags with a local JSON fallback.
- **Feature flags:** `trophy-screen` and `lineup-notification` **ON**; scaffold the flag mechanism even
  though most features don't exist yet.

---

## The prompt

> **You are scaffolding Stage 1 of the "AFC Demo" application.** Read `README.md`,
> `ARCHITECTURE.md`, `SETUP.md`, `DELIVERY_PLAN.md`, and `docs/ADR/0001-football-data-provider.md` in
> this repository and follow them. Build **only Stage 1**: a simple, locally runnable, testable
> Arsenal fan website. Do **not** create any Azure infrastructure, authentication, AI features,
> messaging, or API Management — those are later stages.
>
> **Tech & structure**
>
> - Language: **C#**, targeting the latest **.NET LTS**.
> - Frontend: **Blazor WebAssembly PWA** (installable, responsive; mobile‑first). Project `afc-web`.
> - Backend: a **minimal Core API** (`afc-api`) exposing trophy/standings endpoints.
> - Orchestration: a **.NET Aspire AppHost** (`afc-apphost`) that runs `afc-web` and `afc-api`
>   together locally, plus an **optional** Cosmos DB emulator resource (not required for Stage 1 data).
> - Solution layout under `src/`, tests under `tests/`, kebab-case project folder names. Add
>   `.editorconfig`, `.gitignore` (.NET), and a `Directory.Build.props` for shared settings.
>
> **Features (Stage 1)**
>
> 1. **Trophy standing screen (home page):**
>    - Shows the **next Arsenal fixture**, or the **current/live fixture if it is a gameday**.
>    - Shows **Arsenal's standing in each competition** they are in (Premier League, Champions League,
>      FA Cup, Carabao Cup) as **clickable trophy silhouettes** (generic placeholder art — no Arsenal
>      trademarks). A competition Arsenal is **eliminated** from renders **greyscale**.
>    - Clicking a trophy opens a **competition detail screen** with standings/stats for that
>      competition.
>    - Clean, nice‑looking, responsive layout that works well on mobile viewports.
> 2. **Login/sign‑up affordance (UI only in Stage 1):** show a simple sign‑in entry point on the home
>    screen (social login buttons: Google, Apple, X) that is **non‑functional/stubbed** with a clear
>    "coming in Stage 3" state. Do **not** integrate Azure AD B2C yet.
>
> **Data access**
>
> - Define `IFootballDataProvider` with methods `GetFixtures`, `GetStandings` (and stubs for
>   `GetLineups`, `GetMatchStats` that throw `NotSupportedException` for now).
> - Provide **two implementations**: (a) a **MockFootballDataProvider** returning realistic sample data
>   (default locally, requires no secrets), and (b) a **FootballDataOrgProvider** reading from
>   football-data.org using a key from user‑secrets, selected via configuration. Cache reads in memory.
> - The API and UI depend only on the interface and the domain model (`Fixture`, `Standing`,
>   `Competition`).
>
> **Feature flags**
>
> - Integrate `Microsoft.FeatureManagement` with a **local JSON fallback**. Register flags
>   `trophy-screen` (ON), `lineup-notification` (ON), and placeholders `player-ratings`,
>   `transfer-news`, `search`, `premium-tier` (OFF). Gate the (stub) lineup‑notification UI on its flag.
>
> **Testing**
>
> - **Unit tests** for providers and standings/fixture logic (including greyscale/eliminated logic and
>   gameday selection).
> - **Component tests** (bUnit) for the trophy home and competition detail components.
> - **Playwright e2e**: home renders next/live fixture + four trophies; clicking a trophy navigates to
>   its detail; eliminated competition shows greyscale; sign‑in buttons are visible but stubbed.
> - Make tests runnable via `dotnet test` and `npx playwright test` against the Aspire run.
>
> **CI**
>
> - Add a `.github/workflows/ci.yml` matching `docs/CI_CD.md` (build + unit/component + lint; pin
>   actions to commit SHAs; least‑privilege `permissions: contents: read`; `concurrency`). Do **not**
>   add deploy workflows yet.
>
> **Docs**
>
> - Update `README.md` quickstart if commands differ, and add a Stage 1 entry to `CHANGELOG.md`.
> - Do **not** modify `ARCHITECTURE.md`, `DELIVERY_PLAN.md`, `SECURITY.md`, or `ROADMAP.md` except to
>   fix factual drift.
>
> **Definition of done (Stage 1)**
>
> - `dotnet run --project ./src/afc-apphost` serves the site; home shows the next/live fixture and four
>   trophies with correct greyscale/clickthrough.
> - All unit/component/e2e tests pass locally and in `ci.yml`.
> - No cloud dependency, no secrets required (mock provider by default).
>
> Work in small, reviewable commits. Follow kebab-case and the naming/flag conventions above. When
> done, summarize what you built and how to run and test it, and list what is intentionally deferred to
> Stage 2+.

---

## After Stage 1

Proceed stage‑by‑stage using [DELIVERY_PLAN.md](../DELIVERY_PLAN.md). Each subsequent stage has its own
scope, exit criteria, and (later) its own follow‑on prompt. Suggested next prompt (Stage 2) theme:
*"Add Bicep IaC, App Configuration, and the `cd-dev`/`cd-prod`/`e2e`/`manual-test` workflows from
`docs/CI_CD.md` to deploy the Stage 1 site to `afc-dev` and `afc-prod` in East US 2 via OIDC — without
changing app behavior."*
