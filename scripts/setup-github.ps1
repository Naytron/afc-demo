<#
.SYNOPSIS
  Applies AFC Demo Stage 0 GitHub repo settings: branch protection, dev/prod environments,
  and (org repos only) the afc-runners runner group.

.DESCRIPTION
  Sends JSON request bodies via `gh api --input -` so nested objects, arrays, and null values
  (e.g. branch-protection `restrictions`) serialize correctly. Idempotent for branch protection and
  environments (PUT). The runner group is created only on org-owned repos and only if it does not
  already exist (personal accounts have no runner groups and the step is skipped with a warning).

  Requires the GitHub CLI (gh) authenticated with repo admin (and, for the runner group, org admin /
  the admin:org token scope).

.EXAMPLE
  ./scripts/setup-github.ps1 -Repo "naytron/afc-demo" -ProdReviewers @("Naytron")

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
$Owner = $Repo.Split('/')[0]

function Assert-GhReady {
    if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
        throw "GitHub CLI 'gh' not found. Install from https://cli.github.com/ and run 'gh auth login'."
    }
    gh auth status 1>$null 2>$null
    if ($LASTEXITCODE -ne 0) { throw "gh is not authenticated. Run 'gh auth login' first." }
}

function Invoke-GhJson {
    param([string]$Method, [string]$Path, [hashtable]$Body = @{})
    $json = ConvertTo-Json -InputObject $Body -Depth 12
    if ($PSCmdlet.ShouldProcess($Path, "$Method")) {
        $json | & gh api -X $Method $Path --input - 1>$null
        if ($LASTEXITCODE -ne 0) { throw "gh api $Method $Path failed (exit $LASTEXITCODE)." }
    } else {
        Write-Host "  [WhatIf] gh api -X $Method $Path --input -`n$json"
    }
}

Assert-GhReady

# Is the repo owner an organization? Runner groups exist only for orgs.
$ownerType = (& gh api "users/$Owner" --jq '.type' 2>$null)
Write-Host "Configuring $Repo (owner: $Owner, type: $ownerType)" -ForegroundColor Cyan

# --- 1. dev environment (no protection) -------------------------------------------------
Write-Host "`n[1/4] Environment: dev" -ForegroundColor Yellow
Invoke-GhJson -Method PUT -Path "repos/$Repo/environments/dev" -Body @{}

# --- 2. prod environment (required reviewers, main-only) --------------------------------
Write-Host "`n[2/4] Environment: prod" -ForegroundColor Yellow
$reviewers = @()
foreach ($r in $ProdReviewers) {
    $id = (& gh api "users/$r" --jq '.id')
    if ($LASTEXITCODE -ne 0 -or -not $id) { throw "Could not resolve GitHub user '$r'." }
    $reviewers += @{ type = 'User'; id = [int]$id }
    Write-Host "  required reviewer: $r (id $id)"
}
if ($reviewers.Count -eq 0) {
    Write-Warning "No -ProdReviewers supplied; prod will have no manual approval gate."
}
$prodBody = @{
    reviewers                = $reviewers
    deployment_branch_policy = @{ protected_branches = $true; custom_branch_policies = $false }
}
try {
    Invoke-GhJson -Method PUT -Path "repos/$Repo/environments/prod" -Body $prodBody
} catch {
    Write-Warning "Environment protection rules rejected (reviewers/branch-policy need a public repo or a Team/Enterprise org). Creating a bare 'prod' environment; add the approval gate once the plan supports it."
    Invoke-GhJson -Method PUT -Path "repos/$Repo/environments/prod" -Body @{}
}

# --- 3. branch protection on main -------------------------------------------------------
Write-Host "`n[3/4] Branch protection: main" -ForegroundColor Yellow
$bpBody = @{
    required_status_checks           = @{ strict = $true; contexts = @($StatusChecks) }
    enforce_admins                   = $false
    required_pull_request_reviews    = @{
        required_approving_review_count = 1
        require_code_owner_reviews      = $true
        dismiss_stale_reviews           = $true
    }
    restrictions                     = $null
    required_linear_history          = $true
    required_conversation_resolution = $true
    allow_force_pushes               = $false
    allow_deletions                  = $false
}
Invoke-GhJson -Method PUT -Path "repos/$Repo/branches/main/protection" -Body $bpBody

# --- 4. runner group (org-owned repos only) --------------------------------------------
Write-Host "`n[4/4] Runner group: $RunnerGroup" -ForegroundColor Yellow
if ($ownerType -ne 'Organization') {
    Write-Warning "Owner '$Owner' is a $ownerType, not an Organization. Runner groups are an org-only feature; skipping. Move the repo to an org to use '$RunnerGroup'."
} else {
    $existing = (& gh api "orgs/$Owner/actions/runner-groups" --jq ".runner_groups[] | select(.name==`"$RunnerGroup`") | .name" 2>$null)
    if ($existing) {
        Write-Host "  runner group '$RunnerGroup' already exists; skipping."
    } else {
        try {
            '{"name":"' + $RunnerGroup + '","visibility":"selected","restricted_to_workflows":false}' |
                & gh api -X POST "orgs/$Owner/actions/runner-groups" --input - 1>$null
            if ($LASTEXITCODE -ne 0) { throw "POST returned exit $LASTEXITCODE" }
            Write-Host "  created runner group '$RunnerGroup'. Now add self-hosted runners and scope it to $Repo."
        } catch {
            Write-Warning "Runner group step failed (org admin / admin:org scope required?). Create '$RunnerGroup' manually per docs/REPO_SETUP.md. $_"
        }
    }
}

Write-Host "`nDone. Verify against the Stage 0 checklist in docs/REPO_SETUP.md." -ForegroundColor Green
