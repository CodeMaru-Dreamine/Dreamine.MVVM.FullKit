# ThankYou

> 결혼식 이후 감사 인사와 사진, 계좌 안내, 연락처를 모바일 페이지로 전달하는 감사장 서비스입니다.

![.NET](https://img.shields.io/badge/.NET-net8.0--windows-512BD4) ![Version](https://img.shields.io/badge/version-1.0.0.0-2563EB) ![Source](https://img.shields.io/badge/source-open-16A34A)

[서비스 열기](https://thankyou.codemaru.co.kr/) · [이용 설명서](https://codemaru.co.kr/guide/thankyou) · [GitHub](https://github.com/CodeMaru-Dreamine)

## 프로젝트 소개

결혼식 이후 감사 인사와 사진, 계좌 안내, 연락처를 모바일 페이지로 전달하는 감사장 서비스입니다.

Wedding 이후의 감사 메시지를 별도 주소로 작성·관리하고 하객에게 공유하는 흐름을 담당합니다.

## 주요 기능

- 모바일 감사 인사 페이지
- 본식·스냅 사진 갤러리
- 선택적 계좌 안내와 복사 버튼
- 청첩장과 분리된 공유 링크
- 감사장 전용 문구와 테마 편집

## 이용 순서

1. 신랑·신부 이름과 감사 인사말을 입력합니다.
2. 결혼식 사진과 대표 이미지를 올립니다.
3. 필요한 연락처·계좌·공유 문구를 정리합니다.
4. 감사장 링크를 하객에게 전달합니다.

## 프로젝트 정보

| 항목 | 값 |
|---|---|
| 프로젝트 | WeddingThankYou |
| 버전 | 1.0.0.0 |
| 대상 프레임워크 | net8.0-windows |
| 프로젝트 파일 | WeddingThankYou.csproj |

## 개발 환경에서 실행

```powershell
dotnet run --project "WeddingThankYou.csproj"
```

## API 문서 생성

```powershell
doxygen Doxyfile.kr
```
영문 문서는 `Doxyfile.en`으로 생성합니다.
