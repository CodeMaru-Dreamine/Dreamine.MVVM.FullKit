# Wedding

> 지도, 갤러리, 방명록, 계좌 안내와 배경음악을 링크 하나에 담는 무료 모바일 청첩장 서비스입니다.

![.NET](https://img.shields.io/badge/.NET-net8.0--windows-512BD4) ![Version](https://img.shields.io/badge/version-1.0.0.0-2563EB) ![Source](https://img.shields.io/badge/source-open-16A34A)

[서비스 열기](https://wedding.codemaru.co.kr/) · [이용 설명서](https://codemaru.co.kr/guide/wedding) · [GitHub](https://github.com/CodeMaru-Dreamine)

## 프로젝트 소개

지도, 갤러리, 방명록, 계좌 안내와 배경음악을 링크 하나에 담는 무료 모바일 청첩장 서비스입니다.

청첩장 작성·관리, 공개 초대 페이지, 미디어 업로드와 하객 방명록 흐름을 제공합니다.

## 주요 기능

- OpenStreetMap 및 카카오·네이버 길찾기
- 사진 갤러리와 동영상·배경음악
- 하객 방명록과 CSV 관리
- 계좌번호 복사·카카오페이 링크
- OG 이미지 기반 카카오톡 미리보기

## 이용 순서

1. 로그인 후 신랑·신부, 일시와 장소를 입력합니다.
2. 사진·음악·동영상과 계좌 안내를 등록합니다.
3. 테마와 소개 문구를 확인합니다.
4. 생성된 링크를 카카오톡·문자·SNS로 공유합니다.

## 프로젝트 정보

| 항목 | 값 |
|---|---|
| 프로젝트 | WeddingPlatform.Web |
| 버전 | 1.0.0.0 |
| 대상 프레임워크 | net8.0-windows |
| 프로젝트 파일 | WeddingPlatform.Web.csproj |

## 개발 환경에서 실행

```powershell
dotnet run --project "WeddingPlatform.Web.csproj"
```

## API 문서 생성

```powershell
doxygen Doxyfile.kr
```
영문 문서는 `Doxyfile.en`으로 생성합니다.
