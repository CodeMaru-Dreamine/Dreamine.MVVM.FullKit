namespace DreamineVMS.Options;

/// <summary>
/// \brief 모바일/브라우저 접속용 Blazor Server 옵션입니다.
/// </summary>
public sealed class VmsServerOptions
{
    /// <summary>\brief 서버 포트입니다.</summary>
    public int Port { get; set; } = 6080;

    /// <summary>\brief AnyIP로 바인딩할지 여부입니다. 폰 접속에는 true가 필요합니다.</summary>
    public bool ListenAnyIp { get; set; } = true;
}
