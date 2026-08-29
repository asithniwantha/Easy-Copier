# ArchiveBranches.ps1
# This script archives all local and remote branches by prefixing them with 'archived/'
# It skips 'main' and the current working branch.

$ErrorActionPreference = 'Stop'

# Get the current working branch
$currentBranch = git branch --show-current
if ([string]::IsNullOrWhiteSpace($currentBranch)) {
    Write-Host "Could not determine the current branch. Are you in a detached HEAD state?"
    throw "Detached HEAD state"
}

Write-Host "Current branch is: $currentBranch. It will not be archived."

# ==========================================
# 1. Archive Local Branches
# ==========================================
Write-Host "`n--- Processing Local Branches ---"
$localBranches = git branch --format="%(refname:short)" | ForEach-Object { $_.Trim() }

foreach ($branch in $localBranches) {
    if ([string]::IsNullOrWhiteSpace($branch) -or $branch -eq "main" -or $branch -eq $currentBranch) {
        continue
    }

    Write-Host "Archiving local branch: $branch"
    git branch -m $branch "archived/$branch"
}

# ==========================================
# 2. Archive Remote Branches
# ==========================================
Write-Host "`n--- Processing Remote Branches ---"
# Get remote branches, e.g. "origin/feature-x"
$remoteBranches = git branch -r --format="%(refname:short)" | ForEach-Object { $_.Trim() }

foreach ($rBranch in $remoteBranches) {
    # Skip HEAD pointers, e.g. "origin/HEAD"
    if ($rBranch -match "origin/HEAD") {
        continue
    }

    # We only care about branches that start with 'origin/'
    if ($rBranch -match "^origin/(.+)$") {
        $branchName = $matches[1]

        if ([string]::IsNullOrWhiteSpace($branchName) -or $branchName -eq "main" -or $branchName -eq $currentBranch) {
            continue
        }

        # If it's already archived on the remote, skip
        if ($branchName -match "^archived/") {
            continue
        }

        Write-Host "Archiving remote branch: $branchName"
        # Push new branch reference
        git push origin "origin/${branchName}:refs/heads/archived/${branchName}"

        # Delete original remote branch
        git push origin --delete $branchName
    }
}

Write-Host "`nAll branches have been successfully archived."
