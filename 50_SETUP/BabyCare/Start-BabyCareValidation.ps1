[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$PackageDirectory,
    [string]$ValidationRoot = 'C:\BabyCareValidation',
    [ValidateRange(1024,65535)][int]$Port = 5188,
    [string]$CodexExecutable
)
$ErrorActionPreference = 'Stop'
$package = (Resolve-Path -LiteralPath $PackageDirectory).Path
$manifestPath = Join-Path $package 'runtime-manifest.json'
$manifestHash = (Get-Content -LiteralPath (Join-Path $package 'runtime-manifest.sha256') -Raw).Trim()
if ((Get-FileHash -LiteralPath $manifestPath -Algorithm SHA256).Hash -ne $manifestHash) { throw 'Manifest hash mismatch.' }
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
if ($manifest.Revision -ne '20260923-02' -or $manifest.RuntimeIdentifier -ne 'win-x64') { throw 'Unexpected package revision/platform.' }
$zip = Join-Path $package 'runtime.zip'
if ((Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash -ne $manifest.RuntimeZipSha256) { throw 'Runtime zip hash mismatch.' }
$root = [IO.Path]::GetFullPath($ValidationRoot)
$destination = [IO.Path]::GetFullPath((Join-Path $root '20260923-02-runtime'))
if (-not $destination.StartsWith($root.TrimEnd('\')+'\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid destination.' }
if (Test-Path -LiteralPath $destination) { throw "Validation folder already exists: $destination. Inspect it; do not overwrite a running instance." }
if (Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue) { throw "Port $Port is already in use." }
if ($CodexExecutable -and (-not (Test-Path -LiteralPath $CodexExecutable -PathType Leaf) -or [IO.Path]::GetExtension($CodexExecutable) -ne '.exe')) { throw 'Use the actual Codex .exe path.' }
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::OpenRead($zip)
try {
    foreach ($entry in $archive.Entries) {
        $resolved = [IO.Path]::GetFullPath((Join-Path $destination $entry.FullName))
        if (-not $resolved.StartsWith($destination+'\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Archive path escapes validation directory.' }
    }
} finally { $archive.Dispose() }
New-Item -ItemType Directory -Force -Path $destination | Out-Null
Expand-Archive -LiteralPath $zip -DestinationPath $destination
foreach ($file in $manifest.Files) {
    $path = [IO.Path]::GetFullPath((Join-Path $destination $file.Path))
    if (-not $path.StartsWith($destination+'\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Manifest path escapes validation directory.' }
    if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $file.SHA256) { throw "Extracted file mismatch: $($file.Path)" }
}
$publish = Join-Path $destination 'publish'
$data = Join-Path $destination 'validation-data'
New-Item -ItemType Directory -Force -Path $data | Out-Null
$settings = @{
    ASPNETCORE_ENVIRONMENT='Development'; ASPNETCORE_URLS="http://127.0.0.1:$Port";
    BabyCare__DataPath=$data; Codex__Enabled=([bool]$CodexExecutable).ToString();
    Authentication__UseCentralPortal='false'; Authentication__UsersDbPath=(Join-Path $data 'users.db');
    Authentication__CookieName='.BabyCare.Validation'; Authentication__CookieDomain='';
    Authentication__DataProtectionKeysPath=(Join-Path $data 'IdentityKeys');
    Authentication__DataProtectionApplicationName='BabyCare.Validation'
}
if ($CodexExecutable) { $settings['Codex__Executable']=$CodexExecutable }
$previous = @{}
try {
    foreach ($key in $settings.Keys) { $previous[$key]=[Environment]::GetEnvironmentVariable($key,'Process'); [Environment]::SetEnvironmentVariable($key,$settings[$key],'Process') }
    $process = Start-Process -FilePath (Join-Path $publish 'BabyCare.Web.exe') -WorkingDirectory $publish -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $destination 'stdout.log') -RedirectStandardError (Join-Path $destination 'stderr.log')
} finally { foreach($key in $previous.Keys){[Environment]::SetEnvironmentVariable($key,$previous[$key],'Process')} }
$info = [ordered]@{Revision=$manifest.Revision;ProcessId=$process.Id;StartedUtc=[DateTimeOffset]::UtcNow.ToString('O');Url="http://127.0.0.1:$Port";DataPath=$data;CodexEnabled=[bool]$CodexExecutable}
$info | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $destination 'validation-process.json') -Encoding utf8
$healthy=$false
for($attempt=0;$attempt -lt 30;$attempt++) {
    if($process.HasExited){throw "App exited ($($process.ExitCode)). Inspect validation logs."}
    try { $response=Invoke-WebRequest -Uri "$($info.Url)/healthz" -UseBasicParsing -TimeoutSec 2; if($response.StatusCode -eq 200 -and $response.Content -eq 'OK'){$healthy=$true;break} } catch { }
    Start-Sleep -Seconds 1
}
if(-not $healthy){throw "Health check not ready. Inspect process $($process.Id) and validation logs; existing services were not changed."}
foreach($path in @('/','/auth/login','/app.css','/speech.js','/manifest.webmanifest','/BabyCare.Web.styles.css')) {
    $response=Invoke-WebRequest -Uri ($info.Url+$path) -UseBasicParsing -TimeoutSec 10
    if($response.StatusCode -ne 200){throw "Endpoint failed: $path"}
}
$info['Health']='PASS'; $info | ConvertTo-Json
Write-Output 'Isolated runtime is running. No SDK, Windows service registration, firewall, Caddy, DNS or production DB changes were made.'
