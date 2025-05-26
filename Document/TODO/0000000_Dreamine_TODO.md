# Dreamine TODO 리스트 - 2025.04.11

---

## ✅ 오늘 기준 가장 급한 작업 목록

1. **템플릿 압축 파일로 관리하기**
   - Templates 폴더 내 `.xaml`, `.tmpl` 파일들을 ZIP 압축
   - CLI 실행 시 해당 ZIP 내에서 동적 로드 구현 필요
   - [ ] 압축 시 Build Action 제거 및 무시 처리
   - [ ] `TemplateManager.cs` 유틸 작성

2. **CLI 내부 경로 로직 수정**
   - 현재 템플릿 파일 경로가 변경됨
   - 압축된 구조 기준으로 `ExtractTemplate()` 경로 재설계 필요
   - [ ] 상대 경로 문제 보정
   - [ ] 파일 치환 후 저장 경로도 검토 필요

3. **샘플 프로젝트 구성**
   - 생성된 ViewModel + View가 잘 작동하는지 테스트할 샘플 필요
   - [ ] WPF 프로젝트 1개 생성
   - [ ] CLI 결과물 삽입 테스트
   - [ ] 버튼-Command 연동, TextBox-Property 연동 확인

---

## 🧠 고려 중이거나 추후 진행 예정

- Templates.zip 자동 업데이트 스크립트 작성
- 템플릿 파일 구조를 View, ViewModel, Model로 폴더 구분
- CLI 생성 명령어 확장 (예: `viewmodel`, `view`, `page`, `window`)
- 템플릿 치환 시 namespace 자동 추론 기능

---
📅 문서 작성일: 2025-04-11
📁 문서 분류: `ZZX.Document/TODO`
⏱️ 총 소요시간: 모름 
🤖 협력자: ChatGPT (GPT-4), 별명: 프레임워크 유혹자  
✍️ 직책: Dreamine Core 설계자 (코드마루 대표 설계자)  
🖋️ 기록자 서명: 아키로그 드림
