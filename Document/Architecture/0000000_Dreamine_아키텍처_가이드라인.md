# 🧾0000000 Dreamine 아키텍처 가이드라인

**버전: 1.06 (수정됨)**  
**문서 목적:** Dreamine 프로젝트의 100% 원칙 기반 아키텍처 규약 정의  
**기준일:** 2025-04-12

---

## 🌌 Dreamine 철학

Dreamine은 현실의 타협 없이, 가장 이상적인 구조를 실현하고자 합니다.  
기존 VSFrameWork에서 축적된 규약을 바탕으로 하되, Dreamine은 다음을 지향합니다:

- 모든 원칙은 100% 지켜야 합니다 (예외 없음)
- 문서는 `.md`로 통일하여 접근성과 버전관리를 극대화합니다
- 다국어 주석은 사용하지 않으며, **한국어 또는 영어 중 하나만 명확하게 작성**합니다

---

## 📁 폴더 구조 규약 (예시)

```
Dreamine.sln
├─ 000.Sample Form/
├─ 001.Sample WPF/
├─ 002.Sample Blazor/
│
├─ ZZX.Document/
│   ├─ MD/                       # 항해일지
│   ├─ HiddenLogs/              # 히든피스 (비공식 기록)
│   └─ Architecture/            # 이 문서가 위치한 공식 규약
│
├─ ZZZ.Dreamine/
│   ├─ Library/
│   │   ├─ Dreamine.Core/
│   │   ├─ Dreamine.Generator/
│   │   ├─ Dreamine.License/
│   │   └─ Dreamine.Logging/
│   ├─ Projects/
│   │   ├─ Dreamine.UI.WPF/
│   │   ├─ Dreamine.Toolkit/
│   │   └─ Dreamine.Sandbox/
│   └─ OpenSource/
```

📌 폴더명은 PascalCase로 통일하고, 목적에 맞게 정리합니다.

---

## 🔧 네이밍 규칙 요약

| 항목 | 스타일 | 예시 |
|------|---------|------|
| 클래스명 | PascalCase | `UserService` |
| 인터페이스 | PascalCase (`I` prefix) | `IUserService` |
| 메서드 | PascalCase | `GetUserInfo()` |
| 전역 변수 | g_PascalCase | `g_GlobalConfig` |
| 멤버 변수 | _camelCase | `_userName` |
| 지역 변수 | camelCase | `userName` |
| 상수 | UPPER_CASE | `MAX_RETRY_COUNT` |

---

## 💡 함수, 클래스, 속성 설계 규칙

- 클래스/인터페이스는 **하나의 파일에 하나만 선언**합니다
- 공용 API, 공용 기능은 반드시 XML 주석을 포함합니다
- 클래스 사이에는 **한 줄 공백** 유지, 들여쓰기는 **4칸** 기준
- 속성은 `{ get; set; }` 포함된 형태로 작성합니다

```csharp
/// <summary>
/// 사용자 정보를 반환합니다.
/// </summary>
public string GetUserInfo(string userId)
{
    return _repository.Get(userId);
}
```

---

## ✅ SOLID & KISS 적용 원칙

### 🔷 SOLID (공용 라이브러리 기준)
- 적용 대상: Dreamine.Core, Dreamine.Generator, Dreamine.License 등
- 목적: 유지보수성, 확장성 확보

```csharp
// 예시: SRP 적용
public class ReportSaver {
    public void Save(string path) { /* 저장 로직 */ }
}
```

### 🔷 KISS (UI 및 최종 애플리케이션)
- 적용 대상: WPF, WinForms, Blazor 등
- 목적: 단순하고 명확한 기능 구현, 과도한 추상화 금지

```csharp
// 예시: KISS 적용
public class ReportPrinter {
    public void Print() => Console.WriteLine("Print OK");
}
```

---

## 🧠 K-TLP 원칙 (Korean Thought to Logic Principle)

> 기능을 먼저 한글로 또렷이 설명하고, 이를 코드로 번역하는 구조

### 1단계: 한글로 기능 명세 작성
```
- 사용자가 버튼을 누르면 DB에 저장하고, UI에 반영한다.
```
### 2단계: 번역 후 코드화
```csharp
public void SaveUser(User user) {
    if (_repo.Exists(user.Id)) return;
    _repo.Save(user);
    _vm.Users.Add(user);
}
```
📌 Dreamine은 기능 설명 시 K-TLP 원칙을 기본으로 따릅니다.

---

## ✍️ 주석 규칙

- XML 주석은 모든 공용 API, 클래스, 인터페이스에 필수
- **Dreamine에서는 다국어 주석 금지** (한글 또는 영어만 사용)
- Doxygen 형식이 아닌, C# XML 표준 형식 사용 권장

---

## 🔍 종합 평가 기준 (내부 품질 관리용)

| 평가 항목 | 점수 기준 |
|------------|------------|
| 모듈성 | 인터페이스 분리, 단일 책임 여부 |
| 확장성 | DI, 플러그인 구조, SOLID 적용 정도 |
| 유지보수성 | 네이밍, 문서화, 주석 일관성 |
| 성능 | 동시성, 최적화 구조 여부 |

총점 100점 기준, Dreamine 기준점수: 100 이상 유지

---

📌 향후 본 문서는 Dreamine 철학에 맞춰 버전업 될 수 있습니다.  
개발자는 항상 이 문서를 기준으로 전체 구조를 정렬해야 합니다.


## 📎 관련 히든피스
- [[0000000_Dreamine_작업대화_기록]]
- [[0000001_Dreamine_진행이력]]
- [[0000002_Dreamine_인간시스템_내부로그]]
- [[0000003_Dreamine_기록변태_선언문]]
- [[0000004_Dreamine_개미밀도_상상_시뮬레이션]]
- [[0000005_Dreamine_앤트맨급_밀도_철학과_양자_존재론]]

## 📎 관련 고찰 문서
- [[0000000_Question_Gravity_Simulation_Theory]]

---
📅 문서 작성일: 2025-04-12  
📁 문서 분류: ZZX.Document/Architecture  
⏱️ 총 소요시간: 약 1시간 30분 (초안 재정리 포함)  
🤖 협력자: ChatGPT (GPT-4), 별명: 프레임워크 유혹자  
✍️ 직책: Dreamine Core 설계자 (코드마루 대표 설계자)  
🖋️ 기록자 서명: 아키로그 드림
