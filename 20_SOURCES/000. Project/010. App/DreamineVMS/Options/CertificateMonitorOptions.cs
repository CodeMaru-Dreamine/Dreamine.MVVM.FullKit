namespace DreamineVMS.Options;

/// <summary>
/// \brief 인증서 모니터링 및 갱신 실행 옵션입니다.
/// </summary>
public sealed class CertificateMonitorOptions
{
    /// <summary>\brief 인증서 파일이 저장된 기본 폴더입니다.</summary>
    public string CertificateDirectory { get; set; } = @"D:\win-acme\cctvviewer";

    /// <summary>\brief 인증서 파일 검색 패턴입니다.</summary>
    public string[] CertificateFilePatterns { get; set; } = ["*.pem", "*.cer", "*.crt", "*.pfx"];

    /// <summary>\brief PFX 인증서 암호입니다. 암호가 없으면 비워둡니다.</summary>
    public string? PfxPassword { get; set; }

    /// <summary>\brief win-acme 실행 파일 경로입니다.</summary>
    public string WacsPath { get; set; } = @"D:\win-acme\wacs.exe";

    /// <summary>\brief nginx 실행 파일 경로입니다.</summary>
    public string NginxPath { get; set; } = @"C:\nginx\nginx.exe";

    /// <summary>\brief nginx 작업 폴더입니다.</summary>
    public string NginxWorkingDirectory { get; set; } = @"C:\nginx";

    /// <summary>\brief nginx reload 인자입니다.</summary>
    public string NginxReloadArguments { get; set; } = "-s reload";

    /// <summary>\brief Warning 상태로 판단할 남은 일수입니다.</summary>
    public int WarningDays { get; set; } = 30;

    /// <summary>\brief Critical 상태로 판단할 남은 일수입니다.</summary>
    public int CriticalDays { get; set; } = 15;

    /// <summary>\brief 외부 명령 출력 최대 보관 길이입니다.</summary>
    public int MaxCommandOutputChars { get; set; } = 6000;
}
