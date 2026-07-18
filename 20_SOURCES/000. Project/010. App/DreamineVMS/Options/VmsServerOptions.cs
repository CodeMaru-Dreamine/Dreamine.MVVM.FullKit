namespace DreamineVMS.Options;

/// <summary>
/// \if KO
/// <para>\brief 모바일/브라우저 접속용 Blazor Server 옵션입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates vms server options functionality and related state.</para>
/// \endif
/// </summary>
public sealed class VmsServerOptions
{
    /// <summary>
    /// \if KO
    /// <para>\brief 서버 포트입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the port value.</para>
    /// \endif
    /// </summary>
    public int Port { get; set; } = 6080;

    /// <summary>
    /// \if KO
    /// <para>\brief AnyIP로 바인딩할지 여부입니다. 폰 접속에는 true가 필요합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the listen any ip value.</para>
    /// \endif
    /// </summary>
    public bool ListenAnyIp { get; set; } = true;
}
