param(
    [ValidateSet('EN', 'KR', 'ALL')]
    [string]$Language = 'ALL',
    [string]$ProjectFilter = '*',
    [switch]$Clean,
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
)

$ErrorActionPreference = 'Stop'
$sourcesRoot = Join-Path $RepositoryRoot '20_SOURCES'
$documentationRoot = [IO.Path]::GetFullPath((Join-Path $RepositoryRoot '10_DOCUMENTS\Doxygen'))
$doxygen = @(
    'C:\Program Files\doxygen\bin\doxygen.exe',
    'C:\Program Files (x86)\doxygen\bin\doxygen.exe'
) | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $doxygen) { throw 'Doxygen executable was not found.' }

$languages = if ($Language -eq 'ALL') { @('EN', 'KR') } else { @($Language) }
$projects = @(rg --files $sourcesRoot -g '*.csproj' -g '!**/bin/**' -g '!**/obj/**' |
    Where-Object { [IO.Path]::GetFileNameWithoutExtension($_) -like $ProjectFilter } |
    Sort-Object)
$results = [Collections.Generic.List[object]]::new()

foreach ($projectPathValue in $projects) {
    $projectPath = (Resolve-Path $projectPathValue).Path
    $projectDirectory = Split-Path $projectPath
    $projectName = [IO.Path]::GetFileNameWithoutExtension($projectPath)

    foreach ($currentLanguage in $languages) {
        $configName = if ($currentLanguage -eq 'EN') { 'Doxyfile.en' } else { 'Doxyfile.kr' }
        $configPath = Join-Path $projectDirectory $configName
        if (-not (Test-Path $configPath)) {
            $results.Add([pscustomobject]@{ Project=$projectName; Language=$currentLanguage; ExitCode=-1; Warnings=0; Graphs=0; Status='MissingConfig' })
            continue
        }

        $outputLine = (Select-String -Path $configPath -Pattern '^OUTPUT_DIRECTORY\s*=').Line
        $relativeOutput = ($outputLine -split '=', 2)[1].Trim().Trim('"')
        $outputPath = [IO.Path]::GetFullPath((Join-Path $projectDirectory $relativeOutput))
        if (-not $outputPath.StartsWith($documentationRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Unsafe Doxygen output path: $outputPath"
        }

        [IO.Directory]::CreateDirectory($outputPath) | Out-Null
        if ($Clean) {
            Get-ChildItem -LiteralPath $outputPath -Force | Remove-Item -Recurse -Force
        }

        $process = [Diagnostics.Process]::new()
        $process.StartInfo = [Diagnostics.ProcessStartInfo]::new($doxygen, $configName)
        $process.StartInfo.WorkingDirectory = $projectDirectory
        $process.StartInfo.UseShellExecute = $false
        $process.StartInfo.CreateNoWindow = $true
        $process.StartInfo.RedirectStandardOutput = $true
        $process.StartInfo.RedirectStandardError = $true
        $null = $process.Start()
        $stdoutTask = $process.StandardOutput.ReadToEndAsync()
        $stderrTask = $process.StandardError.ReadToEndAsync()
        $process.WaitForExit()
        $null = $stdoutTask.Result
        $null = $stderrTask.Result

        $section = $currentLanguage.ToLowerInvariant()
        if ($currentLanguage -eq 'KR') { $section = 'ko' }
        $warningLog = Join-Path $projectDirectory "doxygen-$section.warnings.log"
        $warnings = if (Test-Path $warningLog) {
            @(Get-Content $warningLog | Where-Object { $_ -match '(warning|error):' }).Count
        } else { 0 }
        $imageFormatLine = (Select-String -Path $configPath -Pattern '^DOT_IMAGE_FORMAT\s*=').Line
        $imageFormat = if ($imageFormatLine) { ($imageFormatLine -split '=', 2)[1].Trim() } else { 'png' }
        $graphs = @(Get-ChildItem -LiteralPath $outputPath -Recurse -Filter "*.$imageFormat" -ErrorAction SilentlyContinue).Count
        $status = if ($process.ExitCode -eq 0) { 'Success' } else { 'Failed' }
        $result = [pscustomobject]@{
            Project = $projectName
            Language = $currentLanguage
            ExitCode = $process.ExitCode
            Warnings = $warnings
            Graphs = $graphs
            Status = $status
        }
        $results.Add($result)
        Write-Output "$projectName|$currentLanguage|Exit=$($process.ExitCode)|Warnings=$warnings|Graphs=$graphs"
    }
}

$summaryPath = Join-Path $documentationRoot "doxygen-build-summary-$($Language.ToLowerInvariant()).json"
$results | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $summaryPath -Encoding utf8
$failed = @($results | Where-Object ExitCode -ne 0)
Write-Output "Projects=$($projects.Count) Runs=$($results.Count) Failed=$($failed.Count) Summary=$summaryPath"
if ($failed.Count -gt 0) { exit 1 }
