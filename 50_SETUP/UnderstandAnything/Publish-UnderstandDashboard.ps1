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
$sourceMirrorGeneratorPath = Join-Path $PSScriptRoot 'Generate-SourceMirrors.mjs'
$ontologyOverlayPublisherPath = Join-Path $repositoryPath '50_SETUP\Ontology\Apply-OntologyOverlay.mjs'
$ontologySourcePath = Join-Path $repositoryPath '.ua\ontology'
$ontologyArtifactNames = @(
    'manifest.json',
    'instances.json',
    'dreamine.schema.json',
    'architecture-validation.json',
    'linkml-shacl-validation.json',
    'source-audit.json'
)
$ontologyArtifactPaths = @($ontologyArtifactNames | ForEach-Object { Join-Path $ontologySourcePath $_ })

if (-not $destinationPath.StartsWith($webRoot + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase) -or
    [System.IO.Path]::GetFileName($destinationPath) -ne 'understand') {
    throw "Unsafe destination: $destinationPath"
}

foreach ($requiredPath in @($buildPath, $scanPath, $graphPath, $portalPath, $contentPath, $enricherPath, $generatorPath, $projectGraphGeneratorPath, $projectGraphValidatorPath, $ontologyOverlayPublisherPath) + $ontologyArtifactPaths) {
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
& $NodeExecutable $ontologyOverlayPublisherPath $repositoryPath
if ($LASTEXITCODE -ne 0) {
    throw "Source-verified ontology overlay publishing failed with exit code $LASTEXITCODE."
}
$publishedKoGraphPath = Join-Path $repositoryPath '.ua\knowledge-graph.source-verified.ko.json'
$publishedEnGraphPath = Join-Path $repositoryPath '.ua\knowledge-graph.source-verified.en.json'
foreach ($verifiedGraphPath in @($publishedKoGraphPath, $publishedEnGraphPath)) {
    if (-not (Test-Path -LiteralPath $verifiedGraphPath)) { throw "Verified graph not found: $verifiedGraphPath" }
}

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

# Publish only the bounded server-consumer artifacts. The raw Understand graph
# and source-verified overlay remain unchanged; Dreamine.Web reads these files
# server-side and never ships the complete RDF dataset to the browser.
$ontologyDestination = Join-Path $destinationPath 'ontology'
New-Item -ItemType Directory -Path $ontologyDestination -Force | Out-Null
foreach ($artifactPath in $ontologyArtifactPaths) {
    Copy-Item -LiteralPath $artifactPath -Destination (Join-Path $ontologyDestination ([System.IO.Path]::GetFileName($artifactPath))) -Force
}
$optionalConsumerPrecedence = Join-Path $ontologySourcePath 'consumer-precedence.json'
if (Test-Path -LiteralPath $optionalConsumerPrecedence -PathType Leaf) {
    Copy-Item -LiteralPath $optionalConsumerPrecedence -Destination (Join-Path $ontologyDestination 'consumer-precedence.json') -Force
}

$graphDestination = Join-Path $destinationPath 'graph'
New-Item -ItemType Directory -Path $graphDestination | Out-Null
Copy-Item -Path (Join-Path $buildPath '*') -Destination $graphDestination -Recurse -Force
$graphIndexPath = Join-Path $graphDestination 'index.html'
$graphIndexHtml = Get-Content -LiteralPath $graphIndexPath -Raw
$graphIndexHtml = $graphIndexHtml.Replace('<title>Understand Anything</title>', '<title>Dreamine 프로젝트 지식 그래프</title>')
$graphSelectorScript = @'
  <style>
    #dreamine-docs-return {
      display: inline-flex;
      align-items: center;
      flex: 0 0 auto;
      position: fixed;
      left: 52px;
      bottom: 14px;
      z-index: 2147483647;
      min-height: 38px;
      padding: 0 14px;
      margin: 0;
      border: 1px solid rgba(126, 240, 197, .45);
      border-radius: 10px;
      color: #dffcf1;
      background: rgba(7, 16, 27, .92);
      box-shadow: 0 8px 28px rgba(0, 0, 0, .28);
      font: 600 13px/1.2 system-ui, sans-serif;
      text-decoration: none;
    }
    #dreamine-docs-return:hover { border-color: #7ef0c5; background: #102a2b; }
    @media (max-width: 720px) {
      #dreamine-docs-return { left: 46px; bottom: 10px; min-height: 34px; padding-inline: 10px; }
    }
  </style>
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
      window.addEventListener('DOMContentLoaded', () => {
        const returnLink = document.createElement('a');
        returnLink.id = 'dreamine-docs-return';
        returnLink.href = '/knowledge';
        returnLink.textContent = language === 'en' ? '← Dreamine Docs Hub' : '← Dreamine 문서 허브';
        returnLink.setAttribute('aria-label', returnLink.textContent);
        document.body.appendChild(returnLink);
      });
    })();
  </script>
'@
if (-not $graphIndexHtml.Contains('</head>')) { throw 'Dashboard index does not contain </head>.' }
$graphIndexHtml = $graphIndexHtml.Replace('</head>', "$graphSelectorScript`n</head>")
Set-Content -LiteralPath $graphIndexPath -Value $graphIndexHtml -NoNewline -Encoding utf8
Copy-Item -LiteralPath $publishedKoGraphPath -Destination (Join-Path $destinationPath 'knowledge-graph.json') -Force
Copy-Item -LiteralPath $publishedKoGraphPath -Destination (Join-Path $graphDestination 'knowledge-graph.json') -Force

$fullGraphDestination = Join-Path $destinationPath 'full'
foreach ($fullGraph in @(
    @{ Language = 'ko'; Source = $publishedKoGraphPath },
    @{ Language = 'en'; Source = $publishedEnGraphPath }
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

& $NodeExecutable $sourceMirrorGeneratorPath $repositoryPath $destinationPath
if ($LASTEXITCODE -ne 0) {
    throw "Source mirror generation failed with exit code $LASTEXITCODE."
}

& $NodeExecutable $generatorPath $graphPath $destinationPath
if ($LASTEXITCODE -ne 0) {
    throw "Portal/API page generation failed with exit code $LASTEXITCODE."
}

& $NodeExecutable $projectGraphGeneratorPath $repositoryPath $destinationPath $publishedKoGraphPath $publishedEnGraphPath
if ($LASTEXITCODE -ne 0) {
    throw "Project graph generation failed with exit code $LASTEXITCODE."
}

& $NodeExecutable $projectGraphValidatorPath $repositoryPath $destinationPath
if ($LASTEXITCODE -ne 0) {
    throw "Project graph validation failed with exit code $LASTEXITCODE."
}

Write-Output "Published knowledge hub: $destinationPath"
Write-Output "Published advanced graph: $graphDestination"
Write-Output "Published source previews from scan and source-verified Overlay."
Write-Output "Published project graphs: 160"
Write-Output "Published ontology consumer artifacts: $($ontologyArtifactPaths.Count)"
