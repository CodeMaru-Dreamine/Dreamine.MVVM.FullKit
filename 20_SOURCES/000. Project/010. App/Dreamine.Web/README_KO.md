# Dreamine

> WPF와 Blazor를 한 코드 흐름으로 연결하고 반복적인 MVVM 코드를 줄이는 오픈소스 FullKit 공식 웹 애플리케이션입니다.

![.NET](https://img.shields.io/badge/.NET-net8.0--windows-512BD4) ![Version](https://img.shields.io/badge/version-1.0.0.0-2563EB) ![Source](https://img.shields.io/badge/source-open-16A34A)

[서비스 열기](https://dreamine.kr/) · [이용 설명서](https://codemaru.co.kr/guide/dreamine) · [GitHub](https://github.com/CodeMaru-Dreamine)

## 프로젝트 소개

WPF와 Blazor를 한 코드 흐름으로 연결하고 반복적인 MVVM 코드를 줄이는 오픈소스 FullKit 공식 웹 애플리케이션입니다.

FullKit 소개, 패키지·예제 탐색, 지식 그래프와 개발 문서를 제공하는 공식 포털입니다.

## 주요 기능

- FullKit 패키지·레이어 소개
- 초보자용 레시피와 문제 해결 문서
- 프로젝트별 API·Doxygen 문서 진입점
- 전체 소스 지식 그래프
- GitHub·NuGet 공식 링크

## 이용 순서

1. 시작 페이지에서 5분 빠른 시작을 확인합니다.
2. 목적에 맞는 WPF·통신·PLC·Hybrid 레시피를 선택합니다.
3. API 페이지에서 클래스·메서드 계약을 확인합니다.
4. 구조가 필요하면 지식 그래프를 새 창에서 엽니다.

## 프로젝트 정보

| 항목 | 값 |
|---|---|
| 프로젝트 | Dreamine.Web |
| 버전 | 1.0.0.0 |
| 대상 프레임워크 | net8.0-windows |
| 프로젝트 파일 | Dreamine.Web.csproj |

## 개발 환경에서 실행

```powershell
dotnet run --project "Dreamine.Web.csproj"
```

## API 문서 생성

```powershell
doxygen Doxyfile.kr
```
영문 문서는 `Doxyfile.en`으로 생성합니다.
