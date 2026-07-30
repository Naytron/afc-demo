# CI/CD — AFC Demo (GitHub Enterprise)

Strategy and **skeleton workflows** for building, testing, and deploying AFC Demo. Workflows are
introduced at **Stage 2** (first cloud deploy). This file documents the approach and provides
starting‑point YAML; live workflow files land under `.github/workflows/` when app code exists.

> **Security baseline for every workflow**
> - **Pin every `uses:` to a full‑length commit SHA** with a version comment; **Dependabot** keeps
>   them current. Never use `@main`/`@latest`/major‑tag references.
> - **Least‑privilege `permissions:`** at workflow level (default `contents: read`); grant
>   `id-token: write` only where OIDC login is needed.
> - **`concurrency:`** per branch/workflow to cancel superseded runs.
> - **OIDC federation** to Azure — **no stored cloud credentials**.
> - Environment secrets + **required reviewers** on `prod`.

---

## 1. Environments & runner groups
- **GitHub Environments:** `dev` (auto‑deploy from `main`) and `prod` (required reviewers,
  branch = `main`, wait timer optional).
- **Runner group `afc-runners`:** self‑hosted runners in `afc-platform` for jobs needing private
  network access (e.g., APIM/admin); GitHub‑hosted runners for build/test/e2e. Restrict the group to
  the `afc-demo` repo/org and to protected branches.
- **OIDC:** federated credentials per environment map to least‑privilege Azure RBAC on `afc-dev` /
  `afc-prod`.

## 2. Workflow inventory

| File (kebab-case) | Trigger | Purpose |
| --- | --- | --- |
| `ci.yml` | `pull_request`, `push: main` | Build + unit/component + lint + IaC validate |
| `e2e.yml` | `pull_request` (after preview), nightly `schedule` | Playwright e2e + smoke |
| `cd-dev.yml` | `push: main` | Deploy to `afc-dev` (SWA + Container App) |
| `cd-prod.yml` | `workflow_dispatch`, release published | Approved deploy to `afc-prod` |
| `manual-test.yml` | `workflow_dispatch` (inputs) | Ad‑hoc/manual test run against a chosen env |
| `codeql.yml` | `pull_request`, `schedule` | Static analysis (security) |

---

## 3. Skeletons

### `ci.yml`
```yaml
name: ci
on:
  pull_request:
  push:
    branches: [main]

permissions:
  contents: read

concurrency:
  group: ci-${{ github.workflow }}-${{ github.ref }}
  cancel-in-progress: true

jobs:
  build-test:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout
        uses: actions/checkout@34e114876b0b11c390a56381ad16ebd13914f8d5 # v4.3.1
      - name: Setup .NET
        uses: actions/setup-dotnet@e2be71bcb6de6d0d18f1c1cd54c6a2a04f9b6bc # v4.x — pin to verified SHA
        with:
          dotnet-version: '9.0.x'   # latest LTS
      - name: Restore
        run: dotnet restore
      - name: Build
        run: dotnet build --no-restore -c Release
      - name: Test (unit + component)
        run: dotnet test --no-build -c Release --collect:"XPlat Code Coverage"
      - name: Validate Bicep
        run: az bicep build --file ./infra/main.bicep
```

### `e2e.yml`
```yaml
name: e2e
on:
  pull_request:
  schedule:
    - cron: '0 6 * * *'   # nightly

permissions:
  contents: read

concurrency:
  group: e2e-${{ github.ref }}
  cancel-in-progress: true

jobs:
  playwright:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@34e114876b0b11c390a56381ad16ebd13914f8d5 # v4.3.1
      - uses: actions/setup-node@3235b876344d2a9aa001b8d1453c930bba69e610 # v3.9.1
        with:
          node-version: 20
      - name: Install Playwright
        run: npx --yes playwright install --with-deps
      - name: Run e2e
        env:
          BASE_URL: ${{ vars.PREVIEW_URL || 'http://localhost:5000' }}
        run: npx playwright test
      - name: Upload report
        if: always()
        uses: actions/upload-artifact@bbbca2ddaa5d8feaa63e36b76fdaad77386f024f # v7.0.0
        with:
          name: playwright-report
          path: playwright-report
```

### `cd-dev.yml` (OIDC to Azure)
```yaml
name: cd-dev
on:
  push:
    branches: [main]

permissions:
  contents: read
  id-token: write        # required for OIDC federation

concurrency:
  group: cd-dev
  cancel-in-progress: false

jobs:
  deploy:
    runs-on: ubuntu-latest
    environment: dev
    steps:
      - uses: actions/checkout@34e114876b0b11c390a56381ad16ebd13914f8d5 # v4.3.1
      - name: Azure login (OIDC)
        uses: azure/login@a457da9ea143d694b1b9c7c869ebb04ebe844ef5 # v2.x — pin to verified SHA
        with:
          client-id: ${{ secrets.AZURE_CLIENT_ID }}
          tenant-id: ${{ secrets.AZURE_TENANT_ID }}
          subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
      - name: Provision (Bicep)
        run: |
          az deployment sub create \
            --location eastus2 \
            --template-file ./infra/main.bicep \
            --parameters ./infra/params.dev.bicepparam
      - name: Deploy web (Static Web Apps) & api (Container App)
        run: ./scripts/deploy.sh dev
```

### `cd-prod.yml` (manual/approved)
```yaml
name: cd-prod
on:
  workflow_dispatch:
  release:
    types: [published]

permissions:
  contents: read
  id-token: write

concurrency:
  group: cd-prod
  cancel-in-progress: false

jobs:
  deploy:
    runs-on: ubuntu-latest
    environment: prod           # required reviewers enforce manual approval
    steps:
      - uses: actions/checkout@34e114876b0b11c390a56381ad16ebd13914f8d5 # v4.3.1
      - name: Azure login (OIDC)
        uses: azure/login@a457da9ea143d694b1b9c7c869ebb04ebe844ef5 # v2.x — pin to verified SHA
        with:
          client-id: ${{ secrets.AZURE_CLIENT_ID }}
          tenant-id: ${{ secrets.AZURE_TENANT_ID }}
          subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
      - name: Deploy to prod
        run: ./scripts/deploy.sh prod
```

### `manual-test.yml` (manual test harness)
```yaml
name: manual-test
on:
  workflow_dispatch:
    inputs:
      environment:
        description: Target environment
        type: choice
        options: [dev, prod]
        default: dev
      suite:
        description: Test suite
        type: choice
        options: [smoke, e2e, api]
        default: smoke

permissions:
  contents: read

jobs:
  run:
    runs-on: ubuntu-latest
    environment: ${{ inputs.environment }}
    steps:
      - uses: actions/checkout@34e114876b0b11c390a56381ad16ebd13914f8d5 # v4.3.1
      - name: Run ${{ inputs.suite }} against ${{ inputs.environment }}
        run: ./scripts/test.sh --suite ${{ inputs.suite }} --env ${{ inputs.environment }}
```

---

## 4. Deployment flow
See [ARCHITECTURE.md](../ARCHITECTURE.md#5-cicd-flow): PR → CI → preview → e2e → merge → `dev` →
manual approval → `prod`. Each stage from [DELIVERY_PLAN.md](../DELIVERY_PLAN.md) extends these
workflows (e.g., Stage 4 adds Event Hubs infra, Stage 8 adds APIM) without breaking the baseline.

## 5. Agentic GitHub features
- **Copilot coding agent** for routine PRs (dependency bumps, small fixes, doc updates) with required
  human review before merge.
- **Transfer‑news editorial pattern (Stage 6):** the Transfer News agent may open a **draft PR / issue**
  with proposed content for human approval, keeping generated content in the review workflow (see
  [AGENTS.md](../AGENTS.md)).
- **Dependabot** for actions + packages; **CodeQL** + secret scanning + push protection enabled.
- Branch protection on `main`: required checks (`ci`, `e2e`, `codeql`), required reviews, linear
  history.

## 6. Runner group setup (summary)
1. Create runner group `afc-runners` (org‑level), restrict to `afc-demo` and protected branches.
2. Register self‑hosted runners in `afc-platform` (labels: `self-hosted`, `afc`, `linux`).
3. Use GitHub‑hosted runners for build/test/e2e; self‑hosted only for network‑restricted deploy steps.
