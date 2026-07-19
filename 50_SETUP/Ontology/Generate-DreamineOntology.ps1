[CmdletBinding()]
param(
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,
    [switch]$SkipDotNetValidation
)

$ErrorActionPreference = 'Stop'
$schemaPath = Join-Path $PSScriptRoot 'dreamine.schema.yaml'
$venvPath = Join-Path $PSScriptRoot '.venv'
$linkmlPath = Join-Path $venvPath 'Scripts\linkml.exe'
$pythonPath = Join-Path $venvPath 'Scripts\python.exe'
$outputPath = Join-Path $RepositoryRoot '.ua\ontology'
$domainAnalysisPath = Join-Path $RepositoryRoot '.ua\intermediate\domain-analysis.json'
$domainGraphPath = Join-Path $RepositoryRoot '.ua\domain-graph.json'
$knowledgeGraphPath = Join-Path $RepositoryRoot '.ua\knowledge-graph.ko.json'
$nodePath = 'C:\Users\Minsu\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\bin\node.exe'
$sourceAuditProject = Join-Path $PSScriptRoot 'SourceAudit\Dreamine.Ontology.SourceAudit.csproj'
$sourceAuditConfig = Join-Path $PSScriptRoot 'SourceAudit\NuGet.Config'
$sourceAuditDll = Join-Path $PSScriptRoot 'SourceAudit\bin\Release\net10.0\Dreamine.Ontology.SourceAudit.dll'

if (-not (Test-Path -LiteralPath $linkmlPath)) {
    throw "Ontology virtual environment is missing. Create $venvPath and install requirements.txt."
}
if (-not (Test-Path -LiteralPath $nodePath)) {
    $nodeCommand = Get-Command node -ErrorAction SilentlyContinue
    if (-not $nodeCommand) { throw 'Node.js is required to build ontology instances.' }
    $nodePath = $nodeCommand.Source
}
if (-not (Test-Path -LiteralPath $domainAnalysisPath)) {
    throw "Domain graph source is missing: $domainAnalysisPath"
}

$env:PYSTOW_HOME = Join-Path $PSScriptRoot '.pystow'
New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
Copy-Item -LiteralPath $domainAnalysisPath -Destination $domainGraphPath -Force

& dotnet restore $sourceAuditProject --configfile $sourceAuditConfig --nologo
if ($LASTEXITCODE -ne 0) { throw 'Roslyn source audit restore failed.' }
& dotnet build $sourceAuditProject --configuration Release --no-restore --nologo
if ($LASTEXITCODE -ne 0) { throw 'Roslyn source audit build failed.' }
& dotnet $sourceAuditDll $RepositoryRoot $knowledgeGraphPath $domainGraphPath '-' $outputPath
if ($LASTEXITCODE -ne 0) { throw 'Roslyn source declaration scan failed.' }

$generatedJsonSchema = & $linkmlPath generate json-schema --top-class KnowledgeGraph $schemaPath
if ($LASTEXITCODE -ne 0) { throw 'LinkML JSON Schema generation failed.' }
Set-Content -LiteralPath (Join-Path $outputPath 'dreamine.schema.json') -Value ($generatedJsonSchema -join [Environment]::NewLine) -Encoding utf8
& $linkmlPath generate jsonld-context $schemaPath -o (Join-Path $outputPath 'dreamine.context.jsonld')
if ($LASTEXITCODE -ne 0) { throw 'LinkML JSON-LD context generation failed.' }
$generatedOwl = & $linkmlPath generate owl --format ttl $schemaPath
if ($LASTEXITCODE -ne 0) { throw 'LinkML OWL generation failed.' }
Set-Content -LiteralPath (Join-Path $outputPath 'dreamine.owl.ttl') -Value ($generatedOwl -join [Environment]::NewLine) -Encoding utf8
$generatedShacl = & $linkmlPath generate shacl --non-closed $schemaPath
if ($LASTEXITCODE -ne 0) { throw 'LinkML SHACL generation failed.' }
Set-Content -LiteralPath (Join-Path $outputPath 'dreamine.shacl.ttl') -Value ($generatedShacl -join [Environment]::NewLine) -Encoding utf8
& $pythonPath (Join-Path $PSScriptRoot 'Generate-ArchitectureShapes.py') $schemaPath $outputPath
if ($LASTEXITCODE -ne 0) { throw 'Architecture SHACL generation failed.' }

& $nodePath (Join-Path $PSScriptRoot 'Build-OntologyGraph.mjs') $RepositoryRoot $outputPath
if ($LASTEXITCODE -ne 0) { throw 'Ontology instance generation failed.' }

& dotnet $sourceAuditDll $RepositoryRoot $knowledgeGraphPath $domainGraphPath (Join-Path $outputPath 'instances.json') $outputPath
if ($LASTEXITCODE -ne 0) { throw 'Source graph and ontology overlay audit failed.' }

& $nodePath (Join-Path $PSScriptRoot 'Apply-OntologyOverlay.mjs') $RepositoryRoot
if ($LASTEXITCODE -ne 0) { throw 'Source-verified UI and chatbot graph generation failed.' }

& $pythonPath (Join-Path $PSScriptRoot 'Validate-ArchitectureShapes.py') $schemaPath (Join-Path $outputPath 'instances.json') (Join-Path $outputPath 'architecture-projection.jsonld') (Join-Path $PSScriptRoot 'fixtures')
if ($LASTEXITCODE -ne 0) { throw 'Architecture SHACL fixture validation failed.' }

& $pythonPath (Join-Path $PSScriptRoot 'Validate-Ontology.py') (Join-Path $outputPath 'instances.jsonld') (Join-Path $outputPath 'dreamine.shacl.ttl') (Join-Path $outputPath 'linkml-shacl-validation.json')
if ($LASTEXITCODE -ne 0) { throw 'pySHACL validation failed.' }

if (-not $SkipDotNetValidation) {
    $validatorProject = Join-Path $PSScriptRoot 'Validator\Dreamine.Ontology.Validator.csproj'
    & dotnet run --project $validatorProject --configuration Release -- (Join-Path $outputPath 'validation-sample.jsonld') (Join-Path $outputPath 'dreamine.shacl.ttl')
    if ($LASTEXITCODE -ne 0) { throw 'dotNetRDF validation failed.' }
}

Write-Host "Ontology artifacts generated at $outputPath"
