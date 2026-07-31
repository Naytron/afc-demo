# Changelog — AFC Demo

All notable changes to this project are documented here. The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and this project adheres to
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- **Execution playbook** ([docs/EXECUTION_PLAYBOOK.md](./docs/EXECUTION_PLAYBOOK.md)): the per‑stage
  build loop, test gates (G0–G8), branching/PR conventions, environment promotion, and rollback.
- **Stage prompts** ([docs/STAGE_PROMPTS.md](./docs/STAGE_PROMPTS.md)): scoped, testable kickoff prompts
  for Stages 2–9 (one PR per stage).
- **Stage 0 repo foundation:** `.markdownlint-cli2.jsonc` lint config, `.github/CODEOWNERS`, and a
  `docs-lint` GitHub Actions workflow ([.github/workflows/docs-lint.yml](./.github/workflows/docs-lint.yml)).
- **Repo settings as code:** [scripts/setup-github.ps1](./scripts/setup-github.ps1) and
  [docs/REPO_SETUP.md](./docs/REPO_SETUP.md) to apply branch protection, `dev`/`prod` environments, and
  the `afc-runners` runner group.
- Initialized the git repository and added a .NET/OS `.gitignore`.

### Changed

- Normalized markdown across the docs to satisfy the Stage 0 lint gate (**0 issues**); refreshed the
  README repository‑layout tree and Stage 0 checklist in `DELIVERY_PLAN.md`.
- Planned: Stage 1 local simple site (Trophy/standings home) — see
  [DELIVERY_PLAN.md](./DELIVERY_PLAN.md).

## [0.1.0] — 2026-07-30

### Added

- Initial **specification package** for the AFC Demo:
  - `README.md`, `ARCHITECTURE.md` (with Mermaid diagrams), `SECURITY.md`, `ROADMAP.md`,
    `SETUP.md`, `DELIVERY_PLAN.md`, `AGENTS.md`.
  - `.github/PULL_REQUEST_TEMPLATE.md`.
  - `docs/BOOTSTRAP_PROMPT.md` — engineered prompt to generate the Stage 1 starting point.
  - `docs/CI_CD.md` — GitHub Enterprise CI/CD strategy + workflow skeletons.
  - `docs/ADR/0001-football-data-provider.md` — 3rd‑party football data API decision.
- Defined the **staged delivery model** (Stages 0–9) with a testable baseline per stage.
- Defined the **3‑subscription topology** (`platform`, `dev`, `prod`), naming/tagging standards, and
  **feature‑flag strategy**.

[Unreleased]: https://example.com/afc-demo/compare/v0.1.0...HEAD
[0.1.0]: https://example.com/afc-demo/releases/tag/v0.1.0
