# Security — AFC Demo

This document describes the security posture, identity model, secret handling, and threat
considerations for the AFC Demo. It aligns with the **Security** pillar of the Azure
Well‑Architected Framework and the WAF‑for‑AI guidance. Security controls are introduced
**per stage** alongside the features that need them.

> This is a demo/reference application. It uses only generic placeholder imagery (no Arsenal FC
> trademarks) and synthetic/aggregated public data.

---

## 1. Identity & access

### End users
- **Azure AD B2C** provides sign‑up / sign‑in with social identity providers: **Google, Apple, X**.
- Anonymous users can always use the default **Trophy** screen; account‑only features require a valid
  B2C token.
- Web app uses **PKCE / authorization code flow**; tokens are validated by the Core API (issuer,
  audience, expiry, signature).

### Service‑to‑service
- **Managed identities** for every Azure service interaction (Cosmos, App Config, Key Vault, Event
  Hubs, AI Search, Foundry). **No connection strings or keys in code or pipelines.**
- **Least‑privilege RBAC**: scoped data‑plane roles (e.g., Cosmos DB Built‑in Data Contributor,
  Key Vault Secrets User) per identity, per environment.

### Administrators
- Admin portal (Stage 8) is gated by an **admin app role**/group and fronted by **API Management**;
  privileged operations are audited.

### CI/CD
- GitHub → Azure uses **workload identity federation (OIDC)** — no stored cloud credentials.
- Deployments to `afc-prod` require the protected **prod environment** with required reviewers.

---

## 2. Secrets & configuration
- **Key Vault** (`afc-kv-<env>`) stores secrets (e.g., API-Football key, push credentials).
- **App Configuration** (`afc-appconfig-<env>`) stores non‑secret config + **feature flags**;
  secret references point at Key Vault.
- Access via managed identity only. **Secret scanning** and **push protection** enabled on the repo.
- Local dev uses **user‑secrets** / `.env` (git‑ignored) and never commits real keys; a mock provider
  removes the need for real keys at Stage 1.

---

## 3. Network & gateway
- **API Management** (Stage 8) enforces product keys, rate limiting, IP/JWT validation, and payload
  policies for all callable APIs.
- Container Apps use **ingress restrictions**; internal services are not publicly exposed.
- Single US region initially; design leaves room for **Front Door/WAF** at the edge in the future.

---

## 4. Data protection & privacy
- **Encryption in transit** (TLS) and **at rest** (platform‑managed keys; CMK optional).
- Cosmos DB partitioned by non‑PII keys where possible; user PII limited to what B2C provides + opt‑in
  flags.
- **Data minimization**: store only what a feature needs. User ratings are attributable but contain no
  sensitive personal data.
- Notification opt‑ins are explicit and revocable.
- Retention/erasure: user records deletable on account closure (GDPR‑style DSR path documented for
  prod).

---

## 5. AI‑specific security (WAF for AI)
- **Model/version pinning** in Foundry; changes tracked via ADR/CHANGELOG.
- **Grounding**: transfer‑news and search answers are grounded in retrieved content (AI Search) with
  **confidence scoring**.
- **Human‑in‑the‑loop** for user‑facing generated content (ratings/news) before broad display.
- **Prompt/response logging** with PII filtering for evaluation and abuse detection.
- **Content safety** filters on generated output; guardrails documented in [AGENTS.md](./AGENTS.md).

---

## 6. Threat model (summary — STRIDE)

| Threat | Example | Mitigation |
| --- | --- | --- |
| **Spoofing** | Fake tokens / IdP impersonation | B2C token validation, OIDC federation, no static creds |
| **Tampering** | Altering ratings/content | AuthZ on writes, server‑side validation, audit logs |
| **Repudiation** | Denying an admin change | Audit trail via App Insights + APIM logs |
| **Information disclosure** | Leaking keys/PII | Key Vault + managed identity, least‑privilege RBAC, secret scanning |
| **Denial of service** | Abusive traffic / 3rd‑party rate limits | APIM throttling, caching in Cosmos, scale rules |
| **Elevation of privilege** | User accessing admin ops | Admin role/group, APIM policy, RBAC separation |
| **AI abuse** | Prompt injection / bad content | Input sanitization, grounding, content safety, human review |

---

## 7. Secure SDLC
- Branch protection + required reviews on `main`; signed commits encouraged.
- **CodeQL** / dependency review + **Dependabot** on PRs.
- **Container image scanning** in ACR; base images pinned and regularly updated.
- IaC (Bicep) reviewed; policy checks in CI (e.g., no public data endpoints, tags required).
- Per‑stage security check in the [Definition of Done](./DELIVERY_PLAN.md#definition-of-done-every-stage).

---

## 8. Reporting a vulnerability
This is a demo repository. For real deployments, report suspected vulnerabilities privately to the
repository owners (e.g., via a GitHub **private security advisory**) and **do not** open a public
issue. Provide reproduction steps and impact; expect acknowledgement and a remediation timeline in the
advisory thread.
