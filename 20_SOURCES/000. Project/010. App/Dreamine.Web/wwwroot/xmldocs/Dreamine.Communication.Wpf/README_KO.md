# Dreamine.Communication.Wpf

`Dreamine.Communication.Wpf`는 Dreamine Communication 계열 패키지의 일부입니다.

이 패키지는 Dreamine Communication을 위한 WPF 전용 모니터링 및 진단 컴포넌트를 제공합니다.  
Core 통신 패키지들이 UI에 종속되지 않도록 기본 `Dreamine.Communication.FullKit` 패키지와 분리되어 있습니다.

[➡️ English Version](./README.md)

## 설명

Dreamine Communication을 위한 WPF 모니터링 및 진단 컴포넌트입니다.

## 주요 기능

- WPF 통신 모니터 `UserControl`
- 통신 채널 상태 표시
- 메시지 송신/수신 로그 표시
- 연결 상태 시각 표시
- 기본 진단 ViewModel 기반 구조

## 주요 구성 요소

### Views

- `CommunicationMonitorView`

### ViewModels

- `CommunicationMonitorViewModel`

### Models

- `CommunicationChannelViewItem`
- `CommunicationMessageLogItem`

### Converters

- `ConnectionStateBrushConverter`

### Commands

- `DelegateCommand`

## 사용 방법

모니터는 세 부분으로 구성됩니다.

| 구성 | 역할 |
|---|---|
| `CommunicationMonitorViewModel` | 채널과 로그를 보관합니다. 애플리케이션 코드는 `AddChannel`, `UpdateChannelState`, `AddSendLog`, `AddReceiveLog`를 호출합니다. |
| `CommunicationMonitorView` | 채널 상태와 송수신 로그를 렌더링하는 WPF `UserControl`입니다. `DataContext`에 `CommunicationMonitorViewModel`을 바인딩합니다. |
| `ConnectionStateBrushConverter` | 채널 상태 표시용 `ConnectionState` → Brush 변환기입니다. View가 내부적으로 사용합니다. |

### View에 모니터 배치

```xml
<UserControl x:Class="MyApp.Pages.CommunicationPage"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:commViews="clr-namespace:Dreamine.Communication.Wpf.Views;assembly=Dreamine.Communication.Wpf">
    <Grid>
        <commViews:CommunicationMonitorView
            DataContext="{Binding Monitor}" />
    </Grid>
</UserControl>
```

호스트 ViewModel은 `CommunicationMonitorViewModel` 인스턴스를 `Monitor` 속성으로 노출합니다.

### Transport와 연동

```csharp
using Dreamine.Communication.Abstractions.Enums;
using Dreamine.Communication.Abstractions.Models;
using Dreamine.Communication.Sockets.Clients;
using Dreamine.Communication.Sockets.Options;
using Dreamine.Communication.Wpf.ViewModels;

public sealed class CommunicationContext
{
    private readonly TcpClientTransport _client;

    public CommunicationMonitorViewModel Monitor { get; } = new();

    public CommunicationContext()
    {
        const string channelName = "TCP-Client";

        Monitor.AddChannel(
            channelName,
            TransportKind.Tcp,
            "TCP client to 127.0.0.1:15001.");

        _client = new TcpClientTransport(
            new TcpClientTransportOptions
            {
                Host = "127.0.0.1",
                Port = 15001
            });

        _client.MessageReceived += (_, message) =>
        {
            Monitor.AddReceiveLog(channelName, TransportKind.Tcp, message);
        };
    }

    public async Task ConnectAsync()
    {
        const string channelName = "TCP-Client";

        await _client.ConnectAsync();

        Monitor.UpdateChannelState(channelName, ConnectionState.Connected);
    }

    public async Task SendAsync(MessageEnvelope message)
    {
        const string channelName = "TCP-Client";

        Monitor.AddSendLog(channelName, TransportKind.Tcp, message);

        await _client.SendAsync(message);
    }
}
```

### 스레드 주의사항

`CommunicationMonitorViewModel`은 내부적으로 `ObservableCollection<T>`을 사용합니다. Transport 콜백은 보통 백그라운드 스레드에서 실행되므로, `MessageReceived` 이벤트 핸들러 같은 곳에서 `AddReceiveLog`, `AddSendLog`, `UpdateChannelState`를 호출할 때는 UI 스레드로 마샬링이 필요합니다 (예: `Application.Current.Dispatcher.Invoke`).

## 설계 원칙

- WPF UI 컴포넌트를 Core 통신 로직과 분리합니다.
- Sockets, Serial, RabbitMQ 같은 구체 전송 패키지를 직접 참조하지 않습니다.
- `Dreamine.Communication.Abstractions`와 `Dreamine.Communication.Core`에만 의존합니다.
- 단방향 의존성 흐름을 유지합니다.
- 이 패키지는 모니터링 및 진단 UI에만 집중합니다.

## 패키지 역할

```text
Dreamine.Communication.Abstractions
    ↑
Dreamine.Communication.Core
    ↑
Dreamine.Communication.Wpf
```

## 의존성

- `Dreamine.Communication.Abstractions`
- `Dreamine.Communication.Core`

## 대상 프레임워크

```text
net8.0-windows
```

## 참고

이 패키지는 `net8.0-windows`를 대상으로 하며 WPF를 사용합니다.

기본 `Dreamine.Communication.FullKit` 패키지에는 의도적으로 포함하지 않습니다.

## 관련 패키지

- `Dreamine.Communication.Abstractions`
- `Dreamine.Communication.Core`
- `Dreamine.Communication.Sockets`
- `Dreamine.Communication.Serial`
- `Dreamine.Communication.RabbitMQ`
- `Dreamine.Communication.FullKit`

## 라이선스

이 프로젝트는 MIT 라이선스를 따릅니다.
