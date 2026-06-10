# Dreamine FullKit 테스트

이 폴더는 Dreamine MVVM FullKit 라이브러리의 자동화 테스트를 담습니다.

## 프로젝트

- `Dreamine.FullKit.Tests`: 순수 라이브러리 코드를 검증하는 크로스 플랫폼 `net8.0` 테스트입니다.
- `Dreamine.FullKit.Wpf.Tests`: UI 보조 클래스, 커맨드, 컨버터, 뷰모델을 검증하는 Windows/WPF 전용 `net8.0-windows` 테스트입니다.

## 범위

우선순위는 빠르고 결정적인 public 동작 단위 테스트입니다.

- Attribute 메타데이터와 단순 모델/옵션 기본값
- Dependency Injection과 ViewModel locator 동작
- Communication framing, serialization, queue, routing, protocol adapter
- PLC/IO 주소, 프로토콜, 메모리, result 처리
- Logging store, formatter, sink, service 동작
- Threading policy와 모델 동작
- 실제 창을 띄우지 않고 검증 가능한 WPF command/converter/view-model 로직

물리 장비, COM 컴포넌트, 실제 소켓, RabbitMQ 서버, 표시되는 WPF 창이 필요한 테스트는 fake로 분리하거나 추후 integration test 프로젝트에서 다룹니다.

## 실행

`20_SOURCES`에서 실행합니다.

```powershell
dotnet test DreamineWorkSpace.sln
```

순수 .NET 테스트만 실행:

```powershell
dotnet test "200. Tests\Dreamine.FullKit.Tests\Dreamine.FullKit.Tests.csproj"
```

WPF/Windows 테스트만 실행:

```powershell
dotnet test "200. Tests\Dreamine.FullKit.Wpf.Tests\Dreamine.FullKit.Wpf.Tests.csproj"
```
