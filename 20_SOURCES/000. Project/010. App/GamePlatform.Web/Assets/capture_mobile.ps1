param(
    [string]$BaseUrl = "http://127.0.0.1:6080",
    [string]$OutputPath = ".artifacts/maru-mobile-cdp.png",
    [int]$Port = 9224
)

$chromePath = "C:\Program Files\Google\Chrome\Application\chrome.exe"
$profilePath = Join-Path (Get-Location) ".artifacts\chrome-cdp-profile"
$absoluteOutput = [IO.Path]::GetFullPath((Join-Path (Get-Location) $OutputPath))
$outputDirectory = Split-Path -Parent $absoluteOutput
New-Item -ItemType Directory -Force -Path $profilePath, $outputDirectory | Out-Null

$chrome = Start-Process -FilePath $chromePath -WindowStyle Hidden -PassThru -ArgumentList @(
    "--headless=new",
    "--disable-gpu",
    "--hide-scrollbars",
    "--remote-debugging-port=$Port",
    "--user-data-dir=$profilePath",
    "about:blank"
)

$socket = [Net.WebSockets.ClientWebSocket]::new()
$messageId = 0

function Send-CdpCommand {
    param([string]$Method, [hashtable]$Parameters = @{})
    $script:messageId++
    $payload = @{ id = $script:messageId; method = $Method; params = $Parameters } | ConvertTo-Json -Compress -Depth 12
    $bytes = [Text.Encoding]::UTF8.GetBytes($payload)
    $segment = [ArraySegment[byte]]::new($bytes)
    $socket.SendAsync($segment, [Net.WebSockets.WebSocketMessageType]::Text, $true, [Threading.CancellationToken]::None).GetAwaiter().GetResult()

    while ($true) {
        $buffer = New-Object byte[] 1048576
        $received = [IO.MemoryStream]::new()
        do {
            $result = $socket.ReceiveAsync([ArraySegment[byte]]::new($buffer), [Threading.CancellationToken]::None).GetAwaiter().GetResult()
            $received.Write($buffer, 0, $result.Count)
        } while (-not $result.EndOfMessage)
        $json = [Text.Encoding]::UTF8.GetString($received.ToArray()) | ConvertFrom-Json
        if ($json.id -eq $script:messageId) { return $json }
    }
}

try {
    $deadline = [DateTime]::UtcNow.AddSeconds(10)
    do {
        try { $targets = Invoke-RestMethod "http://127.0.0.1:$Port/json"; break }
        catch { Start-Sleep -Milliseconds 150 }
    } while ([DateTime]::UtcNow -lt $deadline)

    if (-not $targets) { throw "Chrome debugging endpoint did not start." }
    $target = $targets | Where-Object type -eq "page" | Select-Object -First 1
    $socket.ConnectAsync([Uri]$target.webSocketDebuggerUrl, [Threading.CancellationToken]::None).GetAwaiter().GetResult()
    Send-CdpCommand "Page.enable" | Out-Null
    Send-CdpCommand "Runtime.enable" | Out-Null
    Send-CdpCommand "Emulation.setDeviceMetricsOverride" @{
        width = 360; height = 640; deviceScaleFactor = 1; mobile = $true; screenWidth = 360; screenHeight = 640
    } | Out-Null
    Send-CdpCommand "Page.navigate" @{ url = "$BaseUrl/_development/login" } | Out-Null
    Start-Sleep -Seconds 2
    Send-CdpCommand "Page.navigate" @{ url = "$BaseUrl/maru-idle/play" } | Out-Null
    Start-Sleep -Seconds 3

    $metrics = Send-CdpCommand "Runtime.evaluate" @{
        expression = "JSON.stringify({viewport:[innerWidth,innerHeight],scroll:[document.documentElement.scrollWidth,document.documentElement.scrollHeight],stage:(()=>{const r=document.querySelector('.mi-stage').getBoundingClientRect();return [r.width,r.height]})(),header:getComputedStyle(document.querySelector('.gp-site-chrome')).display})"
        returnByValue = $true
    }
    $capture = Send-CdpCommand "Page.captureScreenshot" @{ format = "png"; fromSurface = $true }
    [IO.File]::WriteAllBytes($absoluteOutput, [Convert]::FromBase64String($capture.result.data))
    $metrics.result.result.value
}
finally {
    if ($socket.State -eq [Net.WebSockets.WebSocketState]::Open) {
        $socket.CloseAsync([Net.WebSockets.WebSocketCloseStatus]::NormalClosure, "done", [Threading.CancellationToken]::None).GetAwaiter().GetResult()
    }
    $socket.Dispose()
    if (-not $chrome.HasExited) { Stop-Process -Id $chrome.Id -Force }
}
