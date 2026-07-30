# Roadmap — AFC Demo

The roadmap ties **features** to **delivery stages** (see [DELIVERY_PLAN.md](./DELIVERY_PLAN.md)) and
to the **feature‑flag rollout**. Everything is delivered incrementally so a testable baseline is
always available.

---

## Guiding principles
- **Testable baseline every stage** — never break the running Trophy screen.
- **Introduce infra just‑in‑time** — a service appears only when a feature needs it.
- **Flags over branches** — features merge early, hidden behind flags, and roll out via targeting.
- **AI with guardrails** — grounding, confidence, and human‑in‑the‑loop for user‑facing content.

---

## Feature → stage → flag matrix

| Feature | Stage | Flag | Default | Rollout approach |
| --- | --- | --- | --- | --- |
| Trophy standing screen (home) | 1 | `trophy-screen` | **ON** | GA from Stage 1 |
| Accounts & social login | 3 | (core, not flagged) | ON | GA at Stage 3 |
| Lineup notification | 4 | `lineup-notification` | **ON** | GA; opt‑in per user |
| Player ratings | 5 | `player-ratings` | OFF | Testers → premium → % audience → GA |
| Transfer news | 6 | `transfer-news` | OFF | Internal review → GA |
| Search | 7 | `search` | OFF | Beta cohort → GA |
| Premium account tier | Future | `premium-tier` | OFF | Enables higher‑tier features |
| Native iOS app | Future | n/a | — | After PWA validation (Stage 9) |

---

## Timeline (indicative)

```mermaid
gantt
    title AFC Demo — indicative roadmap (2-week stages)
    dateFormat  YYYY-MM-DD
    axisFormat  %b %d
    section Foundation
    Spec & foundation (S0)      :done,   s0, 2026-08-03, 14d
    Local simple site (S1)      :active, s1, after s0, 14d
    section Cloud
    Deploy to Azure (S2)        :        s2, after s1, 14d
    Accounts & auth (S3)        :        s3, after s2, 14d
    section Live & AI
    Lineup notifications (S4)   :        s4, after s3, 14d
    Player ratings (S5)         :        s5, after s4, 14d
    Transfer news (S6)          :        s6, after s5, 14d
    Search (S7)                 :        s7, after s6, 14d
    section Harden
    Govern & admin (S8)         :        s8, after s7, 14d
    Mobile polish (S9)          :        s9, after s8, 14d
```

---

## Near‑term (MVP: Stages 1–4)
- Trophy/standings home + competition detail screens.
- Deploy to Azure with full CI/CD, previews, and e2e.
- B2C social login and account opt‑ins.
- Live lineup notifications (upgrade to API-Football).

## Mid‑term (Stages 5–7)
- AI player ratings (Foundry + Semantic Kernel) + user ratings.
- Daily AI transfer‑news agent.
- AI Search across results/players/ratings/stats.

## Longer‑term (Stage 8+ / Future)
- API Management governance + backend admin portal.
- **Premium account tier** unlocking additional features.
- **Native iOS app** (evaluate .NET MAUI vs Swift) with **APNs** push, replacing web‑push where
  richer capabilities are needed.
- Multi‑region expansion (design already tags region for portability).
- Additional social/data integrations and richer stats.

---

## Explicitly deferred (documented, not in MVP)
- Native mobile apps (iOS first) — **deferred**; responsive PWA ships first.
- Multi‑region / DR — single US region (East US 2) to start.
- CMK encryption, Front Door/edge WAF — noted as future hardening.

See [CHANGELOG.md](./CHANGELOG.md) for what has actually shipped.
