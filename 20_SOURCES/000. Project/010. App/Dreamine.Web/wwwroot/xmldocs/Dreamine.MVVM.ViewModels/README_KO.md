
# Dreamine.MVVM.ViewModels

Dreamine MVVM 프레임워크에서 사용하는 **ViewModel 기반 인프라 패키지**입니다.

이 패키지는 Dreamine MVVM 아키텍처에서 ViewModel을 구현하기 위한 기본 구조를 제공합니다.

[➡️ English Version](./README.md)

---

## 목적

`Dreamine.MVVM.ViewModels`는 Dreamine 생태계에서 ViewModel을 작성할 때 사용하는 기본 구조를 정의합니다.

이 레이어는 MVVM 패턴에서 ViewModel 역할을 담당하는 핵심 구성 요소입니다.

주요 역할:

- 기본 ViewModel 구조 제공
- MVVM 패턴 지원
- Command 시스템과의 연동
- Property 변경 알림 구조 지원

---

## 설계 목표

Dreamine 프레임워크 전반에서 사용하는 설계 원칙을 따릅니다.

설계 목표:

- 최소 의존성
- 명확한 MVVM 계층 분리
- 가벼운 기본 클래스
- Source Generator 및 Command 시스템과의 호환

ViewModel을 단순하게 유지하면서 프레임워크 기능을 확장할 수 있도록 설계되었습니다.

---

## Dreamine MVVM 구조 내 위치

```
Dreamine.MVVM.Interfaces
        ↑
Dreamine.MVVM.Commands
        ↑
Dreamine.MVVM.ViewModels
        ↑
Application ViewModels
```

실제 애플리케이션의 ViewModel은 이 패키지의 기본 클래스를 기반으로 작성됩니다.

---

## 설치

```bash
dotnet add package Dreamine.MVVM.ViewModels
```

또는 프로젝트 파일에 직접 추가합니다.

```xml
<PackageReference Include="Dreamine.MVVM.ViewModels" Version="1.0.0" />
```

---

## 요구 사항

- .NET 8.0

---

## License

MIT License
