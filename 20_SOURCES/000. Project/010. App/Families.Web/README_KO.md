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
$env:Family__SuperAdminPassword = "로컬에서만-사용할-강한-비밀번호"
dotnet run --project "Families.Web.csproj"
```

## 관리자 접속

- 가족 앨범 관리자: `/{slug}/admin` — 앨범 생성 시 정한 비밀번호 또는 연결된 CodeMaru 계정으로 로그인합니다.
- 최고 관리자: `/admin` — `Family__SuperAdminPassword` 환경 변수로 설정한 비밀번호를 사용합니다.
- 저장소에는 샘플 앨범과 기본 최고 관리자 비밀번호가 포함되지 않습니다. 운영 비밀번호는 소스나 `appsettings.json`에 커밋하지 말고 배포 환경의 보안 변수로 설정하세요.

## 모바일 업로드 배포 점검

- 애플리케이션은 영상 파일 최대 2 GiB와 multipart 부가 데이터 32 MiB를 허용합니다. IIS Request Filtering, nginx, CDN, ingress 등 모든 앞단의 요청 본문 한도를 실제 테넌트 정책 한도와 multipart 여유분 이상으로 설정하세요.
- 프록시의 요청·전송·응답 대기 시간은 35분 이상으로 설정하세요. 프록시의 `413 Payload Too Large`나 타임아웃은 Families 업로드 엔드포인트에 도달하기 전에 발생하며, 휴대폰으로 촬영한 큰 영상에서만 문제처럼 보일 수 있습니다.
- 업로드 티켓은 한 번만 사용할 수 있으며 애플리케이션 메모리에 저장됩니다. 같은 호스트를 여러 Families 인스턴스가 처리한다면 티켓 발급과 업로드 요청에 고정 세션을 적용하거나 티켓 저장소를 공유 분산 저장소로 교체해야 합니다.
- 열람 비밀번호 요청 제한은 확인된 원격 주소를 사용합니다. 운영 경계에서는 전달된 주소 헤더를 명시적으로 신뢰한 프록시에서만 받아들이고, 프록시가 클라이언트가 보낸 `X-Forwarded-For`를 제거하거나 덮어쓴 뒤 실제 주소를 추가하도록 설정하며, Kestrel/원본 서버 직접 접근을 차단하세요. 그렇지 않으면 위조된 전달 주소로 클라이언트별 제한이 약화될 수 있습니다.

## 비공개 앨범 열람 권한

- 신규 앨범은 편집용 관리자 비밀번호와 방문자용 가족 열람 비밀번호를 분리합니다. 가족에게는 열람 비밀번호만 공유하세요.
- `ViewerPasswordHash`가 없는 기존 앨범 JSON은 관리자 비밀번호를 열람용으로 임시 재사용합니다. `/{slug}/admin`에서 별도 열람 비밀번호를 지정하고 저장하면 마이그레이션됩니다.
- 방문자 권한은 24시간 유효한 slug 전용 서명 HTTP-only 쿠키로 보관됩니다. `/family-data/{slug}`의 이미지와 동영상도 같은 권한으로 보호되며 다른 slug에 재사용할 수 없습니다.
- 다중 인스턴스 배포에서는 ASP.NET Core Data Protection 키를 영구 보관하고 공유해 모든 인스턴스가 같은 열람 쿠키를 검증하게 하세요.

## API 문서 생성

```powershell
doxygen Doxyfile.kr
```
영문 문서는 `Doxyfile.en`으로 생성합니다.
