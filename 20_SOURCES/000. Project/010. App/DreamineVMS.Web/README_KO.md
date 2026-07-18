# CCTV Viewer

> DreamineVMS 에이전트가 제공하는 HLS 카메라 영상을 브라우저에서 관리·재생하는 원격 CCTV 웹 서비스입니다.

![.NET](https://img.shields.io/badge/.NET-net8.0-512BD4) ![Version](https://img.shields.io/badge/version-1.0.0.0-2563EB) ![Source](https://img.shields.io/badge/source-open-16A34A)

[서비스 열기](https://cctvviewer.codemaru.co.kr/) · [이용 설명서](https://codemaru.co.kr/guide/cctv) · [GitHub](https://github.com/CodeMaru-Dreamine)

## 프로젝트 소개

DreamineVMS 에이전트가 제공하는 HLS 카메라 영상을 브라우저에서 관리·재생하는 원격 CCTV 웹 서비스입니다.

사용자 인증, 장치·카메라 관리, 라이브 뷰, 공개 링크와 OG 메타데이터를 제공합니다.

## 주요 기능

- 브라우저 기반 실시간 HLS 재생
- 계정별 장치·다중 카메라 관리
- 로그인 없는 공개 라이브 링크
- 카메라별 OG 제목·설명·이미지
- PBKDF2 인증과 장기 세션

## 이용 순서

1. 서비스에서 계정을 생성합니다.
2. 카메라 PC에 DreamineVMS 에이전트를 연결합니다.
3. 카메라 관리 화면에서 스트림을 등록합니다.
4. 라이브 뷰 또는 공개 링크로 영상을 확인합니다.

## 프로젝트 정보

| 항목 | 값 |
|---|---|
| 프로젝트 | DreamineVMS.Web |
| 버전 | 1.0.0.0 |
| 대상 프레임워크 | net8.0 |
| 프로젝트 파일 | DreamineVMS.Web.csproj |

## 개발 환경에서 실행

```powershell
dotnet run --project "DreamineVMS.Web.csproj"
```

## API 문서 생성

```powershell
doxygen Doxyfile.kr
```
영문 문서는 `Doxyfile.en`으로 생성합니다.
