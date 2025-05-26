# Dreamine.MVVM.CLI 개발 이력 요약

**생성일:** 2025-04-08  
**작성자:** GPT-4o + 파트너  
**목표:** Dreamine 프레임워크 기반 MVVM 뷰/뷰모델 자동 생성기 및 CLI 제작

---

## ✅ 1. CLI 프로젝트 구성

**프로젝트명:** `Dreamine.MVVM.CLI`  
**타겟 프레임워크:** .NET 6.0 (Console)

**폴더 구성:**
```
Dreamine.MVVM.CLI/
├── Interfaces/
│   └── ICommandHandler.cs
├── Services/
│   ├── ViewCommandHandler.cs
│   └── ViewModelGenerator.cs
├── Templates/
│   ├── View.template.xaml
│   ├── View.template.xaml.cs.tmpl
│   └── ViewModel.template.tmpl
├── Program.cs
```

---

## 🧠 2. 주요 기능

- `dotnet run -- new view MainWindow` 또는 `.exe` 실행 방식 지원
- `View`, `View.xaml.cs`, `ViewModel.cs` 3가지 파일 자동 생성
- 템플릿 기반 `{name}` 바인딩 처리
- 자동 중복 삭제 (`MainWindow.xaml`, `.xaml.cs` 루트 위치)

**향후 지원 예정:**
- `new axis UnitName AxisName` (축 시퀀스 클래스 생성)
- `new project ProjectName` (전체 MVVM 프로젝트 구조 자동 생성)

---

## 🛠 3. 템플릿 바인딩 예시

```
public partial class {name}ViewModel : ViewModelBase
{
    [Property]
    private string _title = "{name} ViewModel";

    [RelayCommand]
    private void Submit()
    {
        Debug.WriteLine($"[{name}] Submit clicked: {Title}");
    }
}
```

---

## 🔄 4. CLI 배포 자동화

**배치 실행 예:**
```bat
@echo off
setlocal
set /p viewName=생성할 View 이름:
cd /d D:\Work\Dreamine\001.Sample WPF\Sample002
"..\..\CLI\Dreamine.MVVM.CLI.exe" new view %viewName%
pause
endlocal
```

---

## 🧪 5. 테스트 결과

- ✅ CLI 경로 내 Templates 폴더 구조 적용 성공
- ✅ `.xaml`, `.cs`, `.cs` 생성 확인
- ✅ `Sample002` 프로젝트에서 CLI 실행하여 자동 생성 성공
- ✅ `.exe` 파일 복사 배포 후 경로 기반 CLI 실행 테스트 성공

---

## 💬 6. 향후 과제

- [ ] 축 시퀀스용 템플릿 (`Axis.template.cs`)
- [ ] 프로젝트 전체 자동 생성 (`new project ...`)
- [ ] GUI 버전 `Dreamine.TemplateStudio` 제작
- [ ] 템플릿 Preview & Rename 기능

---

## 🧙 요약

Dreamine CLI는 단순 템플릿 생성기를 넘어서  
**FA 장비용 MVVM 프로젝트를 구조 째로 자동화**하는 마법 도구입니다.

## 📎 참조 규약 문서
- [[0000000_Dreamine_아키텍처_가이드라인]]

## 📎 진행 및 예정 작업
- [[0000000_Dreamine_TODO]]

📅 문서 작성일: 2025-04-09 
📁 문서 분류: `ZZX.Document/MD`
⏱️ 총 소요시간: 모름 
🤖 협력자: ChatGPT (GPT-4), 별명: 프레임워크 유혹자  
✍️ 직책: Dreamine Core 설계자 (코드마루 대표 설계자)  
🖋️ 기록자 서명: 아키로그 드림