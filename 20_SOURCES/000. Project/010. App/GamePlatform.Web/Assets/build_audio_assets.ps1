param(
    [string]$FfmpegPath,
    [string]$SourceDirectory = "C:\Users\Minsu\Downloads",
    [string]$OutputDirectory = (Join-Path (Split-Path $PSScriptRoot -Parent) "wwwroot\audio\maru-idle")
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($FfmpegPath)) {
    $command = Get-Command ffmpeg -ErrorAction SilentlyContinue
    if ($null -ne $command) {
        $FfmpegPath = $command.Source
    }
    else {
        $workspaceRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..\..\..")).Path
        $bundled = Get-ChildItem (Join-Path $workspaceRoot ".tools\ffmpeg") -Filter ffmpeg.exe -Recurse -ErrorAction SilentlyContinue |
            Select-Object -First 1
        if ($null -ne $bundled) { $FfmpegPath = $bundled.FullName }
    }
}

if ([string]::IsNullOrWhiteSpace($FfmpegPath) -or -not (Test-Path -LiteralPath $FfmpegPath)) {
    throw "FFmpeg executable was not found: $FfmpegPath"
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

function New-SeamlessLoop {
    param(
        [Parameter(Mandatory)][string]$InputFile,
        [Parameter(Mandatory)][double]$StartSeconds,
        [Parameter(Mandatory)][double]$LengthSeconds,
        [Parameter(Mandatory)][string]$OutputFile
    )

    $crossfade = 1.5
    $middleEnd = $LengthSeconds - $crossfade
    $absoluteEnd = $StartSeconds + $LengthSeconds
    $filter = "[0:a]atrim=start=$StartSeconds`:end=$absoluteEnd,asetpts=PTS-STARTPTS,asplit=3[midin][tailin][headin];" +
              "[midin]atrim=start=$crossfade`:end=$middleEnd,asetpts=PTS-STARTPTS[mid];" +
              "[tailin]atrim=start=$middleEnd`:end=$LengthSeconds,asetpts=PTS-STARTPTS[tail];" +
              "[headin]atrim=start=0`:end=$crossfade,asetpts=PTS-STARTPTS[head];" +
              "[tail][head]acrossfade=d=$crossfade`:c1=tri`:c2=tri[seam];" +
              "[mid][seam]concat=n=2`:v=0`:a=1,alimiter=limit=0.92[out]"

    & $FfmpegPath -hide_banner -loglevel error -y -i $InputFile -filter_complex $filter -map "[out]" `
        -c:a libopus -b:a 128k -ar 48000 -ac 2 $OutputFile
    if ($LASTEXITCODE -ne 0) { throw "Failed to build BGM: $OutputFile" }
}

function New-Effect {
    param(
        [Parameter(Mandatory)][string]$InputFile,
        [Parameter(Mandatory)][string]$Filter,
        [Parameter(Mandatory)][string]$OutputFile
    )

    & $FfmpegPath -hide_banner -loglevel error -y -i $InputFile -af $Filter `
        -c:a libopus -b:a 80k -ar 48000 -ac 1 $OutputFile
    if ($LASTEXITCODE -ne 0) { throw "Failed to build effect: $OutputFile" }
}

New-SeamlessLoop `
    -InputFile (Join-Path $SourceDirectory "Ironfang Relics.mp3") `
    -StartSeconds 208 -LengthSeconds 90 `
    -OutputFile (Join-Path $OutputDirectory "ironfang-relics-battle.ogg")

New-SeamlessLoop `
    -InputFile (Join-Path $SourceDirectory "Steel Relic Reel.mp3") `
    -StartSeconds 102.5 -LengthSeconds 90 `
    -OutputFile (Join-Path $OutputDirectory "steel-relic-reel-boss.ogg")

New-SeamlessLoop `
    -InputFile (Join-Path $SourceDirectory "Ironfang Relics (1).mp3") `
    -StartSeconds 111.5 -LengthSeconds 90 `
    -OutputFile (Join-Path $OutputDirectory "ironfang-relics-region.ogg")

$impactSource = Join-Path $SourceDirectory "Steel Against Stone.mp3"
New-Effect -InputFile $impactSource `
    -Filter "atrim=start=0.015:end=0.72,asetpts=PTS-STARTPTS,highpass=f=90,lowpass=f=15500,afade=t=in:st=0:d=0.006,afade=t=out:st=0.52:d=0.18,volume=0.88,alimiter=limit=0.9" `
    -OutputFile (Join-Path $OutputDirectory "sword-impact-normal.ogg")

New-Effect -InputFile $impactSource `
    -Filter "atrim=start=0.01:end=0.82,asetpts=PTS-STARTPTS,highpass=f=55,lowpass=f=12000,equalizer=f=145:t=q:w=1:g=4,asetrate=43200,aresample=48000,afade=t=in:st=0:d=0.006,afade=t=out:st=0.64:d=0.23,volume=0.94,alimiter=limit=0.9" `
    -OutputFile (Join-Path $OutputDirectory "sword-impact-heavy.ogg")

New-Effect -InputFile $impactSource `
    -Filter "atrim=start=0.01:end=0.48,asetpts=PTS-STARTPTS,highpass=f=1150,lowpass=f=15000,afade=t=in:st=0:d=0.004,afade=t=out:st=0.30:d=0.17,volume=1.18,alimiter=limit=0.9" `
    -OutputFile (Join-Path $OutputDirectory "sword-whoosh-light.ogg")

Write-Host "CodeMaru runtime audio assets were generated in $OutputDirectory"
