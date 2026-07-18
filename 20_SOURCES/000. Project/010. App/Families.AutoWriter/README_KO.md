# Families AutoWriter

> Families 콘텐츠를 반복 작업 없이 작성·등록하도록 돕는 자동화 작성 도구입니다.

![.NET](https://img.shields.io/badge/.NET-net8.0--windows-512BD4) ![Version](https://img.shields.io/badge/version-1.0.0.0-2563EB) ![Source](https://img.shields.io/badge/source-open-16A34A)

[서비스 열기](https://families.codemaru.co.kr/) · [이용 설명서](https://codemaru.co.kr/guide/families) · [GitHub](https://github.com/CodeMaru-Dreamine)

## 프로젝트 소개

Families 콘텐츠를 반복 작업 없이 작성·등록하도록 돕는 자동화 작성 도구입니다.

Families.Web의 포스트·미디어 입력을 보조하는 운영자용 작성 애플리케이션입니다.

## 주요 기능

- 가족 콘텐츠 초안 작성
- 미디어·본문 입력 자동화
- Families.Web 연계
- 운영자 중심 반복 작업 단축

## 이용 순서

1. 연결할 Families 환경을 확인합니다.
2. 게시할 본문과 미디어를 준비합니다.
3. 자동 작성 결과를 검토합니다.
4. Families.Web에서 최종 게시 상태를 확인합니다.

## 프로젝트 정보

| 항목 | 값 |
|---|---|
| 프로젝트 | Families.AutoWriter |
| 버전 | 1.0.0.0 |
| 대상 프레임워크 | net8.0-windows |
| 프로젝트 파일 | Families.AutoWriter.csproj |

## 개발 환경에서 실행

```powershell
dotnet run --project "Families.AutoWriter.csproj"
```

## API 문서 생성

```powershell
doxygen Doxyfile.kr
```
영문 문서는 `Doxyfile.en`으로 생성합니다.
