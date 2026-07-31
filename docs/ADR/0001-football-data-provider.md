# ADR 0001 — Third‑party football data provider

- **Status:** Accepted
- **Date:** 2026-07-30
- **Deciders:** AFC Demo architecture
- **Related:** [ARCHITECTURE.md](../../ARCHITECTURE.md), [DELIVERY_PLAN.md](../../DELIVERY_PLAN.md)

## Context

The app needs football data: competitions, fixtures, standings (Trophy screen), plus **lineups**
(lineup notifications), **per‑player match statistics** (player ratings), and **transfer** signals.
Two candidates were evaluated as starting assumptions:

- **football-data.org — free tier**
- **API-Football — paid tier**

Key differences relevant to our features:

| Capability | football-data.org (free) | API-Football (paid) |
| --- | --- | --- |
| Competitions / fixtures / standings | ✅ (limited competitions) | ✅ (broad) |
| Rate limit | ~10 requests/min | Much higher (plan‑dependent) |
| **Lineups** (pre‑match) | ❌ | ✅ |
| **Per‑player match stats** | ❌ | ✅ |
| Events / live detail | Limited | ✅ |
| Transfers | ❌ | ✅ (plus external sources) |
| Cost | Free | Paid (per plan) |

The **Trophy standing screen** and early stages only need fixtures + standings — well within the free
tier. The **lineup notification**, **player ratings**, and **transfer news** features require data the
free tier does not provide.

## Decision

1. **Introduce an abstraction** `IFootballDataProvider` (surfaced to agents via `FootballDataPlugin`)
   with a stable domain model (`Fixture`, `Standing`, `Lineup`, `MatchStats`).
2. **Stages 1–2:** implement with **football-data.org (free tier)** for fixtures/standings, plus a
   **mock provider** for offline/local dev. Cache responses in Cosmos to respect the ~10 req/min limit.
3. **Stage 4+:** add an **API-Football** implementation and switch via configuration when lineups /
   match stats / transfers are needed. Provider selection is **config‑only** (no code change in
   consumers).

## Rationale

- Keeps demo **cost near zero** through the first cloud milestone.
- Avoids **vendor lock‑in**; the swap is a configuration change behind one interface.
- Aligns provider capability with the **stage that needs it**, matching the just‑in‑time infra
  principle and keeping each stage testable.

## Consequences

- **Positive:** low starting cost; clean seam for testing (mock), caching, and rate‑limit handling;
  agents/API share one integration.
- **Negative / trade‑offs:** two client implementations to maintain; domain model must reconcile
  differing provider schemas; lineup/stats/transfer features are **blocked until** API-Football is
  enabled (reflected in the Stage 4 plan).
- **Operational:** store the API-Football key in Key Vault; apply APIM/caching to protect rate limits;
  monitor provider errors and fall back gracefully (feature flags can disable dependent features).

## Alternatives considered

- **API-Football from day one:** richer data immediately, but adds cost before it's needed and
  complicates the free local‑first experience.
- **football-data.org only:** cheapest, but cannot support lineups/ratings/transfers — a hard blocker
  for three planned features.
- **Multiple providers merged behind a facade permanently:** more resilience, but unnecessary
  complexity for a demo; the abstraction already allows adding providers later.
