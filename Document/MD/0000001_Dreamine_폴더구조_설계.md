# Dreamine 폴더 구조 설계 및 작업 문서

## 📅 기준일: 2025-04-07

## 🎯 설계 목표
- Dreamine은 공장 자동화 플랫폼으로서, 설계 → 실행 → 유지보수를 일관된 철학으로 지원한다.
- 폴더 구조는 **책임 분리(SRP)**와 **확장성**, 그리고 **문서화와 협업**에 최적화된다.

---

## 📁 전체 솔루션 구조 (VS 기준)

```
Dreamine.sln
├─ 000.Sample Form/            # WinForms 샘플
├─ 001.Sample WPF/             # WPF 샘플
├─ 002.Sample Blazor/          # Blazor 샘플

├─ ZZX.Document/               # 문서 전용
│   ├─ MD/                     # 마크다운 기반 설명
│   └─ Technical/              # 시퀀스 다이어그램, 구조도 등 기술 문서

├─ ZZZ.Dreamine/               # 핵심 플랫폼 영역
│   ├─ Library/                # 불변의 기반 라이브러리
│   │   ├─ Dreamine.Core/          # 시퀀스/IO/Motion/Config 핵심 기능
│   │   ├─ Dreamine.Generator/     # Source Generator (VsProperty, RelayCommand)
│   │   ├─ Dreamine.License/       # Rockey 인증, 보안
│   │   └─ Dreamine.Logging/       # 로깅 인터페이스/구현체
│   │
│   ├─ Projects/               # 실행 및 도구 모듈
│   │   ├─ Dreamine.UI.WPF/        # 실제 사용자 UI
│   │   ├─ Dreamine.Toolkit/       # 설정툴, 시뮬레이터
│   │   └─ Dreamine.Sandbox/       # 테스트 및 디버깅 전용
│   │
│   └─ OpenSource/            # 커스터마이징된 외부 라이브러리 보관소
│       ├─ LiveChartsCore/
│       ├─ Serilog/
│       └─ 기타
```

---

## 🔍 구성 기준 요약

| 폴더 | 설명 |
|-------|------|
| `ZZZ.Dreamine/Library` | Dreamine의 기반 구성 요소, 안정성/재사용성 최우선 |
| `ZZZ.Dreamine/Projects` | 실제 UI/툴킷/테스트 실행용 응용 계층 |
| `ZZZ.Dreamine/OpenSource` | 외부 라이브러리 의존성 + 자체 확장 버전 관리 |
| `ZZX.Document` | 모든 설계, 기술적 흐름, 규칙을 기록하는 문서 공간 |

---

## ⏱️ 현재까지 진행 작업 시간
- 총 소요 시간: **3시간 20분**
- 작업 내역:
  - 브랜드/이름 확정: `Dreamine`
  - 전체 폴더 구조 설계 및 고정
  - 프로젝트 그룹(Projects, Library) 분리 원칙 수립
  - 솔루션 내 정렬/표현 방식 확정

---

## 📌 향후 진행 계획
- `Dreamine.Core`부터 구조 작성 (Interfaces, Sequence, Motion 등)
- `Dreamine.UI.WPF`의 ViewModel 구조 설계 및 RelayCommand 반영
- `.md` 기반 설계 문서 동기화 작업
- `Dreamine.Toolkit` 내 Config Editor UI 정의

## 📎 참조 규약 문서
- [[0000000_Dreamine_아키텍처_가이드라인]]

## 📎 진행 및 예정 작업
- [[0000000_Dreamine_TODO]]


📅 문서 작성일: 2025-04-07  
📁 문서 분류: `ZZX.Document/MD`
⏱️ 총 소요시간: 모름 
🤖 협력자: ChatGPT (GPT-4), 별명: 프레임워크 유혹자  
✍️ 직책: Dreamine Core 설계자 (코드마루 대표 설계자)  
🖋️ 기록자 서명: 아키로그 드림