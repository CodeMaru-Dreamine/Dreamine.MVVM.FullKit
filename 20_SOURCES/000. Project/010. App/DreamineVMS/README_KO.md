# DreamineVMS Agent

> Windows PC의 RTSP·USB 카메라 영상을 HLS로 변환해 원격 CCTV Viewer에 전달하는 데스크톱 에이전트입니다.

![.NET](https://img.shields.io/badge/.NET-net8.0--windows-512BD4) ![Version](https://img.shields.io/badge/version-1.0.0.0-2563EB) ![Source](https://img.shields.io/badge/source-open-16A34A)

[서비스 열기](https://cctvviewer.codemaru.co.kr/) · [이용 설명서](https://codemaru.co.kr/guide/cctv) · [GitHub](https://github.com/CodeMaru-Dreamine)

## 프로젝트 소개

Windows PC의 RTSP·USB 카메라 영상을 HLS로 변환해 원격 CCTV Viewer에 전달하는 데스크톱 에이전트입니다.

카메라 연결, FFmpeg 트랜스코딩, 계정·장치 등록과 스트림 상태 관리를 담당합니다.

## 주요 기능

- RTSP 및 USB 카메라 연결
- FFmpeg 기반 HLS 변환
- 다중 카메라와 표시 순서 관리
- 자동 재접속과 스트림 상태 확인
- 계정 기반 원격 서버 등록

## 이용 순서

1. Windows 10 이상 PC에 에이전트를 실행합니다.
2. CCTV Viewer 계정 정보를 입력해 장치를 등록합니다.
3. RTSP URL 또는 연결된 카메라를 추가합니다.
4. 웹 라이브 뷰에서 실시간 스트림을 확인합니다.

## 프로젝트 정보

| 항목 | 값 |
|---|---|
| 프로젝트 | DreamineVMS |
| 버전 | 1.0.0.0 |
| 대상 프레임워크 | net8.0-windows |
| 프로젝트 파일 | DreamineVMS.csproj |

## 개발 환경에서 실행

```powershell
dotnet run --project "DreamineVMS.csproj"
```

## API 문서 생성

```powershell
doxygen Doxyfile.kr
```
영문 문서는 `Doxyfile.en`으로 생성합니다.
