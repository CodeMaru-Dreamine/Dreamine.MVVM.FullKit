namespace Codemaru.Options;

/// <summary>
/// \if KO
/// <para>\brief 인증서 모니터링 및 갱신 실행 옵션입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates certificate monitor options functionality and related state.</para>
/// \endif
/// </summary>
public sealed class CertificateMonitorOptions
{
    /// <summary>
    /// \if KO
    /// <para>\brief 인증서 파일이 저장된 기본 폴더입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the certificate directory value.</para>
    /// \endif
    /// </summary>
    public string CertificateDirectory { get; set; } = @"D:\win-acme\cctvviewer";

    /// <summary>
    /// \if KO
    /// <para>\brief 인증서 파일 검색 패턴입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the certificate file patterns value.</para>
    /// \endif
    /// </summary>
    public string[] CertificateFilePatterns { get; set; } = ["*.pem", "*.cer", "*.crt", "*.pfx"];

    /// <summary>
    /// \if KO
    /// <para>\brief PFX 인증서 암호입니다. 암호가 없으면 비워둡니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the pfx password value.</para>
    /// \endif
    /// </summary>
    public string? PfxPassword { get; set; }

    /// <summary>
    /// \if KO
    /// <para>\brief win-acme 실행 파일 경로입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the wacs path value.</para>
    /// \endif
    /// </summary>
    public string WacsPath { get; set; } = @"D:\win-acme\wacs.exe";

    /// <summary>
    /// \if KO
    /// <para>\brief nginx 실행 파일 경로입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the nginx path value.</para>
    /// \endif
    /// </summary>
    public string NginxPath { get; set; } = @"C:\nginx\nginx.exe";

    /// <summary>
    /// \if KO
    /// <para>\brief nginx 작업 폴더입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the nginx working directory value.</para>
    /// \endif
    /// </summary>
    public string NginxWorkingDirectory { get; set; } = @"C:\nginx";

    /// <summary>
    /// \if KO
    /// <para>\brief nginx reload 인자입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the nginx reload arguments value.</para>
    /// \endif
    /// </summary>
    public string NginxReloadArguments { get; set; } = "-s reload";

    /// <summary>
    /// \if KO
    /// <para>\brief Warning 상태로 판단할 남은 일수입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the warning days value.</para>
    /// \endif
    /// </summary>
    public int WarningDays { get; set; } = 30;

    /// <summary>
    /// \if KO
    /// <para>\brief Critical 상태로 판단할 남은 일수입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the critical days value.</para>
    /// \endif
    /// </summary>
    public int CriticalDays { get; set; } = 15;

    /// <summary>
    /// \if KO
    /// <para>\brief 외부 명령 출력 최대 보관 길이입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the max command output chars value.</para>
    /// \endif
    /// </summary>
    public int MaxCommandOutputChars { get; set; } = 6000;
}
