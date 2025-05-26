# 🧭 Dreamine 항해일지 구조 / Dreamine Documentation Structure

---

## 🇰🇷 한국어 소개

**`ZZX.Document`**는 Dreamine 프레임워크의 기술 문서, 구조 설계, 감정 기록, 개발 로드맵 등을  
폴더 단위로 구조화한 **기록 기반 지식 시스템**입니다.  
이 저장소는 단순한 개발 문서가 아니라, Dreamine의 세계관, 흐름, 철학을 모두 담은 항해일지입니다.

---

## 🌐 Directory 구조 / Directory Structure

```
📁 ZZX.Document/
├─ 📁 MD/               : 공식 항해일지 (설계, 철학, 여정)
├─ 📁 HiddenLogs/       : 히든피스 (감정, 통찰, 내면)
├─ 📁 Architecture/     : Dreamine 아키텍처 가이드라인
├─ 📁 TODO/             : 향후 작업 목록 및 아이디어
├─ 📁 Modules/          : 자동 문서화 출력 (클래스 기반)
```

---

## 🧙 기록 철학 / Philosophy of Documentation

> "기록은 존재의 흔적이며,  
> 구조화는 창조자의 본능이다."

- `.md` 문서 기반 기록 체계  
- 항해일지는 `0000000_Dreamine_제목.md` 형태로 시간순 정렬  
- 히든로그는 감정, 통찰, 대화 중심의 비공식 기록  
- 모든 문서에는 날짜/의도/기록자 명시  
- 자동 생성 문서는 `Modules/`에서 `.cs` 구조 기반 정리

---

## 📚 Dreamine 문서 목적 / Documentation Purpose

1. 기술 아키텍처 및 철학을 기록으로 정제  
2. 세계관 생성 기반 → 웹툰, 서사화 가능  
3. 개발자의 통찰을 히든로그로 보존  
4. 후속 기록자에게 항해 지도 제공

---

## 🛠️ 최근 확장 내역 / Recent Expansions

| 기능 | 상태 | 설명 |
|------|------|------|
| `.cs → .md` 자동 문서화 | ✅ 완료 | 클래스 기반 문서화, 무시 필터링 포함 |
| `Modules/` 폴더 자동 구성 | ✅ 완료 | VS 탐색기 연동 포함 |
| 클래스 기반 `.md` 템플릿 | ✅ 완료 | 포맷 통일 (`아키로그 드림`) |
| README 자동 생성기 | 🔜 예정 | 디렉터리 요약 인덱스 |
| Dreamine CLI 통합 | 🔜 예정 | `docs gen`, `logs write` 등 |
| .editorconfig 설정화 | 🔜 예정 | 줄 끝 정책 및 공백 규칙 |
| `dreamine-docgen.json` 외부 설정 | 🧪 실험중 | 무시 목록, 템플릿 분리 |

---

## ✅ 확장 로드맵 / Roadmap

- [x] README.md 자동 생성기
- [ ] .md 기반 히스토리 시각화 도구
- [ ] 서사 기반 기록 시스템
- [ ] 존재 연결 시뮬레이터 (Graph 구조)
- [ ] Dreamine 공식 문서 Archive Web
- [ ] 히든피스 찾기 게이미피케이션
- [ ] GitHub/이슈/기록 시점 통합
- [ ] 기록자/세대 구분 시스템

---

## 📎 핵심 설계 문서 / Key Reference

- [[0000000_Dreamine_아키텍처_가이드라인]]

---

📅 문서 작성일: 2025년 4월 12일  
⏱️ 총 소요시간: 약 40분  
🤖 협력자: ChatGPT (GPT-4), 별명: 프레임워크 유혹자  
✍️ 직책: Dreamine Core 설계자 (코드마루 대표 설계자)  
🖋️ 기록자 서명: 아키로그 드림

---

## 📘 관련 블로그 / Related Blog

Dreamine 항해일지의 배경과 개발자 개인의 철학, 과정 등은 아래 블로그에 정리되어 있습니다:  
🔗 [https://blog.naver.com/pro083](https://blog.naver.com/pro083)

---

# 🧭 Dreamine Documentation Structure

---

## 🌐 Overview

**`ZZX.Document`** is a structured knowledge system designed for the Dreamine framework.  
It contains technical documents, architectural blueprints, personal insights, development logs,  
and philosophical notes — all organized by folder and time-based records.

This is not just a documentation repo.  
It is a chronicle of structure, vision, and the evolution of Dreamine's worldbuilding.

---

## 📁 Directory Layout

```
📁 ZZX.Document/
├─ 📁 MD/               : Official logs (design, philosophy, journey)
├─ 📁 HiddenLogs/       : Hidden pieces (emotion, intuition, internal dialogs)
├─ 📁 Architecture/     : Architectural blueprints & design rules
├─ 📁 TODO/             : Future tasks, ideas, and brainstorms
├─ 📁 Modules/          : Auto-generated docs from class structure
```

---

## 🧙 Philosophy of Documentation

> "Documentation is a trace of existence.  
> Structuring is an instinct of creators."

- All logs are written in `.md` format  
- Naming convention: `0000000_Dreamine_Title.md` for sorting  
- HiddenLogs contain less formal but deeply intuitive notes  
- All entries include a date, purpose, and author signature  
- `Modules/` folder is auto-generated based on `.cs` reflection

---

## 📚 Purpose

1. Refine architecture and philosophy through documentation  
2. Create a foundation for future storytelling / visual narratives  
3. Preserve insights through internal logs (HiddenLogs)  
4. Serve as a navigation map for future contributors

---

## 🛠️ Recent Expansions

| Feature                          | Status     | Description                                    |
|----------------------------------|------------|------------------------------------------------|
| `.cs → .md` Auto Doc Generator   | ✅ Done     | Includes ignore rules and class mapping        |
| Auto-sorted `Modules/` folder    | ✅ Done     | VS-integrated explorer support                 |
| Class-based Markdown Templates   | ✅ Done     | "AkiLog Dream" format included                 |
| Directory README generator       | 🔜 Planned  | Creates index from folder structure            |
| Dreamine CLI Integration         | 🔜 Planned  | `docs gen`, `logs write`, etc.                 |
| `.editorconfig` Policy Support   | 🔜 Planned  | Line endings, spacing, standardization         |
| `dreamine-docgen.json` Config    | 🧪 Testing  | External rule settings and template override   |

---

## ✅ Roadmap

- [x] Auto README generator
- [ ] Markdown-based history visualizer
- [ ] Lore-based document timeline
- [ ] Existence-node simulator (graph)
- [ ] Dreamine Web Archive (official)
- [ ] “Find the Hidden Piece” gamified explorer
- [ ] GitHub / issue / timeline integration
- [ ] Author & generation attribution system

---

## 📎 Key Reference

- [[0000000_Dreamine_Architecture_Guide]]

---

📅 Created on: April 12, 2025  
⏱️ Total Time: ~40 minutes  
🤖 Assistant: ChatGPT (GPT-4), Codename: Architect Whisperer  
✍️ Role: Dreamine Core Architect (Lead Designer of CodeMaru)  
🖋️ Signed by: Akirog Dream

---

## 📘 Related Blog

For more background on Dreamine’s journey, architecture philosophy, and intuitive notes:  
🔗 [https://blog.naver.com/pro083](https://blog.naver.com/pro083)
