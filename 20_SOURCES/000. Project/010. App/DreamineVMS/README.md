# DreamineVMS

WPF + Blazor Hybrid 기반 CCTV/VMS 실험 애플리케이션입니다.

## 현재 포함 범위

- WPF Shell
- Embedded Blazor Dashboard
- Blazor Server Dashboard (`http://localhost:6080`)
- 폰/브라우저 접속용 AnyIP 바인딩 옵션
- 5채널 카메라 설정 구조
- FFmpeg RTSP → HLS 변환 서비스
- HLS Watchdog / 재시작 뼈대
- Blazor `/live` HLS 5채널 레이아웃

## 먼저 수정할 것

`appsettings.local.json`을 만들고 실제 RTSP 주소를 넣으십시오. `appsettings.json`에는 비밀번호를 커밋하지 마십시오.

```json
{
  "Ffmpeg": {
    "Path": "C:\\ffmpeg\\bin\\ffmpeg.exe",
    "StartOnApplicationStartup": true
  },
  "Cameras": [
    {
      "Id": "cam-051-main",
      "Name": "192.168.0.51 CH01",
      "Host": "192.168.0.51",
      "RtspUrl": "rtsp://USER:PASSWORD@192.168.0.51:554/REAL_PATH",
      "DisplayOrder": 1,
      "Enabled": true,
      "AutoReconnect": true
    }
  ]
}
```

## 접속

- 공개 테스트 주소: https://cctvviewer.codemaru.co.kr/
  - 로컬 Windows PC에서 직접 운영 중인 테스트 서버입니다.
  - 공개 테스트 서버는 상시 운영 서버가 아닌 개발/실험용 서버입니다.
  - PC 전원, 네트워크, 공유기 포트포워딩, Nginx/인증서 상태에 따라 접속이 일시적으로 불안정할 수 있습니다.
- WPF 내부 Embedded Dashboard: 앱 실행 시 자동 표시
- PC 브라우저: `http://localhost:6080`
- 휴대폰: `http://PC_IP:6080`
- Live View: `http://PC_IP:6080/live`

## 주의

- HLS 재생은 `hls.js` CDN을 사용합니다. 인터넷이 없는 내부망이면 `wwwroot/js`에 hls.js를 로컬로 넣고 스크립트 경로를 바꾸십시오.
- 기본 `appsettings.json`에는 실제 계정/비밀번호를 넣지 않았습니다.
- WPF 쪽 실제 RTSP 직접 재생은 아직 Placeholder입니다. PC 로컬 관제는 이후 LibVLCSharp로 붙이는 것을 권장합니다.
