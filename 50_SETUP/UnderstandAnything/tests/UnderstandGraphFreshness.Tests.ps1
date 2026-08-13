$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$modulePath = Join-Path (Split-Path $PSScriptRoot) 'UnderstandGraphFreshness.psm1'
Import-Module $modulePath -Force

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

function Write-TestGraph {
    param(
        [string]$Path,
        [string]$Commit,
        [string]$AnalyzedAt,
        [string]$EnrichmentVersion = '',
        [string]$Marker = ''
    )
    $project = [ordered]@{ gitCommitHash = $Commit; analyzedAt = $AnalyzedAt }
    if ($EnrichmentVersion) { $project.enrichmentVersion = $EnrichmentVersion }
    $graph = [ordered]@{
        version = '1.0.0'
        project = $project
        nodes = @([ordered]@{ id = "file:$Marker"; type = 'file'; name = $Marker; summary = $Marker; tags = @('test') })
        edges = @()
    }
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($Path)) | Out-Null
    [IO.File]::WriteAllText($Path, ($graph | ConvertTo-Json -Depth 8), [Text.UTF8Encoding]::new($false))
}

$testRoot = Join-Path ([IO.Path]::GetTempPath()) "dreamine-understand-freshness-$([Guid]::NewGuid().ToString('N'))"
$repositoryRoot = Join-Path $testRoot 'repository'
$uaRoot = Join-Path $repositoryRoot '.ua'
[IO.Directory]::CreateDirectory($uaRoot) | Out-Null
$rawPath = Join-Path $uaRoot 'knowledge-graph.json'
$snapshotPath = Join-Path $uaRoot 'knowledge-graph.pre-enrichment.json'

try {
    $oldCommit = '1111111111111111111111111111111111111111'
    $newCommit = '2222222222222222222222222222222222222222'

    Write-TestGraph $snapshotPath $oldCommit '2026-07-16T00:00:00Z' -Marker 'old-snapshot'
    Write-TestGraph $rawPath $newCommit '2026-08-12T00:00:00Z' -Marker 'fresh-raw'
    $selection = Resolve-UnderstandBaseGraph -RepositoryRoot $repositoryRoot -RawGraphPath $rawPath `
        -SnapshotGraphPath $snapshotPath -CurrentCommit $newCommit -RefreshSnapshot
    Assert-True ($selection.SelectedPath -eq $rawPath) 'A fresh HEAD-matching raw graph must win over a stale snapshot.'
    Assert-True $selection.RefreshedSnapshot 'Selecting a fresh raw graph must refresh the pre-enrichment snapshot.'
    Assert-True ((Get-Content -LiteralPath $snapshotPath -Raw).Contains('fresh-raw')) 'The refreshed snapshot must contain the selected raw graph.'

    Write-TestGraph $snapshotPath $newCommit '2026-08-12T00:00:00Z' -Marker 'raw-snapshot'
    Write-TestGraph $rawPath $newCommit '2026-08-12T01:00:00Z' -EnrichmentVersion 'dreamine-api-v1' -Marker 'published-ko'
    $selection = Resolve-UnderstandBaseGraph -RepositoryRoot $repositoryRoot -RawGraphPath $rawPath `
        -SnapshotGraphPath $snapshotPath -CurrentCommit $newCommit
    Assert-True ($selection.SelectedPath -eq $snapshotPath) 'An enriched published graph must never replace an unenriched snapshot.'
    Assert-True ($selection.Reason -eq 'snapshot-unenriched-raw-enriched') 'The enriched/raw distinction must be explicit.'

    Write-TestGraph $snapshotPath $newCommit '2026-08-12T00:00:00Z' -Marker 'same-commit-old'
    Write-TestGraph $rawPath $newCommit '2026-08-12T02:00:00Z' -Marker 'same-commit-new'
    $selection = Resolve-UnderstandBaseGraph -RepositoryRoot $repositoryRoot -RawGraphPath $rawPath `
        -SnapshotGraphPath $snapshotPath -CurrentCommit $newCommit
    Assert-True ($selection.SelectedPath -eq $rawPath) 'For the same commit, the later analyzedAt timestamp must win.'
    Assert-True ($selection.Reason -eq 'raw-newer-analyzed-at') 'Same-commit freshness must be deterministic.'

    [IO.File]::WriteAllText($rawPath, '{"project":{},"nodes":[]}', [Text.UTF8Encoding]::new($false))
    $threw = $false
    try {
        $null = Resolve-UnderstandBaseGraph -RepositoryRoot $repositoryRoot -RawGraphPath $rawPath `
            -SnapshotGraphPath (Join-Path $uaRoot 'missing.json') -CurrentCommit $newCommit
    }
    catch { $threw = $_.Exception.Message.Contains('edges array') }
    Assert-True $threw 'Malformed graph candidates must fail closed.'

    Write-Output 'UnderstandGraphFreshness tests passed: 4.'
}
finally {
    $resolvedTemp = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
    $resolvedTestRoot = [IO.Path]::GetFullPath($testRoot)
    if ($resolvedTestRoot.StartsWith($resolvedTemp, [StringComparison]::OrdinalIgnoreCase) -and
        [IO.Path]::GetFileName($resolvedTestRoot).StartsWith('dreamine-understand-freshness-', [StringComparison]::Ordinal)) {
        [IO.Directory]::Delete($resolvedTestRoot, $true)
    }
}
