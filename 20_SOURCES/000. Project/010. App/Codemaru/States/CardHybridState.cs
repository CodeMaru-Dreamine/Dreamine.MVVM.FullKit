using Codemaru.Models;

namespace Codemaru.States;

/// <summary>
/// \if KO
/// <para>Card Hybrid State 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates card hybrid state functionality and related state.</para>
/// \endif
/// </summary>
/// <param name="Profile">
/// \if KO
/// <para>표시할 카드 프로필입니다.</para>
/// \endif
/// \if EN
/// <para>The card profile to display.</para>
/// \endif
/// </param>
/// <param name="QrPayload">
/// \if KO
/// <para>QR 코드에 인코딩된 원본 페이로드입니다.</para>
/// \endif
/// \if EN
/// <para>The original payload encoded in the QR code.</para>
/// \endif
/// </param>
/// <param name="QrSvg">
/// \if KO
/// <para>렌더링할 QR 코드 SVG입니다.</para>
/// \endif
/// \if EN
/// <para>The QR-code SVG to render.</para>
/// \endif
/// </param>
/// <param name="History">
/// \if KO
/// <para>카드 변경 이력입니다.</para>
/// \endif
/// \if EN
/// <para>The card change history.</para>
/// \endif
/// </param>
/// <param name="LastUpdated">
/// \if KO
/// <para>상태가 마지막으로 갱신된 시각입니다.</para>
/// \endif
/// \if EN
/// <para>The time at which the state was last updated.</para>
/// \endif
/// </param>
public sealed record CardHybridState(
    CardProfile Profile,
    string QrPayload,
    string QrSvg,
    IReadOnlyList<CardHistoryEntry> History,
    DateTime LastUpdated)
{
    /// <summary>
    /// \if KO
    /// <para>Default 값을 생성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Creates the default value.</para>
    /// \endif
    /// </summary>
    /// <param name="qrSvg">
    /// \if KO
    /// <para>qr Svg에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for qr svg.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Create Default 작업에서 생성한 <c>CardHybridState</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CardHybridState</c> result produced by the create default operation.</para>
    /// \endif
    /// </returns>
    public static CardHybridState CreateDefault(string qrSvg)
    {
        var profile = CardProfile.Default;

        return Create(profile, qrSvg, Array.Empty<CardHistoryEntry>());
    }

    /// \fn CardHybridState Create(CardProfile profile, string qrSvg, IReadOnlyList<CardHistoryEntry> history)
    /// <summary>
    /// \if KO
    /// <para>값을 생성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Creates the value.</para>
    /// \endif
    /// </summary>
    /// <param name="profile">
    /// \if KO
    /// <para>profile에 사용할 <c>CardProfile</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CardProfile</c> value used for profile.</para>
    /// \endif
    /// </param>
    /// <param name="qrSvg">
    /// \if KO
    /// <para>qr Svg에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for qr svg.</para>
    /// \endif
    /// </param>
    /// <param name="history">
    /// \if KO
    /// <para>history에 사용할 <c>IReadOnlyList&lt;CardHistoryEntry&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IReadOnlyList&lt;CardHistoryEntry&gt;</c> value used for history.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Create 작업에서 생성한 <c>CardHybridState</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CardHybridState</c> result produced by the create operation.</para>
    /// \endif
    /// </returns>
    public static CardHybridState Create(CardProfile profile, string qrSvg, IReadOnlyList<CardHistoryEntry> history)
    {
        return new CardHybridState(
            Profile: profile,
            QrPayload: profile.LandingUrl,
            QrSvg: qrSvg,
            History: history,
            LastUpdated: DateTime.Now);
    }
}
