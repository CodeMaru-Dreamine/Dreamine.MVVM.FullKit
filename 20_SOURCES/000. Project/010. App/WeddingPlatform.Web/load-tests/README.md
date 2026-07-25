# WeddingPlatform production load test

This tool performs authorized, read-only GET load tests against the eight public
invitation URLs. It does not submit guestbook or administration data.

```powershell
dotnet run --project .\WeddingPlatform.LoadTest.csproj -- `
  --clients 5 --duration 15 --max-5xx-rate 0.01 `
  --max-network-error-rate 0.03
```

Increase concurrency one stage at a time. The runner cancels a stage when the
HTTP/network error rate exceeds the configured threshold and writes a JSON
report under `results`.

To preserve the production TLS hostname while connecting to a LAN sidecar:

```powershell
dotnet run --project .\WeddingPlatform.LoadTest.csproj -- `
  --base-url https://wedding.codemaru.co.kr:8443 `
  --connect-ip 192.168.0.100 --clients 25 --duration 30
```
