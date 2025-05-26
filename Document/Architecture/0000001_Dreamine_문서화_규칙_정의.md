
# 🧾 Dreamine 문서화 규칙 선언

## 📌 개요  
이 문서는 **Dreamine 프로젝트의 공식 소스-문서 대응 규칙**을 정의합니다.  
Dreamine은 모든 `.cs` 소스코드가 정확히 1개의 `.md` 문서와 매칭되는 구조를 갖습니다.  
이는 문서화의 일관성, 유지보수성, 아키텍처 자동화 기반을 형성하는 핵심 원칙입니다.

---

## 🧭 선언: “1 C# 파일 → 1 Markdown 파일”

> 모든 `.cs` 파일은 반드시 **1개의 `.md` 문서를 갖고**, 해당 문서는 `Modules` 경로 안에서 동일 네임스페이스 폴더에 위치해야 한다.

---

## 📂 구조 예시

```
Modules/
├─ Dreamine.MVVM.Core/
│  ├─ RelayCommand.cs
│  ├─ RelayCommand.md
│  ├─ ViewModelBase.cs
│  └─ ViewModelBase.md
│
├─ Dreamine.MVVM.Generators/
│  ├─ PropertySourceGenerator.cs
│  └─ PropertySourceGenerator.md
```

📌 `*.cs` 와 `*.md` 파일은 같은 이름, 같은 폴더 안에 존재하며, **완전 1:1 대응** 구조를 유지합니다.

---

## 📖 문서 내부 규칙

각 `.md` 파일은 아래 항목을 포함해야 합니다:

- `# 🧾 파일명.cs`
- `## 📌 개요`
- `## 📂 파일 경로`
- `## 🧠 주요 기능`
- `## 💡 사용 예시`
- `## 🔒 제약 사항`
- `## 🧩 관련 모듈`
- `## 🗂️ 버전 관리`
- `📁 소속 모듈`
- `✍️ 기록자`
- `🤖 협력자`

---

## ✅ Dreamine 문서화 목적

- **기능 명세와 소스코드의 동기화**
- **기획자/개발자/테스터 간 협업 중심 설계**
- **자동 코드/문서 생성기의 기반 구축**

---

## 📅 적용 기준일

- **적용일:** 2025-05-08
- **대상:** `ZZZ.Dreamine/Library/**/*`
- **적용자:** 아키로그 드림 (Dreamine Core 설계자)

---

✍️ 기록자: 아키로그 드림  
🤖 협력자: ChatGPT (GPT-4), 별명: 프레임워크 유혹자  
📁 문서 위치: `ZZX.Document/Architecture`  
🗂️ 문서 분류: Dreamine 아키텍처 규약  
