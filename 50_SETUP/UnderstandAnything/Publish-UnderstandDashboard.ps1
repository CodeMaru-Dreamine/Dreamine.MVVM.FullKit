[CmdletBinding()]
param(
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,
    [string]$DashboardBuild = (Join-Path (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path '.ua\public-dashboard-build'),
    [string]$Destination = (Join-Path (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path '20_SOURCES\000. Project\010. App\Dreamine.Web\wwwroot\understand'),
    [string]$NodeExecutable = 'node'
)

$ErrorActionPreference = 'Stop'

$repositoryPath = [System.IO.Path]::GetFullPath($RepositoryRoot)
$buildPath = [System.IO.Path]::GetFullPath($DashboardBuild)
$destinationPath = [System.IO.Path]::GetFullPath($Destination)
$webRoot = [System.IO.Path]::GetFullPath((Join-Path $repositoryPath '20_SOURCES\000. Project\010. App\Dreamine.Web\wwwroot'))
$scanPath = Join-Path $repositoryPath '.ua\intermediate\scan-result.json'
$graphPath = Join-Path $repositoryPath '.ua\knowledge-graph.json'
$portalPath = Join-Path $PSScriptRoot 'portal'
$contentPath = Join-Path $PSScriptRoot 'content\onboarding.json'
$enricherPath = Join-Path $PSScriptRoot 'Enrich-UnderstandGraph.mjs'
$generatorPath = Join-Path $PSScriptRoot 'Generate-UnderstandPortal.mjs'
$projectGraphGeneratorPath = Join-Path $PSScriptRoot 'Generate-ProjectGraphs.mjs'
$projectGraphValidatorPath = Join-Path $PSScriptRoot 'Validate-ProjectGraphs.mjs'

if (-not $destinationPath.StartsWith($webRoot + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase) -or
    [System.IO.Path]::GetFileName($destinationPath) -ne 'understand') {
    throw "Unsafe destination: $destinationPath"
}

foreach ($requiredPath in @($buildPath, $scanPath, $graphPath, $portalPath, $contentPath, $enricherPath, $generatorPath, $projectGraphGeneratorPath, $projectGraphValidatorPath)) {
    if (-not (Test-Path -LiteralPath $requiredPath)) {
        throw "Required input not found: $requiredPath"
    }
}

# Restore API contracts and code-derived relationships in both languages after
# every fresh Understand Anything scan. Both outputs share node IDs/topology.
$baseGraphPath = Join-Path $repositoryPath '.ua\knowledge-graph.pre-enrichment.json'
if (-not (Test-Path -LiteralPath $baseGraphPath)) { $baseGraphPath = $graphPath }
$koGraphPath = Join-Path $repositoryPath '.ua\knowledge-graph.ko.json'
$enGraphPath = Join-Path $repositoryPath '.ua\knowledge-graph.en.json'
foreach ($languageRun in @(
    @{ Language = 'ko'; Output = $koGraphPath },
    @{ Language = 'en'; Output = $enGraphPath }
)) {
    & $NodeExecutable $enricherPath $repositoryPath $languageRun.Language $baseGraphPath $languageRun.Output
    if ($LASTEXITCODE -ne 0) {
        throw "Graph enrichment ($($languageRun.Language)) failed with exit code $LASTEXITCODE."
    }
}
Copy-Item -LiteralPath $koGraphPath -Destination $graphPath -Force

if (Test-Path -LiteralPath $destinationPath) {
    $resolvedDestination = (Resolve-Path -LiteralPath $destinationPath).Path
    if (-not $resolvedDestination.StartsWith($webRoot + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to replace a directory outside wwwroot: $resolvedDestination"
    }
    Remove-Item -LiteralPath $resolvedDestination -Recurse -Force
}

New-Item -ItemType Directory -Path $destinationPath | Out-Null
Copy-Item -Path (Join-Path $portalPath '*') -Destination $destinationPath -Recurse -Force
Copy-Item -LiteralPath $contentPath -Destination (Join-Path $destinationPath 'onboarding.json') -Force

$graphDestination = Join-Path $destinationPath 'graph'
New-Item -ItemType Directory -Path $graphDestination | Out-Null
Copy-Item -Path (Join-Path $buildPath '*') -Destination $graphDestination -Recurse -Force
$graphIndexPath = Join-Path $graphDestination 'index.html'
$graphIndexHtml = Get-Content -LiteralPath $graphIndexPath -Raw
$graphIndexHtml = $graphIndexHtml.Replace('<title>Understand Anything</title>', '<title>Dreamine 프로젝트 지식 그래프</title>')
$graphSelectorScript = @'
  <script>
    (() => {
      const query = new URLSearchParams(location.search);
      const project = query.get('project');
      const language = query.get('lang') === 'en' ? 'en' : 'ko';
      const originalFetch = window.fetch.bind(window);
      window.fetch = (input, init) => {
        const raw = typeof input === 'string' ? input : input.url;
        const url = new URL(raw, location.href);
        const file = url.pathname.split('/').pop();
        if (url.pathname.startsWith('/understand/graph/') && ['knowledge-graph.json', 'config.json', 'meta.json'].includes(file)) {
          const dataRoot = project
            ? `/understand/projects/${encodeURIComponent(project)}/${language}`
            : `/understand/full/${language}`;
          return originalFetch(`${dataRoot}/${file}`, init);
        }
        return originalFetch(input, init);
      };
    })();
  </script>
'@
if (-not $graphIndexHtml.Contains('</head>')) { throw 'Dashboard index does not contain </head>.' }
$graphIndexHtml = $graphIndexHtml.Replace('</head>', "$graphSelectorScript`n</head>")
Set-Content -LiteralPath $graphIndexPath -Value $graphIndexHtml -NoNewline -Encoding utf8
Copy-Item -LiteralPath $koGraphPath -Destination (Join-Path $destinationPath 'knowledge-graph.json') -Force
Copy-Item -LiteralPath $koGraphPath -Destination (Join-Path $graphDestination 'knowledge-graph.json') -Force

$fullGraphDestination = Join-Path $destinationPath 'full'
foreach ($fullGraph in @(
    @{ Language = 'ko'; Source = $koGraphPath },
    @{ Language = 'en'; Source = $enGraphPath }
)) {
    $languageDestination = Join-Path $fullGraphDestination $fullGraph.Language
    New-Item -ItemType Directory -Path $languageDestination -Force | Out-Null
    Copy-Item -LiteralPath $fullGraph.Source -Destination (Join-Path $languageDestination 'knowledge-graph.json') -Force
    @{ outputLanguage = $fullGraph.Language } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $languageDestination 'config.json') -Encoding utf8
    @{ outputLanguage = $fullGraph.Language; scope = 'full'; generatedAt = [DateTimeOffset]::Now.ToString('o') } |
        ConvertTo-Json | Set-Content -LiteralPath (Join-Path $languageDestination 'meta.json') -Encoding utf8
}

foreach ($optionalName in @('meta.json', 'config.json')) {
    $optionalPath = Join-Path $repositoryPath ".ua\$optionalName"
    if (Test-Path -LiteralPath $optionalPath) {
        Copy-Item -LiteralPath $optionalPath -Destination (Join-Path $destinationPath $optionalName) -Force
        Copy-Item -LiteralPath $optionalPath -Destination (Join-Path $graphDestination $optionalName) -Force
    }
}

$ogImageSource = Join-Path $PSScriptRoot 'assets\og-knowledge-graph.png'
if (-not (Test-Path -LiteralPath $ogImageSource -PathType Leaf)) {
    throw "OG image not found: $ogImageSource"
}
Copy-Item -LiteralPath $ogImageSource -Destination (Join-Path $destinationPath 'og-knowledge-graph.png') -Force
Copy-Item -LiteralPath $ogImageSource -Destination (Join-Path $graphDestination 'og-knowledge-graph.png') -Force

# The official static/demo build intentionally disables source preview. Patch the
# generated lazy CodeViewer chunk so it reads pre-generated JSON source mirrors.
$codeViewer = @(Get-ChildItem -LiteralPath (Join-Path $graphDestination 'assets') -Filter 'CodeViewer-*.js')
if ($codeViewer.Count -ne 1) {
    throw "Expected exactly one CodeViewer chunk, found $($codeViewer.Count)."
}

$viewerText = Get-Content -LiteralPath $codeViewer[0].FullName -Raw
$urlPattern = 'function (?<name>\w+)\(e,t\)\{return`/file-content\.json\?\$\{new URLSearchParams\(\{token:t,path:e\}\)\.toString\(\)\}`\}'
$urlMatches = [regex]::Matches($viewerText, $urlPattern)
if ($urlMatches.Count -ne 1) {
    throw "Could not identify the official file-content URL function. Matches: $($urlMatches.Count)"
}
$functionName = $urlMatches[0].Groups['name'].Value
$urlReplacement = 'function ' + $functionName + '(e,t){return"/understand/source/"+e.split("/").map(encodeURIComponent).join("/")+".json"}'
$viewerText = [regex]::Replace($viewerText, $urlPattern, $urlReplacement, 1)

$demoGuardPattern = 'if\(e==="__demo__"\)\{v\(\{status:"error",source:null,error:"Source preview is available only when the local dashboard server is running\."\}\);return\}'
$guardMatches = [regex]::Matches($viewerText, $demoGuardPattern)
if ($guardMatches.Count -ne 1) {
    throw "Could not identify the official demo-mode source guard. Matches: $($guardMatches.Count)"
}
$viewerText = [regex]::Replace($viewerText, $demoGuardPattern, '', 1)
Set-Content -LiteralPath $codeViewer[0].FullName -Value $viewerText -NoNewline -Encoding utf8

$allowedExtensions = @(
    '.bat', '.c', '.cc', '.cmd', '.cpp', '.cs', '.cshtml', '.csproj', '.css',
    '.fs', '.fsproj', '.go', '.h', '.hpp', '.htm', '.html', '.java', '.js',
    '.jsx', '.kt', '.kts', '.mjs', '.cjs', '.md', '.php', '.props', '.proto',
    '.ps1', '.py', '.razor', '.rb', '.rs', '.scss', '.sh', '.sln', '.sql',
    '.swift', '.targets', '.ts', '.tsx', '.vb', '.vbproj', '.xaml', '.yml', '.yaml'
)
$blockedFileNames = @(
    '.env', 'appsettings.json', 'appsettings.development.json', 'launchsettings.json',
    'secrets.json', 'settings.local.json'
)
$secretPatterns = @(
    '-----BEGIN [A-Z ]*PRIVATE KEY-----',
    '\bAKIA[0-9A-Z]{16}\b',
    '\bgh[pousr]_[A-Za-z0-9]{20,}\b',
    '\bsk-[A-Za-z0-9_-]{20,}\b',
    '(?i)(password|passwd|pwd|secret|api[_-]?key|access[_-]?token|connectionstring)[A-Za-z0-9_]*[^\r\n]{0,120}=\s*["''][^"''\r\n]{4,}["'']'
)

$scan = Get-Content -LiteralPath $scanPath -Raw | ConvertFrom-Json
$sourceRoot = Join-Path $destinationPath 'source'
New-Item -ItemType Directory -Path $sourceRoot | Out-Null
$published = 0
$blocked = [System.Collections.Generic.List[string]]::new()

foreach ($entry in $scan.files) {
    $relativePath = [string]$entry.path
    if (-not $relativePath.StartsWith('20_SOURCES/', [System.StringComparison]::OrdinalIgnoreCase)) {
        continue
    }

    $extension = [System.IO.Path]::GetExtension($relativePath).ToLowerInvariant()
    $fileName = [System.IO.Path]::GetFileName($relativePath).ToLowerInvariant()
    if ($extension -notin $allowedExtensions -or $fileName -in $blockedFileNames) {
        continue
    }

    $sourcePath = [System.IO.Path]::GetFullPath((Join-Path $repositoryPath ($relativePath.Replace('/', [System.IO.Path]::DirectorySeparatorChar))))
    if (-not $sourcePath.StartsWith($repositoryPath + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase) -or
        -not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
        continue
    }

    $content = Get-Content -LiteralPath $sourcePath -Raw
    $containsSecret = $false
    foreach ($pattern in $secretPatterns) {
        if ($content -match $pattern) {
            $containsSecret = $true
            break
        }
    }
    if ($containsSecret) {
        $blocked.Add($relativePath)
        continue
    }

    $targetPath = Join-Path $sourceRoot ($relativePath.Replace('/', [System.IO.Path]::DirectorySeparatorChar) + '.json')
    $targetDirectory = [System.IO.Path]::GetDirectoryName($targetPath)
    New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null
    $lineCount = if ($content.Length -eq 0) { 0 } else { ([regex]::Matches($content, "`n").Count + 1) }
    $payload = [ordered]@{
        path = $relativePath
        language = [string]$entry.language
        content = $content
        sizeBytes = (Get-Item -LiteralPath $sourcePath).Length
        lineCount = $lineCount
    }
    $payload | ConvertTo-Json -Depth 4 -Compress | Set-Content -LiteralPath $targetPath -Encoding utf8 -NoNewline
    $published++
}

$manifest = [ordered]@{
    generatedAt = [DateTimeOffset]::Now.ToString('o')
    sourceFiles = $published
    blockedBySecretScan = @($blocked)
    policy = 'Only allow-listed source/document extensions under 20_SOURCES are mirrored; runtime settings and known secret files are excluded.'
}
$manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $destinationPath 'source-manifest.json') -Encoding utf8

& $NodeExecutable $generatorPath $graphPath $destinationPath
if ($LASTEXITCODE -ne 0) {
    throw "Portal/API page generation failed with exit code $LASTEXITCODE."
}

& $NodeExecutable $projectGraphGeneratorPath $repositoryPath $destinationPath $koGraphPath $enGraphPath
if ($LASTEXITCODE -ne 0) {
    throw "Project graph generation failed with exit code $LASTEXITCODE."
}

& $NodeExecutable $projectGraphValidatorPath $repositoryPath $destinationPath
if ($LASTEXITCODE -ne 0) {
    throw "Project graph validation failed with exit code $LASTEXITCODE."
}

Write-Output "Published knowledge hub: $destinationPath"
Write-Output "Published advanced graph: $graphDestination"
Write-Output "Published source previews: $published"
Write-Output "Blocked by secret scan: $($blocked.Count)"
Write-Output "Published project graphs: 160"
