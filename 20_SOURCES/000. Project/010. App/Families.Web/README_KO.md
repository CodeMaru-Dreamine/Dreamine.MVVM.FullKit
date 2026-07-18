# Families

> 사진, 동영상, 글, 댓글과 반응을 가족끼리만 공유하는 비공개 앨범·타임라인 서비스입니다.

![.NET](https://img.shields.io/badge/.NET-net8.0--windows-512BD4) ![Version](https://img.shields.io/badge/version-1.0.0.0-2563EB) ![Source](https://img.shields.io/badge/source-open-16A34A)

[서비스 열기](https://families.codemaru.co.kr/) · [이용 설명서](https://codemaru.co.kr/guide/families) · [GitHub](https://github.com/CodeMaru-Dreamine)

## 프로젝트 소개

사진, 동영상, 글, 댓글과 반응을 가족끼리만 공유하는 비공개 앨범·타임라인 서비스입니다.

가족 그룹 인증, 비공개 포스트·앨범, 댓글·반응과 미디어 제공을 담당하는 웹 애플리케이션입니다.

## 주요 기능

- 그룹 비밀번호 기반 비공개 접근
- 사진·영상·YouTube·Markdown 포스트
- 이벤트별 앨범 폴더
- 포스트 고정·댓글·이모지 반응
- 라이트·다크 테마와 그룹 커버

## 이용 순서

1. 가족 그룹을 만들고 비밀번호를 지정합니다.
2. 그룹 링크와 비밀번호를 가족에게 공유합니다.
3. 포스트 또는 앨범을 만들어 미디어와 이야기를 올립니다.
4. 댓글과 반응으로 가족 기록을 이어갑니다.

## 프로젝트 정보

| 항목 | 값 |
|---|---|
| 프로젝트 | Families.Web |
| 버전 | 1.0.0.0 |
| 대상 프레임워크 | net8.0-windows |
| 프로젝트 파일 | Families.Web.csproj |

## 개발 환경에서 실행

```powershell
dotnet run --project "Families.Web.csproj"
```

## API 문서 생성

```powershell
doxygen Doxyfile.kr
```
영문 문서는 `Doxyfile.en`으로 생성합니다.
