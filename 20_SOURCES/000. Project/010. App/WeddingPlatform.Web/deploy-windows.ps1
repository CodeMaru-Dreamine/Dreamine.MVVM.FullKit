# ============================================================
# WeddingPlatform Windows 서버 배포 스크립트
# 서버에서 관리자 PowerShell로 실행: .\deploy-windows.ps1
# ============================================================

$InstallDir  = "C:\wedding"
$ServiceName = "WeddingPlatform"
$ExeName     = "WeddingPlatform.Web.exe"
$ZipFile     = Join-Path $PSScriptRoot "wedding-deploy.zip"

# ── 1. 서비스 중지 ────────────────────────────────────────────
$svc = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($svc -and $svc.Status -eq 'Running') {
    Write-Host "▶ 서비스 중지..." -ForegroundColor Yellow
    Stop-Service -Name $ServiceName -Force
    Start-Sleep -Seconds 2
}

# ── 2. 기존 App_Data 백업 (사진/데이터 보존) ─────────────────
$dataBackup = "$InstallDir\_data_backup"
$dataDir    = "$InstallDir\App_Data\Wedding"
if (Test-Path $dataDir) {
    Write-Host "▶ 기존 데이터 백업..." -ForegroundColor Yellow
    if (Test-Path $dataBackup) { Remove-Item $dataBackup -Recurse -Force }
    Copy-Item $dataDir $dataBackup -Recurse -Force
}

# ── 3. 앱 압축 해제 ───────────────────────────────────────────
if (Test-Path $ZipFile) {
    Write-Host "▶ 앱 패키지 압축 해제..." -ForegroundColor Yellow
    New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null
    Expand-Archive -Path $ZipFile -DestinationPath $InstallDir -Force
} else {
    Write-Host "⚠ wedding-deploy.zip 없음 — publish.ps1 실행 후 이 파일과 같은 폴더에 복사하세요" -ForegroundColor Red
    exit 1
}

# ── 4. 백업 데이터 복원 (새 zip이 덮어쓴 경우 다시 복원) ─────
if (Test-Path $dataBackup) {
    Write-Host "▶ 기존 사진/데이터 복원..." -ForegroundColor Yellow
    # 백업 데이터 우선 (서버 사진이 더 최신)
    $backupItems = Get-ChildItem $dataBackup -Recurse -File
    foreach ($item in $backupItems) {
        $rel  = $item.FullName.Substring($dataBackup.Length)
        $dest = "$dataDir$rel"
        $destDir = Split-Path $dest -Parent
        New-Item -ItemType Directory -Force -Path $destDir | Out-Null
        if (-not (Test-Path $dest)) {
            Copy-Item $item.FullName $dest -Force
        }
    }
}

# ── 5. appsettings.Production.json 생성 (없을 때만) ───────────
$settingsFile = "$InstallDir\appsettings.Production.json"
if (-not (Test-Path $settingsFile)) {
    Write-Host "▶ appsettings.Production.json 생성..." -ForegroundColor Yellow
    @"
{
  "WeddingServer": {
    "Port": 4080,
    "ListenAnyIp": false
  },
  "Wedding": {
    "DataPath": "",
    "SuperAdminPassword": "여기를-강력한-비번으로-바꾸세요"
  }
}
"@ | Set-Content $settingsFile -Encoding UTF8
    Write-Host "⚠ $settingsFile 의 SuperAdminPassword 를 반드시 변경하세요!" -ForegroundColor Red
    Write-Host "⚠ 앱은 localhost:4080 에서만 대기합니다. 외부 접속은 nginx(conf\nginx.conf)를 통해 80/443으로 제공하세요." -ForegroundColor Cyan
}

# ── 6. Windows 서비스 등록 ────────────────────────────────────
$exePath = "$InstallDir\$ExeName"
if (-not (Test-Path $exePath)) {
    Write-Host "✖ $exePath 없음 — 빌드 산출물 확인 필요" -ForegroundColor Red
    exit 1
}

if ($svc) {
    # 이미 있으면 경로만 업데이트
    Write-Host "▶ 서비스 경로 업데이트..." -ForegroundColor Yellow
    sc.exe config $ServiceName binPath= "`"$exePath`"" | Out-Null
} else {
    Write-Host "▶ 서비스 등록..." -ForegroundColor Yellow
    New-Service -Name $ServiceName `
                -DisplayName "Codemaru Wedding Platform" `
                -BinaryPathName "`"$exePath`"" `
                -StartupType Automatic | Out-Null
}

# ── 7. 서비스 시작 ────────────────────────────────────────────
Write-Host "▶ 서비스 시작..." -ForegroundColor Yellow
Start-Service -Name $ServiceName
Start-Sleep -Seconds 3

$svc = Get-Service -Name $ServiceName
if ($svc.Status -eq 'Running') {
    Write-Host ""
    Write-Host "✅ 배포 완료! 서비스 실행 중" -ForegroundColor Green
    Write-Host "   로그: Get-EventLog -LogName Application -Source $ServiceName -Newest 20"
} else {
    Write-Host "✖ 서비스 시작 실패. 상태: $($svc.Status)" -ForegroundColor Red
    Write-Host "   로그 확인: eventvwr.msc"
}
