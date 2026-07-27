[CmdletBinding()]
param(
    [ValidateSet('Audit', 'Apply')]
    [string]$Mode = 'Audit',

    [string]$Organization = 'codemaru-dreamine',

    [string]$GitHubOrganization = 'CodeMaru-Dreamine',

    [string]$ProjectKeyPrefix = 'CodeMaru-Dreamine_',

    [switch]$SkipNewCodeDefinition
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

# Windows PowerShell 5.1 can otherwise negotiate an obsolete TLS version.
if ($PSVersionTable.PSEdition -eq 'Desktop') {
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
}

$script:SonarCloudBaseUri = 'https://sonarcloud.io'
$script:SonarCloudApiBaseUri = 'https://api.sonarcloud.io'
$script:SonarToken = $null

function ConvertFrom-SecureStringToPlainText {
    param(
        [Parameter(Mandatory)]
        [Security.SecureString]$SecureValue
    )

    $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecureValue)
    try {
        return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)
    }
}

function Get-SonarToken {
    if (-not [string]::IsNullOrWhiteSpace($env:SONAR_TOKEN)) {
        return $env:SONAR_TOKEN
    }

    $secureToken = Read-Host 'SonarCloud token (My Account > Security)' -AsSecureString
    $plainToken = ConvertFrom-SecureStringToPlainText -SecureValue $secureToken
    if ([string]::IsNullOrWhiteSpace($plainToken)) {
        throw 'SonarCloud token is required for Apply mode.'
    }

    return $plainToken
}

function Get-SonarHeaders {
    $headers = @{
        Accept = 'application/json'
    }

    if (-not [string]::IsNullOrWhiteSpace($script:SonarToken)) {
        $headers.Authorization = "Bearer $($script:SonarToken)"
    }

    return $headers
}

function Get-HttpStatusCode {
    param(
        [Parameter(Mandatory)]
        [System.Management.Automation.ErrorRecord]$ErrorRecord
    )

    if ($null -ne $ErrorRecord.Exception.Response) {
        $statusCode = $ErrorRecord.Exception.Response.StatusCode
        if ($null -ne $statusCode) {
            return [int]$statusCode
        }
    }

    return 0
}

function Invoke-SonarGet {
    param(
        [Parameter(Mandatory)]
        [string]$Uri
    )

    return Invoke-RestMethod `
        -Method Get `
        -Uri $Uri `
        -Headers (Get-SonarHeaders)
}

function Invoke-SonarFormPost {
    param(
        [Parameter(Mandatory)]
        [string]$Uri,

        [Parameter(Mandatory)]
        [hashtable]$Body
    )

    return Invoke-RestMethod `
        -Method Post `
        -Uri $Uri `
        -Headers (Get-SonarHeaders) `
        -ContentType 'application/x-www-form-urlencoded' `
        -Body $Body
}

function Invoke-SonarJsonPost {
    param(
        [Parameter(Mandatory)]
        [string]$Uri,

        [Parameter(Mandatory)]
        [hashtable]$Body
    )

    return Invoke-RestMethod `
        -Method Post `
        -Uri $Uri `
        -Headers (Get-SonarHeaders) `
        -ContentType 'application/json' `
        -Body ($Body | ConvertTo-Json -Compress)
}

function Get-RepositoryRoot {
    $root = Resolve-Path (Join-Path $PSScriptRoot '..\..')
    if (-not (Test-Path (Join-Path $root '.gitmodules'))) {
        throw "Could not find .gitmodules below repository root '$root'."
    }

    return $root.Path
}

function Get-TargetRepositoryNames {
    param(
        [Parameter(Mandatory)]
        [string]$RepositoryRoot
    )

    $gitmodulesPath = Join-Path $RepositoryRoot '.gitmodules'
    $repositoryNames = foreach ($line in Get-Content -LiteralPath $gitmodulesPath) {
        if ($line -match '^\s*url\s*=\s*https://github\.com/[^/]+/(?<name>[^/\s]+?)(?:\.git)?\s*$') {
            $Matches.name
        }
    }

    @(
        $repositoryNames
        'Dreamine.Communication.FullKit'
        'Dreamine.MVVM.FullKit'
    ) |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        Sort-Object -Unique
}

function Get-SonarProjects {
    $encodedOrganization = [Uri]::EscapeDataString($Organization)
    $uri = "$script:SonarCloudBaseUri/api/components/search?organization=$encodedOrganization&qualifiers=TRK&ps=500"
    $response = Invoke-SonarGet -Uri $uri

    $projects = @{}
    foreach ($component in @($response.components)) {
        $projects[$component.key] = $component
    }

    return $projects
}

function Get-ProjectAnalysisState {
    param(
        [Parameter(Mandatory)]
        [string]$ProjectKey
    )

    $encodedKey = [Uri]::EscapeDataString($ProjectKey)
    $uri = "$script:SonarCloudBaseUri/api/project_analyses/search?project=$encodedKey&ps=1"
    $response = Invoke-SonarGet -Uri $uri
    if (@($response.analyses).Count -gt 0) {
        return 'Analyzed'
    }

    return 'Registered-NoAnalysis'
}

function Get-SonarProjectId {
    param(
        [Parameter(Mandatory)]
        [string]$ProjectKey
    )

    $encodedKey = [Uri]::EscapeDataString($ProjectKey)
    $encodedOrganization = [Uri]::EscapeDataString($Organization)
    $uri = "$script:SonarCloudBaseUri/api/navigation/component?component=$encodedKey&organization=$encodedOrganization"
    $response = Invoke-SonarGet -Uri $uri

    $componentProperty = $response.PSObject.Properties['component']
    if ($null -ne $componentProperty -and $null -ne $componentProperty.Value) {
        $componentIdProperty = $componentProperty.Value.PSObject.Properties['id']
        if (
            $null -ne $componentIdProperty -and
            -not [string]::IsNullOrWhiteSpace([string]$componentIdProperty.Value)
        ) {
            return [string]$componentIdProperty.Value
        }
    }

    $idProperty = $response.PSObject.Properties['id']
    if (
        $null -ne $idProperty -and
        -not [string]::IsNullOrWhiteSpace([string]$idProperty.Value)
    ) {
        return [string]$idProperty.Value
    }

    throw "SonarCloud project id was not returned for '$ProjectKey'."
}

function Get-ProjectBinding {
    param(
        [Parameter(Mandatory)]
        [string]$ProjectId
    )

    $encodedId = [Uri]::EscapeDataString($ProjectId)
    $uri = "$script:SonarCloudApiBaseUri/dop-translation/project-bindings?projectId=$encodedId"

    try {
        return Invoke-SonarGet -Uri $uri
    }
    catch {
        $statusCode = Get-HttpStatusCode -ErrorRecord $_
        if ($statusCode -eq 404) {
            return $null
        }

        throw
    }
}

function Get-GitHubRepositories {
    $repositories = @{}
    $encodedOwner = [Uri]::EscapeDataString($GitHubOrganization)
    $headers = @{
        Accept = 'application/vnd.github+json'
        'User-Agent' = 'Dreamine-SonarCloud-Sync'
        'X-GitHub-Api-Version' = '2022-11-28'
    }

    $owner = Invoke-RestMethod `
        -Method Get `
        -Uri "https://api.github.com/users/$encodedOwner" `
        -Headers $headers

    $isOrganization = $owner.type -eq 'Organization'
    if ($isOrganization) {
        Write-Host "GitHub owner type: Organization ($GitHubOrganization)"
    }
    else {
        Write-Host "GitHub owner type: User ($GitHubOrganization)"
    }

    $gh = Get-Command gh -ErrorAction SilentlyContinue
    if ($null -ne $gh) {
        & $gh.Source auth status -h github.com 2>$null | Out-Null
        if ($LASTEXITCODE -eq 0) {
            $authenticatedLogin = (& $gh.Source api user --jq '.login').Trim()
            if ($LASTEXITCODE -ne 0) {
                throw 'GitHub CLI is authenticated, but the current GitHub user could not be read.'
            }

            if (-not $isOrganization -and $authenticatedLogin -ne $GitHubOrganization) {
                throw (
                    "GitHub CLI is logged in as '$authenticatedLogin', but the repository owner is " +
                    "'$GitHubOrganization'. Run: gh auth login -h github.com -p https -w"
                )
            }

            Write-Host "GitHub API authentication: gh ($authenticatedLogin)"
            if ($isOrganization) {
                $endpoint = "orgs/$encodedOwner/repos?type=all&per_page=100"
            }
            else {
                $endpoint = 'user/repos?visibility=all&affiliation=owner&per_page=100'
            }

            $json = (& $gh.Source api --paginate --slurp $endpoint) -join [Environment]::NewLine
            if ($LASTEXITCODE -ne 0) {
                throw "GitHub CLI could not list repositories for '$GitHubOrganization'."
            }

            $pages = ConvertFrom-Json -InputObject $json -NoEnumerate
            foreach ($pageItems in $pages) {
                foreach ($item in @($pageItems)) {
                    if ($isOrganization -or $item.owner.login -eq $GitHubOrganization) {
                        $repositories[$item.name] = $item
                    }
                }
            }

            if ($repositories.Count -eq 0) {
                throw "GitHub CLI returned no repositories owned by '$GitHubOrganization'."
            }

            Write-Host "GitHub repositories visible: $($repositories.Count)"
            return $repositories
        }
    }

    Write-Warning (
        'GitHub CLI is not authenticated. Only public repositories can be discovered. ' +
        'For private repositories run: gh auth login -h github.com -p https -w'
    )

    $page = 1
    while ($true) {
        if ($isOrganization) {
            $uri = "https://api.github.com/orgs/$encodedOwner/repos?type=all&per_page=100&page=$page"
        }
        else {
            $uri = "https://api.github.com/users/$encodedOwner/repos?type=owner&per_page=100&page=$page"
        }

        $items = @(
            Invoke-RestMethod `
                -Method Get `
                -Uri $uri `
                -Headers $headers
        )

        foreach ($item in $items) {
            $repositories[$item.name] = $item
        }

        if ($items.Count -lt 100) {
            break
        }

        $page++
    }

    if ($repositories.Count -eq 0) {
        throw (
            "No public GitHub repositories were found for '$GitHubOrganization'. " +
            'Authenticate GitHub CLI first: gh auth login -h github.com -p https -w'
        )
    }

    return $repositories
}

function New-SonarProject {
    param(
        [Parameter(Mandatory)]
        [string]$RepositoryName,

        [Parameter(Mandatory)]
        [string]$ProjectKey
    )

    Write-Host "  CREATE  $RepositoryName" -ForegroundColor Yellow
    $null = Invoke-SonarFormPost `
        -Uri "$script:SonarCloudBaseUri/api/projects/create" `
        -Body @{
            organization = $Organization
            project = $ProjectKey
            name = $RepositoryName
            visibility = 'public'
        }
}

function Connect-SonarProjectToGitHub {
    param(
        [Parameter(Mandatory)]
        [string]$ProjectKey,

        [Parameter(Mandatory)]
        [string]$GitHubRepositoryId
    )

    $projectId = Get-SonarProjectId -ProjectKey $ProjectKey
    $binding = Get-ProjectBinding -ProjectId $projectId
    if ($null -ne $binding) {
        Write-Host '  BIND    already connected' -ForegroundColor DarkGray
        return
    }

    Write-Host '  BIND    connect GitHub repository' -ForegroundColor Yellow
    $null = Invoke-SonarJsonPost `
        -Uri "$script:SonarCloudApiBaseUri/dop-translation/project-bindings" `
        -Body @{
            projectId = $projectId
            repositoryId = $GitHubRepositoryId
        }
}

function Set-PreviousVersionNewCodeDefinition {
    param(
        [Parameter(Mandatory)]
        [string]$ProjectKey
    )

    if ($SkipNewCodeDefinition) {
        return
    }

    Write-Host '  NEWCODE Previous Version' -ForegroundColor Yellow
    foreach ($setting in @(
        @{ key = 'sonar.leak.period'; value = 'previous_version' },
        @{ key = 'sonar.leak.period.type'; value = 'previous_version' }
    )) {
        $null = Invoke-SonarFormPost `
            -Uri "$script:SonarCloudBaseUri/api/settings/set" `
            -Body @{
                component = $ProjectKey
                key = $setting.key
                value = $setting.value
            }
    }
}

function Get-AuditRows {
    param(
        [Parameter(Mandatory)]
        [string[]]$RepositoryNames,

        [Parameter(Mandatory)]
        [hashtable]$SonarProjects
    )

    foreach ($repositoryName in $RepositoryNames) {
        $projectKey = "$ProjectKeyPrefix$repositoryName"
        if (-not $SonarProjects.ContainsKey($projectKey)) {
            [pscustomobject]@{
                Repository = $repositoryName
                ProjectKey = $projectKey
                State = 'Missing'
            }
            continue
        }

        [pscustomobject]@{
            Repository = $repositoryName
            ProjectKey = $projectKey
            State = Get-ProjectAnalysisState -ProjectKey $projectKey
        }
    }
}

function Write-AuditReport {
    param(
        [Parameter(Mandatory)]
        [object[]]$Rows
    )

    $missing = @($Rows | Where-Object State -eq 'Missing')
    $notAnalyzed = @($Rows | Where-Object State -eq 'Registered-NoAnalysis')
    $analyzed = @($Rows | Where-Object State -eq 'Analyzed')

    Write-Host ''
    Write-Host 'SonarCloud project audit' -ForegroundColor Cyan
    Write-Host "  Analyzed:               $($analyzed.Count)" -ForegroundColor Green
    Write-Host "  Registered, no analysis: $($notAnalyzed.Count)" -ForegroundColor Yellow
    Write-Host "  Missing:                 $($missing.Count)" -ForegroundColor Red
    Write-Host ''

    $Rows |
        Sort-Object State, Repository |
        Format-Table Repository, State, ProjectKey -AutoSize
}

$repositoryRoot = Get-RepositoryRoot
$repositoryNames = @(Get-TargetRepositoryNames -RepositoryRoot $repositoryRoot)

Write-Host "Mode: $Mode"
Write-Host "Organization: $Organization"
Write-Host "Target repositories: $($repositoryNames.Count)"

try {
    if ($Mode -eq 'Apply') {
        $script:SonarToken = Get-SonarToken
    }

    $sonarProjects = Get-SonarProjects
    $auditRows = @(Get-AuditRows -RepositoryNames $repositoryNames -SonarProjects $sonarProjects)
    Write-AuditReport -Rows $auditRows

    if ($Mode -eq 'Audit') {
        return
    }

    $githubRepositories = Get-GitHubRepositories
    foreach ($repositoryName in $repositoryNames) {
        if (-not $githubRepositories.ContainsKey($repositoryName)) {
            Write-Warning "GitHub repository '$GitHubOrganization/$repositoryName' was not found; skipped."
            continue
        }

        $projectKey = "$ProjectKeyPrefix$repositoryName"
        Write-Host ''
        Write-Host "[$repositoryName]" -ForegroundColor Cyan

        if (-not $sonarProjects.ContainsKey($projectKey)) {
            New-SonarProject -RepositoryName $repositoryName -ProjectKey $projectKey
            $sonarProjects[$projectKey] = [pscustomobject]@{ key = $projectKey }
        }
        else {
            Write-Host '  CREATE  already registered' -ForegroundColor DarkGray
        }

        Connect-SonarProjectToGitHub `
            -ProjectKey $projectKey `
            -GitHubRepositoryId ([string]$githubRepositories[$repositoryName].id)

        Set-PreviousVersionNewCodeDefinition -ProjectKey $projectKey
    }

    Write-Host ''
    Write-Host 'Apply completed. Refreshing the SonarCloud audit...' -ForegroundColor Green
    $sonarProjects = Get-SonarProjects
    $auditRows = @(Get-AuditRows -RepositoryNames $repositoryNames -SonarProjects $sonarProjects)
    Write-AuditReport -Rows $auditRows

    Write-Warning (
        'Project creation/binding does not disable SonarCloud Automatic Analysis. ' +
        'Before enabling the CI scanner, turn it off at Administration > Analysis Method.'
    )
}
catch {
    if ($_.Exception.Message -match 'Authentication failed|SSL|TLS|secure channel') {
        Write-Warning (
            'Could not establish the TLS connection to SonarCloud. ' +
            'Run this script in a normal local PowerShell session outside a restricted sandbox, ' +
            'and verify that https://sonarcloud.io opens on this PC.'
        )
    }

    throw
}
finally {
    $script:SonarToken = $null
}
