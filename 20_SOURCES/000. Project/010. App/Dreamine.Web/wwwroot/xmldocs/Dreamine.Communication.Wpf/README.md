# Dreamine.Communication.Wpf

`Dreamine.Communication.Wpf` is part of the Dreamine Communication package family.

This package provides WPF-specific monitoring and diagnostic components for Dreamine Communication.  
It is separated from the default `Dreamine.Communication.FullKit` package to keep the core communication packages UI-independent.

[➡️ 한국어 문서 보기](./README_KO.md)

## Description

WPF monitoring and diagnostic components for Dreamine Communication.

## Features

- WPF communication monitor `UserControl`
- Communication channel state display
- Message send/receive log display
- Connection state visual indicator
- Basic diagnostic view model foundation

## Main Components

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

## Usage

The monitor is composed of three parts:

| Part | Role |
|---|---|
| `CommunicationMonitorViewModel` | Holds channels and logs. Application code calls `AddChannel`, `UpdateChannelState`, `AddSendLog`, and `AddReceiveLog`. |
| `CommunicationMonitorView` | WPF `UserControl` that renders channel state and send/receive logs. Bind a `CommunicationMonitorViewModel` to its `DataContext`. |
| `ConnectionStateBrushConverter` | Maps `ConnectionState` to a brush for the channel state indicator. Used internally by the view. |

### Placing the monitor in a view

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

The host view model exposes a `CommunicationMonitorViewModel` instance as `Monitor`.

### Driving the monitor from transports

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

### Threading note

`CommunicationMonitorViewModel` uses `ObservableCollection<T>` internally. Transport callbacks usually run on a background thread, so calls into `AddReceiveLog`, `AddSendLog`, and `UpdateChannelState` must be marshaled to the UI thread (for example through `Application.Current.Dispatcher.Invoke`) when invoked from a transport's `MessageReceived` event.

## Design Principles

- Keep WPF UI components separated from core communication logic.
- Do not reference concrete transport packages such as Sockets, Serial, or RabbitMQ.
- Depend only on `Dreamine.Communication.Abstractions` and `Dreamine.Communication.Core`.
- Preserve one-way dependency flow.
- Keep this package focused on monitoring and diagnostics UI.

## Package Role

```text
Dreamine.Communication.Abstractions
    ↑
Dreamine.Communication.Core
    ↑
Dreamine.Communication.Wpf
```

## Dependencies

- `Dreamine.Communication.Abstractions`
- `Dreamine.Communication.Core`

## Target Framework

```text
net8.0-windows
```

## Note

This package targets `net8.0-windows` and uses WPF.

It is intentionally excluded from the default `Dreamine.Communication.FullKit` package.

## Related Packages

- `Dreamine.Communication.Abstractions`
- `Dreamine.Communication.Core`
- `Dreamine.Communication.Sockets`
- `Dreamine.Communication.Serial`
- `Dreamine.Communication.RabbitMQ`
- `Dreamine.Communication.FullKit`

## License

This project is licensed under the MIT License.
