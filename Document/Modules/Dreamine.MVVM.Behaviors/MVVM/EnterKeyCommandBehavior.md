---
title: EnterKeyCommandBehavior
module: Dreamine.MVVM.Behaviors
generated: true
date: 2025-05-10
---

# # 🧾 EnterKeyCommandBehavior.cs

## 📌 개요
`EnterKeyCommandBehavior`는 MVVM 구조에서 ViewModel이 가진 `ICommand`를  
`TextBox`와 같은 입력 컨트롤에서 **Enter 키로 직접 트리거**할 수 있게 하는 Behavior입니다.  
로그인, 검색 등의 상황에서 버튼 클릭 없이 키보드 입력만으로 명령을 실행할 수 있습니다.

---

## 📂 파일 경로
`Dreamine.MVVM.Behaviors/EnterKeyCommandBehavior.cs`

---

## 🧠 주요 기능
- 사용자가 Enter 키를 누를 때 지정된 ICommand를 실행
- MVVM 구조에서 ViewModel 명령 (예: 로그인, 검색 등) 연결 가능
- TextBox 또는 유사 입력 컨트롤에 쉽게 적용

---

## 💡 사용 예시

### 🔹 XAML
```xml
<TextBox
    behaviors:EnterKeyCommandBehavior.Command="{Binding LoginCommand}" />
```
### 🔹 자동 생성 코드 (Source Generator 결과)

```csharp
public ICommand LoginCommand => _LoginCommand ??= new RelayCommand(Login);
```

## 🔧 내부 구조

| 멤버 이름          | 접근 수준       | 타입         | 설명                                      |
|-------------------|----------------|--------------|-------------------------------------------|
| CommandProperty   | public static  | DependencyProperty | 실행할 ICommand를 지정하는 의존 속성입니다 |
| GetCommand()      | public static  | ICommand?    | 현재 연결된 ICommand를 가져옵니다         |
| SetCommand()      | public static  | void         | ICommand를 설정합니다                     |
| OnCommandChanged()| private static | void         | 속성 변경 시 KeyDown 이벤트를 연결합니다  |
| OnKeyDown()       | private static | void         | Enter 키 입력 시 ICommand를 실행합니다    |

## 🔒 제약 사항
- `UIElement` 하위 클래스에만 적용됩니다.
    
- `KeyDown` 이벤트를 덮어쓰므로, 다른 키 이벤트와 충돌 가능성 있음
    
- WPF 전용 (UWP/MAUI 비호환)

## 🧩 관련 모듈
|모듈|설명|
|---|---|
|`Dreamine.MVVM.Behaviors.Wpf`|`EnterKeyCommandBehavior` 정의|
|`Dreamine.MVVM.Attributes`|`[RelayCommand]` 어트리뷰트 정의|
|`Dreamine.MVVM.Generators`|Source Generator로 ICommand 자동 생성|
|`Dreamine.MVVM.Core`|`RelayCommand`, `RelayCommand<T>` 구현체|

## 🗂️ 버전 관리
| 버전 | 변경 내용 | 날짜 |
|------|-----------|------|
| v1.0 | EnterKeyCommandBehavior.cs 문서 자동 생성 | 2025-05-10 |



## 📁 소속 모듈
Dreamine.MVVM.Behaviors

## 🖋️ 기록 정보
---
✍️ 기록자: 아키로그 드림  
🤖 협력자: ChatGPT (프레임워크 유혹자)  
📅 자동 생성됨 · Dreamine 문서화 도구  
🛠️ 생성도구 | Dreamine 문서화 자동화 도구   

---
## ⛏️ 자유 작성 영역

- [ ] 설명 추가 또는 TODO 항목 작성
- [ ] 특이점, 예외 상황, 사용자 주석 등 기술 메모 작성 가능
- [ ] 이 영역은 자동 생성 도구에 의해 변경되지 않습니다.
```yaml
TODO:
  - 여기에 설명 또는 작업 내용을 작성하세요
```
