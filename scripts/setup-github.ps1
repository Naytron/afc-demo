<#
.SYNOPSIS
  Applies AFC Demo Stage 0 GitHub repo settings: branch protection, dev/prod environments,
  and the afc-runners runner group.

.DESCRIPTION
  Idempotent for branch protection and environments (PUT). The runner group is created only if it
  does not already exist. Requires the GitHub CLI (gh) authenticated against your GitHub Enterprise
  host with repo admin (and org admin for the runner group).

.EXAMPLE
  ./scripts/setup-github.ps1 -Repo "your-org/afc-demo" -ProdReviewers @("nlangston")

.NOTES
  Naming: GitHub/Azure resources use kebab-case; markdown files use UPPERCASE_WITH_UNDERSCORES.
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[^/]+/[^/]+$')]
    [string]$Repo,

    [string[]]$ProdReviewers = @(),

    [string]$RunnerGroup = 'afc-runners',

    [string[]]$StatusChecks = @('docs-lint')
)

$ErrorActionPreference = 'Stop'
$Org = $Repo.Split('/')[0]

function Assert-GhReady {
    if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
        throw "GitHub CLI 'gh' not found. Install from https://cli.github.com/ and run 'gh auth login'."
    }
    gh auth status 1>$null 2>$null
    if ($LASTEXITCODE -ne 0) { throw "gh is not authenticated. Run 'gh auth login' first." }
}

function Invoke-GhApi {
    param([string]$Method, [string]$Path, [string[]]$ApiArgs = @())
    if ($PSCmdlet.ShouldProcess($Path, "$Method")) {
        & gh api -X $Method $Path @ApiArgs
        if ($LASTEXITCODE -ne 0) { throw "gh api $Method $Path failed (exit $LASTEXITCODE)." }
    } else {
        Write-Host "  [WhatIf] gh api -X $Method $Path $($ApiArgs -join ' ')"
    }
}

Assert-GhReady
Write-Host "Configuring $Repo (org: $Org)" -ForegroundColor Cyan

# --- 1. dev environment (no protection) -------------------------------------------------
Write-Host "`n[1/4] Environment: dev" -ForegroundColor Yellow
Invoke-GhApi -Method PUT -Path "repos/$Repo/environments/dev"

# --- 2. prod environment (required reviewers, main-only) --------------------------------
Write-Host "`n[2/4] Environment: prod" -ForegroundColor Yellow
$prodArgs = @(
    '-F', 'deployment_branch_policy[protected_branches]=true',
    '-F', 'deployment_branch_policy[custom_branch_policies]=false'
)
foreach ($r in $ProdReviewers) {
    $id = (& gh api "users/$r" --jq '.id')
    if ($LASTEXITCODE -ne 0 -or -not $id) { throw "Could not resolve GitHub user '$r'." }
    $prodArgs += @('-F', 'reviewers[][type]=User', '-F', "reviewers[][id]=$id")
    Write-Host "  required reviewer: $r (id $id)"
}
if ($ProdReviewers.Count -eq 0) {
    Write-Warning "No -ProdReviewers supplied; prod will have no manual approval gate. Re-run with reviewers to enforce it."
}
Invoke-GhApi -Method PUT -Path "repos/$Repo/environments/prod" -ApiArgs $prodArgs

# --- 3. branch protection on main -------------------------------------------------------
Write-Host "`n[3/4] Branch protection: main" -ForegroundColor Yellow
$bpArgs = @(
    '-F', 'required_status_checks[strict]=true',
    '-F', 'required_pull_request_reviews[required_approving_review_count]=1',
    '-F', 'required_pull_request_reviews[require_code_owner_reviews]=true',
    '-F', 'required_linear_history=true',
    '-F', 'required_conversation_resolution=true',
    '-F', 'enforce_admins=false',
    '-F', 'restrictions=',
    '-F', 'allow_force_pushes=false',
    '-F', 'allow_deletions=false'
)
foreach ($c in $StatusChecks) { $bpArgs += @('-F', "required_status_checks[contexts][]=$c") }
Invoke-GhApi -Method PUT -Path "repos/$Repo/branches/main/protection" -ApiArgs $bpArgs

# --- 4. runner group (org-level) --------------------------------------------------------
Write-Host "`n[4/4] Runner group: $RunnerGroup" -ForegroundColor Yellow
$existing = (& gh api "orgs/$Org/actions/runner-groups" --jq ".runner_groups[] | select(.name==`"$RunnerGroup`") | .name" 2>$null)
if ($existing) {
    Write-Host "  runner group '$RunnerGroup' already exists; skipping."
} else {
    try {
        Invoke-GhApi -Method POST -Path "orgs/$Org/actions/runner-groups" -ApiArgs @(
            '-f', "name=$RunnerGroup",
            '-f', 'visibility=selected',
            '-F', 'restricted_to_workflows=false'
        )
        Write-Host "  created runner group '$RunnerGroup'. Now add self-hosted runners and scope it to $Repo."
    } catch {
        Write-Warning "Runner group step failed (org admin required?). Create '$RunnerGroup' manually per docs/REPO_SETUP.md. $_"
    }
}

Write-Host "`nDone. Verify against the Stage 0 checklist in docs/REPO_SETUP.md." -ForegroundColor Green
