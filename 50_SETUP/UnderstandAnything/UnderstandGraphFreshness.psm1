Set-StrictMode -Version Latest

function Get-UnderstandGraphCandidate {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Path)

    $fullPath = [IO.Path]::GetFullPath($Path)
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        return [pscustomobject]@{ Path = $fullPath; Exists = $false }
    }

    $graph = [IO.File]::ReadAllText($fullPath) | ConvertFrom-Json
    $nodesProperty = $graph.PSObject.Properties['nodes']
    $edgesProperty = $graph.PSObject.Properties['edges']
    $projectProperty = $graph.PSObject.Properties['project']
    if ($null -eq $nodesProperty) { throw "Graph has no nodes array: $fullPath" }
    if ($null -eq $edgesProperty) { throw "Graph has no edges array: $fullPath" }
    if ($null -eq $projectProperty -or $null -eq $projectProperty.Value) { throw "Graph has no project object: $fullPath" }

    $project = $projectProperty.Value
    $commitProperty = $project.PSObject.Properties['gitCommitHash']
    $analyzedProperty = $project.PSObject.Properties['analyzedAt']
    $enrichmentProperty = $project.PSObject.Properties['enrichmentVersion']
    $gitCommitHash = if ($null -ne $commitProperty) { [string]$commitProperty.Value } else { $null }
    $analyzedAtText = if ($null -ne $analyzedProperty) { [string]$analyzedProperty.Value } else { $null }
    $enrichmentVersion = if ($null -ne $enrichmentProperty) { [string]$enrichmentProperty.Value } else { $null }
    $analyzedAt = [DateTimeOffset]::MinValue
    if (-not [string]::IsNullOrWhiteSpace($analyzedAtText)) {
        $parsed = [DateTimeOffset]::MinValue
        if ([DateTimeOffset]::TryParse($analyzedAtText, [ref]$parsed)) { $analyzedAt = $parsed }
    }
    return [pscustomobject]@{
        Path = $fullPath
        Exists = $true
        GitCommitHash = $gitCommitHash
        AnalyzedAt = $analyzedAt
        LastWriteTimeUtc = [IO.File]::GetLastWriteTimeUtc($fullPath)
        IsEnriched = -not [string]::IsNullOrWhiteSpace($enrichmentVersion)
        EnrichmentVersion = $enrichmentVersion
        NodeCount = @($nodesProperty.Value).Count
        EdgeCount = @($edgesProperty.Value).Count
    }
}

function Test-UnderstandGitAncestor {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$RepositoryRoot,
        [Parameter(Mandatory)][AllowNull()][AllowEmptyString()][string]$Ancestor,
        [Parameter(Mandatory)][AllowNull()][AllowEmptyString()][string]$Descendant
    )
    if ($Ancestor -notmatch '^[0-9a-fA-F]{40}$' -or $Descendant -notmatch '^[0-9a-fA-F]{40}$') { return $false }
    $null = & git -C $RepositoryRoot merge-base --is-ancestor $Ancestor $Descendant 2>$null
    return $LASTEXITCODE -eq 0
}

function Resolve-UnderstandBaseGraph {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$RepositoryRoot,
        [string]$RawGraphPath = '',
        [string]$SnapshotGraphPath = '',
        [string]$CurrentCommit = '',
        [switch]$RefreshSnapshot
    )

    $root = [IO.Path]::GetFullPath($RepositoryRoot)
    $uaRoot = [IO.Path]::GetFullPath((Join-Path $root '.ua'))
    if ([string]::IsNullOrWhiteSpace($RawGraphPath)) { $RawGraphPath = Join-Path $uaRoot 'knowledge-graph.json' }
    if ([string]::IsNullOrWhiteSpace($SnapshotGraphPath)) { $SnapshotGraphPath = Join-Path $uaRoot 'knowledge-graph.pre-enrichment.json' }
    $rawPath = [IO.Path]::GetFullPath($RawGraphPath)
    $snapshotPath = [IO.Path]::GetFullPath($SnapshotGraphPath)
    $uaBoundary = $uaRoot + [IO.Path]::DirectorySeparatorChar
    foreach ($candidatePath in @($rawPath, $snapshotPath)) {
        if (-not $candidatePath.StartsWith($uaBoundary, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Understand graph path escapes .ua: $candidatePath"
        }
    }

    $raw = Get-UnderstandGraphCandidate -Path $rawPath
    $snapshot = Get-UnderstandGraphCandidate -Path $snapshotPath
    if (-not $raw.Exists -and -not $snapshot.Exists) { throw 'Neither the raw graph nor the pre-enrichment snapshot exists.' }
    if ([string]::IsNullOrWhiteSpace($CurrentCommit)) {
        $headOutput = & git -C $root rev-parse HEAD 2>$null | Select-Object -First 1
        $CurrentCommit = if ($headOutput) { $headOutput.Trim() } else { '' }
    }

    $selected = $null
    $reason = ''
    if ($raw.Exists -and -not $raw.IsEnriched -and (-not $snapshot.Exists -or $snapshot.IsEnriched)) {
        $selected = $raw
        $reason = if ($snapshot.Exists) { 'raw-unenriched-snapshot-enriched' } else { 'raw-only' }
    }
    elseif ($snapshot.Exists -and -not $snapshot.IsEnriched -and (-not $raw.Exists -or $raw.IsEnriched)) {
        $selected = $snapshot
        $reason = if ($raw.Exists) { 'snapshot-unenriched-raw-enriched' } else { 'snapshot-only' }
    }
    elseif ($raw.Exists -and -not $raw.IsEnriched -and $snapshot.Exists -and -not $snapshot.IsEnriched) {
        if ($raw.GitCommitHash -eq $CurrentCommit -and $snapshot.GitCommitHash -ne $CurrentCommit) {
            $selected = $raw; $reason = 'raw-matches-head'
        }
        elseif ($snapshot.GitCommitHash -eq $CurrentCommit -and $raw.GitCommitHash -ne $CurrentCommit) {
            $selected = $snapshot; $reason = 'snapshot-matches-head'
        }
        elseif ($raw.GitCommitHash -ne $snapshot.GitCommitHash -and
            (Test-UnderstandGitAncestor -RepositoryRoot $root -Ancestor $snapshot.GitCommitHash -Descendant $raw.GitCommitHash)) {
            $selected = $raw; $reason = 'raw-descends-from-snapshot'
        }
        elseif ($raw.GitCommitHash -ne $snapshot.GitCommitHash -and
            (Test-UnderstandGitAncestor -RepositoryRoot $root -Ancestor $raw.GitCommitHash -Descendant $snapshot.GitCommitHash)) {
            $selected = $snapshot; $reason = 'snapshot-descends-from-raw'
        }
        elseif ($raw.AnalyzedAt -gt $snapshot.AnalyzedAt) {
            $selected = $raw; $reason = 'raw-newer-analyzed-at'
        }
        elseif ($snapshot.AnalyzedAt -gt $raw.AnalyzedAt) {
            $selected = $snapshot; $reason = 'snapshot-newer-analyzed-at'
        }
        elseif ($raw.LastWriteTimeUtc -gt $snapshot.LastWriteTimeUtc) {
            $selected = $raw; $reason = 'raw-newer-mtime'
        }
        else {
            $selected = $snapshot; $reason = 'snapshot-stable-tie-break'
        }
    }
    else {
        throw 'No valid unenriched Understand graph is available. Run a full graph rebuild.'
    }

    $refreshed = $false
    if ($RefreshSnapshot -and $selected.Path -eq $rawPath) {
        [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($snapshotPath)) | Out-Null
        $temporaryPath = "$snapshotPath.tmp-$PID-$([Guid]::NewGuid().ToString('N'))"
        $backupPath = "$snapshotPath.backup-$PID-$([Guid]::NewGuid().ToString('N'))"
        try {
            [IO.File]::Copy($rawPath, $temporaryPath, $true)
            if (Test-Path -LiteralPath $snapshotPath -PathType Leaf) {
                [IO.File]::Replace($temporaryPath, $snapshotPath, $backupPath, $true)
            }
            else {
                [IO.File]::Move($temporaryPath, $snapshotPath)
            }
            $refreshed = $true
        }
        finally {
            if (Test-Path -LiteralPath $temporaryPath -PathType Leaf) { [IO.File]::Delete($temporaryPath) }
            if (Test-Path -LiteralPath $backupPath -PathType Leaf) { [IO.File]::Delete($backupPath) }
        }
    }

    return [pscustomobject]@{
        SelectedPath = $selected.Path
        Reason = $reason
        RefreshedSnapshot = $refreshed
        GitCommitHash = $selected.GitCommitHash
        AnalyzedAt = $selected.AnalyzedAt
        NodeCount = $selected.NodeCount
        EdgeCount = $selected.EdgeCount
    }
}

Export-ModuleMember -Function Resolve-UnderstandBaseGraph
