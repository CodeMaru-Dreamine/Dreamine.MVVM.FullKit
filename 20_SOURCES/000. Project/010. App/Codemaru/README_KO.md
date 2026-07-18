# CodeMaru · CardHybrid

> QR 코드, 모바일 랜딩 페이지, vCard 연락처 저장과 명함 디자인을 한 화면에서 제공하는 디지털 명함 서비스입니다.

![.NET](https://img.shields.io/badge/.NET-net8.0--windows-512BD4) ![Version](https://img.shields.io/badge/version-1.0.0.0-2563EB) ![Source](https://img.shields.io/badge/source-open-16A34A)

[서비스 열기](https://codemaru.co.kr/cardhybrid) · [이용 설명서](https://codemaru.co.kr/guide/cardhybrid) · [GitHub](https://github.com/CodeMaru-Dreamine)

## 프로젝트 소개

QR 코드, 모바일 랜딩 페이지, vCard 연락처 저장과 명함 디자인을 한 화면에서 제공하는 디지털 명함 서비스입니다.

CodeMaru 서비스 허브와 CardHybrid 편집기, 공개 랜딩 페이지, 인증·저장 흐름을 호스팅하는 애플리케이션입니다.

## 주요 기능

- SVG QR 코드와 모바일 랜딩 페이지 실시간 생성
- vCard 연락처 파일 내려받기
- 앞·뒤 명함 레이아웃과 색상·폰트 편집
- AI 로고 배경 제거와 SVG/HTML 내보내기
- 로그인 기반 명함 버전 저장·복원

## 이용 순서

1. CardHybrid 화면을 열고 이름·소속·연락처를 입력합니다.
2. 브랜드 색상과 로고, 명함 앞·뒤 레이아웃을 편집합니다.
3. 실시간 QR과 모바일 랜딩 페이지를 확인합니다.
4. SVG/HTML로 내보내거나 QR 링크를 공유합니다.
5. 로그인 사용자는 원하는 버전을 이력으로 저장합니다.

## 프로젝트 정보

| 항목 | 값 |
|---|---|
| 프로젝트 | Codemaru |
| 버전 | 1.0.0.0 |
| 대상 프레임워크 | net8.0-windows |
| 프로젝트 파일 | Codemaru.csproj |

## 개발 환경에서 실행

```powershell
dotnet run --project "Codemaru.csproj"
```

## API 문서 생성

```powershell
doxygen Doxyfile.kr
```
영문 문서는 `Doxyfile.en`으로 생성합니다.
