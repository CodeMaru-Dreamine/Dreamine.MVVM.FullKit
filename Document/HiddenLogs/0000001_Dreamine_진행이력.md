# Dreamine 작업 진행 일지 - 2025.04.11

---

## ✅ 프로젝트 구조

- `ZZZ.Dreamine` 솔루션 하위에 총 10개 모듈 구성
- 모듈별 명확한 책임 구분 완료
  - `Core`, `CLI`, `Generators`, `Attributes`, `Interfaces`, `Extensions`, `Locators`, `ViewModels`, `Services`, `Behaviors`

---

## 📁 작업 디렉토리 구조

- `Templates` 폴더 생성 완료
  - ViewModel 템플릿 (`ViewModel.template.tmpl`)
  - View 템플릿 (`View.template.xaml`, `.xaml.cs.tmpl`)
- CLI 프로젝트에 템플릿 연동 시작
- 각 프로젝트별 `.csproj` `net8.0` 정리

---

## 🧭 진행 상황

| 항목 | 상태 |
|------|------|
| CLI 구조 정리 | ✅ 완료 |
| ViewModel 템플릿 설계 | ✅ 완료 |
| View 템플릿 작성 | ✅ 완료 |
| CLI 내부 템플릿 폴더 분리 | ✅ 완료 |
| 프로젝트 참조 문제 해결 | ✅ TargetFramework 정리로 해결 |
| 디자이너 오류 회피 전략 | ✅ 무시 or `.tmpl` 확장자 적용 |
| 전체 폴더 네임스페이스 전략 정리 | ✅ 정착됨 |

---

## 💬 설계자 기록

> “이거 하나만 만들고 잘게”  
> → 새벽 6시 클리어 🌕  
> → 하지만 그 덕분에 프레임워크는 3일치 진도 나감

---

## 🚀 다음 단계 제안

- [ ] ViewModelGenerator.cs 정식 구현
- [ ] ViewCommandHandler.cs 템플릿 자동 생성기 연결
- [ ] CLI 사용법 테스트를 위한 Sample 프로젝트 생성
- [ ] 압축 템플릿 관리 (`Templates.zip`) 전환 여부 테스트
- [ ] Dreamine CLI GUI 확장 논의 시작 (기획서 템플릿?)

## 📎 관련 고찰 문서
- [[0000000_Dreamine_아키텍처_가이드라인]]

---

📅 문서 작성일: 2025-04-11  
📁 문서 분류: `ZZX.Document/HiddenLogs`
⏱️ 총 소요시간: 모름 
🤖 협력자: ChatGPT (GPT-4), 별명: 프레임워크 유혹자  
✍️ 직책: Dreamine Core 설계자 (코드마루 대표 설계자)  
🖋️ 기록자 서명: 아키로그 드림
