param(
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,
    [string]$ProjectFilter = '*'
)

$ErrorActionPreference = 'Stop'
$sourcesRoot = Join-Path $RepositoryRoot '20_SOURCES'
$outputRoot = Join-Path $RepositoryRoot '10_DOCUMENTS\Doxygen'
$utf8 = [System.Text.UTF8Encoding]::new($false)

function Get-ProjectValue {
    param([xml]$ProjectXml, [string[]]$Names)

    foreach ($name in $Names) {
        $node = $ProjectXml.SelectSingleNode("//*[local-name()='$name']")
        if ($node -and -not [string]::IsNullOrWhiteSpace($node.InnerText) -and $node.InnerText -notmatch '^\$\(') {
            return $node.InnerText.Trim()
        }
    }

    return $null
}

function Get-KoreanBrief {
    param([string]$ProjectName)

    if ($ProjectName -match '\.Tests$') { return "$ProjectName 기능을 검증하는 자동화 테스트 프로젝트입니다." }
    if ($ProjectName -match '^Sample') { return "$ProjectName 사용 방법을 보여 주는 예제 프로젝트입니다." }
    if ($ProjectName -match '(Web|Codemaru|Wedding|Families|Portfolio|ShopPlatform|DreamineVMS)') { return "$ProjectName 웹 및 애플리케이션 기능을 제공합니다." }
    if ($ProjectName -match 'Abstractions|Interfaces') { return "$ProjectName 공용 계약과 추상화를 제공합니다." }
    if ($ProjectName -match 'Communication') { return "$ProjectName 통신 기능과 관련 API를 제공합니다." }
    if ($ProjectName -match 'Database') { return "$ProjectName 데이터베이스 기능과 관련 API를 제공합니다." }
    if ($ProjectName -match 'PLC|IO') { return "$ProjectName 산업 자동화 및 I/O 기능을 제공합니다." }
    if ($ProjectName -match 'UI|Wpf|Maui|WinForms|Blazor') { return "$ProjectName 사용자 인터페이스 기능과 구성 요소를 제공합니다." }
    return "$ProjectName 프로젝트의 API와 구성 요소를 제공합니다."
}

function Get-ReadmeSummary {
    param([string]$Path)

    if (-not (Test-Path $Path)) { return $null }
    foreach ($line in [IO.File]::ReadLines($Path)) {
        if ($line -match '^>\s+(.+?)\s*$') { return $Matches[1].Trim() }
    }
    return $null
}

function Escape-DoxyValue {
    param([string]$Value)
    return $Value.Replace('"', '\"').Replace("`r", ' ').Replace("`n", ' ')
}

function New-Readme {
    param(
        [string]$Path,
        [string]$ProjectName,
        [string]$Version,
        [string]$Description,
        [string]$TargetFramework,
        [bool]$Korean
    )

    if (Test-Path $Path) { return $false }

    if ($Korean) {
        $content = @"
# $ProjectName

$Description

## 프로젝트 정보

- 버전: $Version
- 대상 프레임워크: $TargetFramework

## 문서 생성

이 프로젝트 디렉터리에서 다음 명령을 실행합니다.

```powershell
doxygen Doxyfile.kr
```

영문 문서는 `Doxyfile.en`으로 생성할 수 있습니다.
"@
    }
    else {
        $content = @"
# $ProjectName

$Description

## Project information

- Version: $Version
- Target framework: $TargetFramework

## Generate documentation

Run the following command from this project directory:

```powershell
doxygen Doxyfile.en
```

Korean documentation can be generated with `Doxyfile.kr`.
"@
    }

    [IO.File]::WriteAllText($Path, $content.Trim() + "`r`n", $utf8)
    return $true
}

function New-DoxyfileContent {
    param(
        [string]$ProjectName,
        [string]$Version,
        [string]$Brief,
        [string]$Language,
        [string]$Section,
        [string]$OutputDirectory,
        [string]$Readme,
        [string]$ExcludedReadme,
        [string]$DotPath,
        [string]$LayoutFile
    )

    $escapedName = Escape-DoxyValue $ProjectName
    $escapedBrief = Escape-DoxyValue $Brief
    $escapedOutput = $OutputDirectory.Replace('\', '/')
    $escapedDotPath = $DotPath.Replace('\', '/')
    $escapedLayoutFile = $LayoutFile.Replace('\', '/')

    return @"
# Generated from project metadata by 10_DOCUMENTS/Doxygen/Generate-DoxygenConfigs.ps1
DOXYFILE_ENCODING       = UTF-8
PROJECT_NAME            = "$escapedName"
PROJECT_NUMBER          = "$Version"
PROJECT_BRIEF           = "$escapedBrief"
OUTPUT_DIRECTORY        = "$escapedOutput"
OUTPUT_LANGUAGE         = $Language
ENABLED_SECTIONS        = $Section

INPUT                    = "." "$Readme"
USE_MDFILE_AS_MAINPAGE   = "$Readme"
FILE_PATTERNS            = *.cs *.md
RECURSIVE                = YES
EXCLUDE                  = bin obj .git .vs node_modules TestResults wwwroot "$ExcludedReadme"
EXCLUDE_PATTERNS         = */bin/* */obj/* */.git/* */.vs/* */node_modules/* */TestResults/* */wwwroot/* */Generated/* */generated/* *.g.cs *.g.i.cs *.Designer.cs *.AssemblyInfo.cs *.AssemblyAttributes.cs

EXTRACT_ALL              = YES
EXTRACT_PRIVATE          = YES
EXTRACT_STATIC           = YES
EXTRACT_LOCAL_CLASSES    = YES
EXTRACT_LOCAL_METHODS    = YES
HIDE_UNDOC_MEMBERS       = NO
HIDE_UNDOC_CLASSES       = NO
FULL_PATH_NAMES          = NO
SHORT_NAMES              = YES

MARKDOWN_SUPPORT         = YES
AUTOLINK_SUPPORT         = YES
SOURCE_BROWSER           = YES
INLINE_SOURCES           = YES
REFERENCED_BY_RELATION   = YES
REFERENCES_RELATION      = YES
GENERATE_TREEVIEW        = YES
SEARCHENGINE             = YES

GENERATE_HTML            = YES
GENERATE_LATEX           = NO
GENERATE_XML             = YES
LAYOUT_FILE              = "$escapedLayoutFile"
HTML_COLORSTYLE          = TOGGLE
HTML_DYNAMIC_MENUS       = YES
DISABLE_INDEX            = NO

HAVE_DOT                 = YES
DOT_PATH                 = "$escapedDotPath"
CLASS_GRAPH              = YES
COLLABORATION_GRAPH      = YES
GROUP_GRAPHS             = YES
GRAPHICAL_HIERARCHY      = YES
DIRECTORY_GRAPH          = YES
CALL_GRAPH               = YES
CALLER_GRAPH             = YES
DOT_IMAGE_FORMAT         = png
INTERACTIVE_SVG          = NO
DOT_GRAPH_MAX_NODES      = 100
MAX_DOT_GRAPH_DEPTH      = 4
DOT_CLEANUP              = YES

QUIET                    = NO
WARNINGS                 = YES
WARN_IF_UNDOCUMENTED     = YES
WARN_IF_DOC_ERROR        = YES
WARN_LOGFILE             = "doxygen-$($Section.ToLowerInvariant()).warnings.log"
"@.Trim() + "`r`n"
}

$dotExecutable = @(
    'C:\Program Files (x86)\Graphviz\bin\dot.exe',
    'C:\Program Files\Graphviz\bin\dot.exe'
) | Where-Object { Test-Path $_ } | Select-Object -First 1
$dotPath = if ($dotExecutable) { Split-Path $dotExecutable } else { '' }

$doxygenCommand = Get-Command doxygen -ErrorAction SilentlyContinue
if (-not $doxygenCommand) {
    throw 'Doxygen executable was not found. Install Doxygen before generating project configurations.'
}

function New-DoxygenLayout {
    param(
        [string]$Path,
        [string]$Title,
        [string]$Language
    )

    if (Test-Path -LiteralPath $Path) {
        Remove-Item -LiteralPath $Path -Force
    }
    & $doxygenCommand.Source -l $Path
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $Path)) {
        throw "Could not generate the Doxygen layout file: $Path"
    }

    $layout = [IO.File]::ReadAllText($Path)
    $tab = "    <tab type=`"user`" visible=`"yes`" url=`"https://dreamine.kr/knowledge?lang=$Language`" title=`"$Title`"/>"
    if (-not $layout.Contains('<navindex>')) {
        throw "Doxygen layout does not contain a navigation index: $Path"
    }
    $layout = $layout.Replace('<navindex>', "<navindex>`r`n$tab")
    [IO.File]::WriteAllText($Path, $layout, $utf8)
}

$layoutKoPath = Join-Path $outputRoot 'DoxygenLayout.kr.xml'
$layoutEnPath = Join-Path $outputRoot 'DoxygenLayout.en.xml'
New-DoxygenLayout $layoutKoPath '← Dreamine 문서 허브' 'ko'
New-DoxygenLayout $layoutEnPath '← Dreamine Docs Hub' 'en'

$defaultVersion = '1.0.0'
$rootPropertiesPath = Join-Path $sourcesRoot 'Directory.Build.props'
if (Test-Path $rootPropertiesPath) {
    [xml]$rootPropertiesXml = [IO.File]::ReadAllText($rootPropertiesPath)
    $inheritedVersion = Get-ProjectValue $rootPropertiesXml @('AppVersion', 'Version', 'VersionPrefix')
    if (-not [string]::IsNullOrWhiteSpace($inheritedVersion)) {
        $defaultVersion = $inheritedVersion
    }
}

$projectPaths = @(
    rg --files $sourcesRoot -g '*.csproj' -g '!**/bin/**' -g '!**/obj/**' |
        Where-Object { [IO.Path]::GetFileNameWithoutExtension($_) -like $ProjectFilter }
)
$createdReadmes = 0
$generatedConfigs = 0

foreach ($relativeOrAbsolutePath in $projectPaths) {
    $projectPath = (Resolve-Path $relativeOrAbsolutePath).Path
    $projectDirectory = Split-Path $projectPath
    [xml]$projectXml = [IO.File]::ReadAllText($projectPath)
    $baseName = [IO.Path]::GetFileNameWithoutExtension($projectPath)
    $projectName = Get-ProjectValue $projectXml @('PackageId', 'AssemblyName', 'Title')
    if ([string]::IsNullOrWhiteSpace($projectName)) { $projectName = $baseName }
    $version = Get-ProjectValue $projectXml @('Version', 'VersionPrefix', 'PackageVersion')
    if ([string]::IsNullOrWhiteSpace($version)) { $version = $defaultVersion }
    $descriptionEn = Get-ProjectValue $projectXml @('Description', 'PackageDescription')
    if ([string]::IsNullOrWhiteSpace($descriptionEn)) { $descriptionEn = "$projectName project APIs and components." }
    $descriptionKo = Get-KoreanBrief $projectName
    $readmeSummaryEn = Get-ReadmeSummary (Join-Path $projectDirectory 'README.md')
    $readmeSummaryKo = Get-ReadmeSummary (Join-Path $projectDirectory 'README_KO.md')
    if (-not [string]::IsNullOrWhiteSpace($readmeSummaryEn)) { $descriptionEn = $readmeSummaryEn }
    if (-not [string]::IsNullOrWhiteSpace($readmeSummaryKo)) { $descriptionKo = $readmeSummaryKo }
    $targetFramework = Get-ProjectValue $projectXml @('TargetFramework', 'TargetFrameworks')
    if ([string]::IsNullOrWhiteSpace($targetFramework)) { $targetFramework = 'See project file' }

    $relativeProjectPath = [IO.Path]::GetRelativePath($sourcesRoot, $projectPath)
    $category = $relativeProjectPath.Split([IO.Path]::DirectorySeparatorChar)[0]
    $projectOutputRoot = Join-Path $outputRoot (Join-Path $category $projectName)
    $enOutputPath = Join-Path $projectOutputRoot 'EN'
    $krOutputPath = Join-Path $projectOutputRoot 'KR'
    [IO.Directory]::CreateDirectory($enOutputPath) | Out-Null
    [IO.Directory]::CreateDirectory($krOutputPath) | Out-Null
    $enOutput = [IO.Path]::GetRelativePath($projectDirectory, $enOutputPath)
    $krOutput = [IO.Path]::GetRelativePath($projectDirectory, $krOutputPath)

    if (New-Readme (Join-Path $projectDirectory 'README.md') $projectName $version $descriptionEn $targetFramework $false) { $createdReadmes++ }
    if (New-Readme (Join-Path $projectDirectory 'README_KO.md') $projectName $version $descriptionKo $targetFramework $true) { $createdReadmes++ }

    $enLayout = [IO.Path]::GetRelativePath($projectDirectory, $layoutEnPath)
    $krLayout = [IO.Path]::GetRelativePath($projectDirectory, $layoutKoPath)
    $enConfig = New-DoxyfileContent $projectName $version $descriptionEn 'English' 'EN' $enOutput 'README.md' 'README_KO.md' $dotPath $enLayout
    $krConfig = New-DoxyfileContent $projectName $version $descriptionKo 'Korean' 'KO' $krOutput 'README_KO.md' 'README.md' $dotPath $krLayout
    [IO.File]::WriteAllText((Join-Path $projectDirectory 'Doxyfile.en'), $enConfig, $utf8)
    [IO.File]::WriteAllText((Join-Path $projectDirectory 'Doxyfile.kr'), $krConfig, $utf8)
    $generatedConfigs += 2
}

Write-Output "Projects=$($projectPaths.Count)"
Write-Output "GeneratedConfigs=$generatedConfigs"
Write-Output "CreatedReadmes=$createdReadmes"
Write-Output "DotPath=$dotPath"
