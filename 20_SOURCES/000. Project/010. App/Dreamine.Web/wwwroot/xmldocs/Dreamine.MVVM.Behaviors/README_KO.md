# Dreamine.MVVM.Behaviors

WPF 환경에서 **MVVM 기반 입력 처리와 포커스 제어**를 단순하게 구성하기 위한 경량 Behavior 패키지입니다.

이 라이브러리는 반복적으로 발생하는 UI 상호작용을 코드 비하인드 대신 **XAML + ICommand 중심 구조**로 정리하는 용도에 맞춰져 있습니다.

[➡️ English Version](README.md)

---

## 개요

`Dreamine.MVVM.Behaviors`는 WPF MVVM 애플리케이션에서 자주 필요한 UI 동작을 Behavior 형태로 분리한 모듈입니다.

현재 패키지는 다음 두 가지 동작을 제공합니다.

- 사용자가 **Enter 키**를 눌렀을 때 ViewModel의 명령 실행
- 컨트롤이 **로드된 직후 자동 포커스 설정**

이 모듈은 **.NET 8 WPF**를 대상으로 하며, Dreamine MVVM 생태계 안에서 사용할 수 있도록 구성되어 있습니다. 프로젝트 파일 기준 패키지 정보는 `Dreamine.MVVM.Behaviors`, 버전 `1.0.2`입니다.

---

## 포함 기능

### 1. `EnterKeyCommandBehavior`

`UIElement` 대상 Attached Behavior입니다.

역할:
- `KeyDown` 이벤트 감지
- `Key.Enter` 입력 확인
- 연결된 `ICommand` 실행
- 실행 후 이벤트 처리 완료 표시

사용 예:
- 로그인 입력창 엔터 실행
- 검색창 엔터 실행
- 텍스트 입력 후 확인 동작

동작 요약:
- Attached Property: `Command`
- Command Parameter: 현재 `null`
- `CanExecute(null)`가 `true`일 때만 실행

---

### 2. `FocusOnLoadedBehavior`

`FrameworkElement` 대상 Attached Behavior입니다.

역할:
- 컨트롤 로드 완료 시점 감지
- 대상 요소에 자동으로 키보드 포커스 부여

사용 예:
- 로그인 화면 첫 입력칸 포커스
- 검색창 자동 포커스
- 입력 폼 초기 커서 위치 지정

동작 요약:
- Attached Property: `IsEnabled`
- 아래 조건을 만족할 때만 포커스 적용
  - `Focusable`
  - `IsEnabled == true`
  - `Visibility == Visible`

---

## 설치

### NuGet

```bash
dotnet add package Dreamine.MVVM.Behaviors
```

### PackageReference

```xml
<PackageReference Include="Dreamine.MVVM.Behaviors" Version="1.0.2" />
```

---

## 요구사항

- .NET: `net8.0-windows`
- WPF 활성화 필요
- Dreamine Behavior 기반 의존성:
  - `Dreamine.MVVM.Behaviors.Core`

---

## 프로젝트 구조

```text
Dreamine.MVVM.Behaviors/
├─ Dreamine.MVVM.Behaviors.csproj
├─ MVVM/
│  ├─ EnterKeyCommandBehavior.cs
│  └─ FocusOnLoadedBehavior.cs
└─ README.md
```

---

## Quick Start

### EnterKeyCommandBehavior

```xml
<Window x:Class="Sample.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:mvvm="clr-namespace:Dreamine.MVVM.Behaviors.MVVM;assembly=Dreamine.MVVM.Behaviors">
    <Grid>
        <TextBox Width="240"
                 mvvm:EnterKeyCommandBehavior.Command="{Binding LoginCommand}" />
    </Grid>
</Window>
```

예시 ViewModel:

```csharp
public sealed class LoginViewModel
{
    public ICommand LoginCommand { get; }

    public LoginViewModel()
    {
        LoginCommand = new RelayCommand(OnLogin);
    }

    private void OnLogin()
    {
        // 로그인 처리
    }
}
```

---

### FocusOnLoadedBehavior

```xml
<Window x:Class="Sample.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:mvvm="clr-namespace:Dreamine.MVVM.Behaviors.MVVM;assembly=Dreamine.MVVM.Behaviors">
    <Grid>
        <TextBox Width="240"
                 mvvm:FocusOnLoadedBehavior.IsEnabled="True" />
    </Grid>
</Window>
```

---

## 설계 의도

이 패키지는 다음 방향을 따른다.

- Behavior를 작게 유지한다
- XAML 사용 방식을 명시적으로 유지한다
- 불필요한 추상화를 추가하지 않는다
- 코드 비하인드로 빠지는 UI 로직을 MVVM 구조 안으로 끌어온다

즉, 재사용 가능한 UI 모듈에서 반복되는 상호작용 코드를 표준화하는 데 적합하다.

---

## 정밀 분석 결과

압축 파일 내부 소스를 기준으로 확인된 내용은 다음과 같다.

1. 실제 포함된 Behavior는 **2개뿐**이다.
2. 기존 README에 적힌 이름은 실제 클래스명과 **일치하지 않는다**.
   - 잘못된 표기: `MVVMEntryCommandBehavior`
   - 실제 클래스명: `EnterKeyCommandBehavior`
   - 잘못된 표기: `MVVMFocusOnLoadedBehavior`
   - 실제 클래스명: `FocusOnLoadedBehavior`
3. 프로젝트 파일 기준 패키지 버전은 **1.0.2**이다.
4. `FocusOnLoadedBehavior`는 Attached Property 기반 `Loaded` 처리 경로 하나만 사용한다.

즉, README는 예전 문구가 아니라 **실제 코드 기준으로 다시 작성**하는 것이 맞다.

---

## 한계

현재 구현 기준 한계는 다음과 같다.

- `EnterKeyCommandBehavior`는 CommandParameter를 전달하지 않는다
- `EnterKeyCommandBehavior`는 `KeyDown`만 처리한다
- `FocusOnLoadedBehavior`는 Attached Property 경로를 통해 로드 시점 1회 포커스만 처리한다
- 이벤트 인자 전달, 지연 포커스, 선택적 포커스 라우팅 같은 고급 기능은 아직 없다

---

## 권장 개선 사항

- `CommandParameter` 지원 추가
- Enter 고정이 아니라 Key 선택형 옵션 추가
- Dispatcher 기반 지연 포커스 옵션 추가
- 텍스트 입력용 `SelectAllOnFocus` Behavior 추가
- XML 주석 / README / 패키지 메타데이터 동기화 정리

---

## 저장소 메타데이터

- Package ID: `Dreamine.MVVM.Behaviors`
- Version: `1.0.2`
- License: `MIT`
- Repository: `https://github.com/CodeMaru-Dreamine/Dreamine.MVVM.Behaviors`

---

## License

`LICENSE` 파일 참고.
