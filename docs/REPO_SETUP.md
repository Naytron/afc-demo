# Repository Setup — AFC Demo (Stage 0)

Codified GitHub repo settings that complete the **Stage 0** foundation:
**branch protection**, the **`dev`/`prod` environments**, and the **`afc-runners`** runner group.
These are GitHub‑side settings and require a **remote repository** plus **admin** rights, so they are
applied by running the script below (or the equivalent `gh` calls) against your GitHub Enterprise repo.

> This repo currently has **no remote**. Create/point it at your GitHub Enterprise repo first, then run
> the script. Nothing here changes application behavior — it only hardens the repo and prepares
> environments used from Stage 2 onward.

---

## 1. What gets configured

### Branch protection on `main`

- Require a **pull request** with **≥ 1 approving review**.
- Require **CODEOWNERS** review (pairs with [`.github/CODEOWNERS`](../.github/CODEOWNERS)).
- Require **status checks** to pass. At Stage 0 the only check is **`docs-lint`**; later stages add
  `ci`, `e2e`, and `codeql` (see [docs/CI_CD.md](./CI_CD.md)).
- Require **conversation resolution**, **linear history**; **block force‑pushes** and deletions.

### Environments

- **`dev`** — auto‑deploy target from `main` (no manual gate). Introduced for use at Stage 2.
- **`prod`** — **required reviewers** (manual approval) and deployments restricted to `main`.

### Runner group

- **`afc-runners`** (org‑level) — self‑hosted runners in `afc-platform` for network‑restricted jobs,
  restricted to this repo and protected branches. GitHub‑hosted runners are used for build/test/e2e.

---

## 2. Apply it (script)

Prerequisites: [`gh`](https://cli.github.com/) authenticated (`gh auth login`) against your GitHub
Enterprise host with **repo admin** (and **org admin** for the runner group).

```powershell
# From the repo root:
./scripts/setup-github.ps1 `
  -Repo "your-org/afc-demo" `
  -ProdReviewers @("nlangston") `
  -RunnerGroup "afc-runners"
```

Parameters:

| Param | Required | Purpose |
| --- | --- | --- |
| `-Repo` | yes | `owner/name` of the GitHub repo to configure |
| `-ProdReviewers` | no | GitHub usernames required to approve `prod` deployments |
| `-RunnerGroup` | no | Runner group name (default `afc-runners`); org‑admin required |
| `-StatusChecks` | no | Required status check contexts (default `docs-lint`) |
| `-WhatIf` | no | Print the actions without applying them |

The script is **idempotent** for branch protection and environments (safe to re‑run); the runner group
step is skipped if the group already exists.

---

## 3. Apply it (manual `gh`, reference)

```bash
OWNER_REPO="your-org/afc-demo"

# dev environment (no protection)
gh api -X PUT "repos/$OWNER_REPO/environments/dev"

# prod environment with a required reviewer (resolve the user id first)
REVIEWER_ID=$(gh api "users/nlangston" --jq .id)
gh api -X PUT "repos/$OWNER_REPO/environments/prod" \
  -F "reviewers[][type]=User" -F "reviewers[][id]=$REVIEWER_ID" \
  -F "deployment_branch_policy[protected_branches]=true" \
  -F "deployment_branch_policy[custom_branch_policies]=false"

# branch protection on main (Stage 0 check = docs-lint)
gh api -X PUT "repos/$OWNER_REPO/branches/main/protection" \
  -F "required_status_checks[strict]=true" \
  -F "required_status_checks[contexts][]=docs-lint" \
  -F "required_pull_request_reviews[required_approving_review_count]=1" \
  -F "required_pull_request_reviews[require_code_owner_reviews]=true" \
  -F "required_linear_history=true" \
  -F "required_conversation_resolution=true" \
  -F "enforce_admins=false" -F "restrictions=" -F "allow_force_pushes=false" -F "allow_deletions=false"

# runner group (org-level; org admin required)
ORG="${OWNER_REPO%%/*}"
gh api -X POST "orgs/$ORG/actions/runner-groups" -f name="afc-runners" -f visibility="selected"
```

---

## 4. Stage 0 exit checklist

- [ ] `docs-lint` passes (run `npx markdownlint-cli2` locally; enforced by CI on PRs).
- [ ] Repo structure matches [README.md](../README.md) layout.
- [ ] `CODEOWNERS` in place; branch protection requires review.
- [ ] `dev` and `prod` environments created; `prod` has required reviewers.
- [ ] `afc-runners` runner group created and scoped to the repo.
- [ ] Reviewers sign off on the spec.

> Azure **OIDC federation** for `afc-dev`/`afc-prod` is set up in **Stage 2** (first cloud deploy),
> not here — see [docs/CI_CD.md](./CI_CD.md) and the Stage 2 prompt in
> [docs/STAGE_PROMPTS.md](./STAGE_PROMPTS.md).
