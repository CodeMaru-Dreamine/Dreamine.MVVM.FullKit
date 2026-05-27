# DreamineVMS

WPF + Blazor Hybrid 기반 CCTV/VMS 실험 애플리케이션입니다.

## 현재 포함 범위

- WPF Shell
- Embedded Blazor Dashboard
- Blazor Server Dashboard (`http://localhost:6080`)
- 폰/브라우저 접속용 AnyIP 바인딩 옵션
- 3채널 이상 카메라 설정 기반 관리
- FFmpeg HLS 변환 서비스
- 인증서 만료일 모니터링
- win-acme 자동 갱신 작업 확인
- win-acme 수동 갱신 실행
- nginx reload 실행

## 인증서 모니터링

`Certificate` 탭에서 다음 항목을 확인할 수 있습니다.

- 인증서 폴더
- 선택된 인증서 파일
- 발급자 / 주체
- 만료일
- 남은 일수
- 상태: `Ok`, `Warning`, `Critical`, `Expired`, `Error`
- win-acme 예약 작업 상태
- 마지막 실행 시간 / 다음 실행 시간
- nginx reload 실행 결과

기본 인증서 폴더는 다음 경로입니다.

```txt
D:\win-acme\cctvviewer
```

사용자는 `Certificate` 탭에서 인증서 폴더, `wacs.exe`, `nginx.exe`, nginx 작업 폴더를 직접 수정할 수 있습니다. `Save Settings`를 누르면 실행 폴더의 `appsettings.local.json`에 저장됩니다.

## appsettings 예시

```json
{
  "CertificateMonitor": {
    "CertificateDirectory": "D:\\win-acme\\cctvviewer",
    "CertificateFilePatterns": [ "*.pem", "*.cer", "*.crt", "*.pfx" ],
    "PfxPassword": null,
    "WacsPath": "D:\\win-acme\\wacs.exe",
    "NginxPath": "C:\\nginx\\nginx.exe",
    "NginxWorkingDirectory": "C:\\nginx",
    "NginxReloadArguments": "-s reload",
    "WarningDays": 30,
    "CriticalDays": 15,
    "MaxCommandOutputChars": 6000
  }
}
```

## 운영 주의사항

- `Run Renew`는 일반 갱신 확인용입니다.
- `Force Renew`는 Let’s Encrypt 발급 제한에 걸릴 수 있으므로 반복 실행하지 마십시오.
- `Reload Nginx`는 nginx 경로와 작업 폴더가 맞아야 동작합니다.
- Windows 예약 작업 조회, 갱신 실행, nginx reload는 권한 문제로 실패할 수 있습니다. 필요한 경우 관리자 권한으로 실행하십시오.

## 권장 운영 흐름

1. `Certificate` 탭에서 `Refresh` 실행
2. 인증서 남은 기간 확인
3. `Check Task`로 win-acme 예약 작업 확인
4. 필요 시 `Run Renew`
5. 갱신 후 `Reload Nginx`
6. 다시 `Refresh`로 인증서 만료일 확인


## Blazor Server Routing

- `/` and `/live` open the Live View.
- `/dashboard` opens the Server Dashboard.
- The WPF `Server Live` tab loads `/live` first.
- Use the Dashboard button inside Live View to move to `/dashboard`.


## HLS script cache note

If `dreamineVmsHls.ensure` or another JS interop function is reported as undefined after an update, clear the WebView/browser cache or restart the app. The bundled scripts now use versioned URLs to avoid stale `hls-interop.js` files.
