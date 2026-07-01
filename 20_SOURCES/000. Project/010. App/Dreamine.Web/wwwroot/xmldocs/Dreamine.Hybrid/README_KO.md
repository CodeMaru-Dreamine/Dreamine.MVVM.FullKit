# Dreamine.Hybrid

Dreamine 하이브리드 애플리케이션을 위한 코어 런타임 및 추상화 패키지입니다.

[➡️ English Version](README.md)

## 목적

`Dreamine.Hybrid`는 Host 애플리케이션과 Embedded UI 계층 사이에서 메시지와 상태를 공유하기 위한 플랫폼 중립 계약과 InMemory 구현을 제공합니다.

이 패키지는 WebView2 또는 Blazor를 직접 호스팅하지 않습니다. WPF 전용 호스팅은 `Dreamine.Hybrid.Wpf`에서 제공합니다.

## 주요 타입

- `IHybridMessage`
- `IHybridMessageBus`
- `IHybridStateStore`
- `HybridMessageBase`
- `InMemoryHybridMessageBus`
- `HybridStateStore`

앱별 메시지는 이 라이브러리 패키지가 아니라 애플리케이션 또는 샘플 프로젝트에 둡니다. 필요하면 `HybridMessageBase`를 상속하거나 `IHybridMessage`를 구현하세요.

## 패키지 경계

하이브리드 공통 계약 또는 단일 프로세스 내 메시지 버스가 필요할 때 이 패키지를 사용합니다.

WPF에서 BlazorWebView/WebView2를 호스팅하는 `HybridHostControl`이 필요하면 `Dreamine.Hybrid.Wpf`를 사용합니다.

## 라이선스

MIT License
