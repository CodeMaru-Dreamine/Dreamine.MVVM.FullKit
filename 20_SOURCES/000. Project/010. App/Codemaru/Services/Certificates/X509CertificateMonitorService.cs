using Codemaru.Models.Certificates;
using Codemaru.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Codemaru.Services.Certificates;

/// <summary>
/// \if KO
/// <para>\brief 파일 시스템의 X509 인증서를 읽어 만료 상태를 계산합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates x509 certificate monitor service functionality and related state.</para>
/// \endif
/// </summary>
public sealed class X509CertificateMonitorService : ICertificateMonitorService
{
    /// <summary>
    /// \if KO
    /// <para>Status Async 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the status async value.</para>
    /// \endif
    /// </summary>
    /// <param name="options">
    /// \if KO
    /// <para>동작을 구성하는 설정입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The options that configure the operation.</para>
    /// \endif
    /// </param>
    /// <param name="cancellationToken">
    /// \if KO
    /// <para>취소 요청을 감시하는 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get Status Async 작업에서 생성한 <c>Task&lt;CertificateStatusInfo&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;CertificateStatusInfo&gt;</c> result produced by the get status async operation.</para>
    /// \endif
    /// </returns>
    public Task<CertificateStatusInfo> GetStatusAsync(
        CertificateMonitorOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(options.CertificateDirectory))
        {
            return Task.FromResult(Failure(options.CertificateDirectory, "Certificate directory is empty."));
        }

        if (!Directory.Exists(options.CertificateDirectory))
        {
            return Task.FromResult(Failure(
                options.CertificateDirectory,
                $"Certificate directory not found: {options.CertificateDirectory}"));
        }

        try
        {
            string[] files = FindCertificateFiles(options).ToArray();

            if (files.Length == 0)
            {
                return Task.FromResult(Failure(
                    options.CertificateDirectory,
                    $"No certificate files were found. Directory: {options.CertificateDirectory}"));
            }

            List<string> readErrors = [];
            List<CertificateStatusInfo> candidates = [];

            foreach (string file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                CertificateStatusInfo? status = TryReadCertificate(file, options, readErrors);
                if (status is not null)
                {
                    candidates.Add(status);
                }
            }

            if (candidates.Count == 0)
            {
                string errorText = readErrors.Count == 0
                    ? "No detailed read errors were captured."
                    : string.Join(Environment.NewLine, readErrors);

                return Task.FromResult(Failure(
                    options.CertificateDirectory,
                    $"Certificate files were found, but none could be loaded.{Environment.NewLine}{errorText}"));
            }

            CertificateStatusInfo selected = SelectBestCertificate(candidates);

            return Task.FromResult(selected);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Task.FromResult(Failure(options.CertificateDirectory, ex.Message));
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Certificate Files 항목을 찾습니다.</para>
    /// \endif
    /// \if EN
    /// <para>Finds the certificate files item.</para>
    /// \endif
    /// </summary>
    /// <param name="options">
    /// \if KO
    /// <para>동작을 구성하는 설정입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The options that configure the operation.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Find Certificate Files 작업에서 생성한 <c>IEnumerable&lt;string&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IEnumerable&lt;string&gt;</c> result produced by the find certificate files operation.</para>
    /// \endif
    /// </returns>
    private static IEnumerable<string> FindCertificateFiles(CertificateMonitorOptions options)
    {
        string[] patterns = options.CertificateFilePatterns is { Length: > 0 }
            ? options.CertificateFilePatterns
            : ["*.pem", "*.cer", "*.crt", "*.pfx", "*.p12"];

        IEnumerable<string> files = patterns
            .SelectMany(pattern =>
                Directory.EnumerateFiles(
                    options.CertificateDirectory,
                    pattern,
                    SearchOption.AllDirectories));

        return files
            .Where(IsCertificateCandidate)
            .OrderByDescending(GetCertificatePriority)
            .ThenByDescending(File.GetLastWriteTimeUtc);
    }

    /// <summary>
    /// \if KO
    /// <para>Is Certificate Candidate 조건을 확인합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Determines whether is certificate candidate.</para>
    /// \endif
    /// </summary>
    /// <param name="path">
    /// \if KO
    /// <para>path에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for path.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Is Certificate Candidate 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the is certificate candidate condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    private static bool IsCertificateCandidate(string path)
    {
        string fileName = Path.GetFileName(path).ToLowerInvariant();
        string extension = Path.GetExtension(path).ToLowerInvariant();

        if (fileName.Contains("key", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (fileName.Contains("privkey", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (fileName.Contains("chain-only", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return extension is ".pem" or ".crt" or ".cer" or ".pfx" or ".p12";
    }

    /// <summary>
    /// \if KO
    /// <para>Certificate Priority 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the certificate priority value.</para>
    /// \endif
    /// </summary>
    /// <param name="path">
    /// \if KO
    /// <para>path에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for path.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get Certificate Priority 작업에서 생성한 <c>int</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> result produced by the get certificate priority operation.</para>
    /// \endif
    /// </returns>
    private static int GetCertificatePriority(string path)
    {
        string fileName = Path.GetFileName(path).ToLowerInvariant();

        if (fileName.Contains("-crt.pem", StringComparison.OrdinalIgnoreCase))
        {
            return 700;
        }

        if (fileName.Contains("fullchain", StringComparison.OrdinalIgnoreCase))
        {
            return 600;
        }

        if (fileName.EndsWith(".crt", StringComparison.OrdinalIgnoreCase))
        {
            return 500;
        }

        if (fileName.EndsWith(".cer", StringComparison.OrdinalIgnoreCase))
        {
            return 400;
        }

        if (fileName.EndsWith(".pfx", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".p12", StringComparison.OrdinalIgnoreCase))
        {
            return 300;
        }

        if (fileName.EndsWith(".pem", StringComparison.OrdinalIgnoreCase))
        {
            return 100;
        }

        return 0;
    }

    /// <summary>
    /// \if KO
    /// <para>Read Certificate 작업을 시도하고 성공 여부를 반환합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Attempts to read certificate and returns whether the operation succeeds.</para>
    /// \endif
    /// </summary>
    /// <param name="path">
    /// \if KO
    /// <para>path에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for path.</para>
    /// \endif
    /// </param>
    /// <param name="options">
    /// \if KO
    /// <para>동작을 구성하는 설정입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The options that configure the operation.</para>
    /// \endif
    /// </param>
    /// <param name="readErrors">
    /// \if KO
    /// <para>read Errors에 사용할 <c>List&lt;string&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>List&lt;string&gt;</c> value used for read errors.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Try Read Certificate 작업에서 생성한 <c>CertificateStatusInfo?</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CertificateStatusInfo?</c> result produced by the try read certificate operation.</para>
    /// \endif
    /// </returns>
    private static CertificateStatusInfo? TryReadCertificate(
        string path,
        CertificateMonitorOptions options,
        List<string> readErrors)
    {
        try
        {
            using X509Certificate2 certificate = LoadCertificate(path, options);

            if (string.IsNullOrWhiteSpace(certificate.Subject))
            {
                readErrors.Add($"{Path.GetFileName(path)}: Certificate subject is empty.");
                return null;
            }

            DateTimeOffset notAfter = new(certificate.NotAfter);
            DateTimeOffset notBefore = new(certificate.NotBefore);
            int remainingDays = (int)Math.Floor((notAfter - DateTimeOffset.Now).TotalDays);
            CertificateHealthState state = ResolveState(remainingDays, options);

            return new CertificateStatusInfo
            {
                IsSuccess = true,
                CertificateDirectory = options.CertificateDirectory,
                CertificatePath = path,
                Subject = certificate.Subject,
                Issuer = certificate.Issuer,
                NotBefore = notBefore,
                NotAfter = notAfter,
                RemainingDays = remainingDays,
                State = state,
                Message = BuildMessage(state, remainingDays, path)
            };
        }
        catch (Exception ex)
        {
            readErrors.Add($"{Path.GetFileName(path)}: {ex.GetType().Name} - {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Certificate 데이터를 불러옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Loads certificate data.</para>
    /// \endif
    /// </summary>
    /// <param name="path">
    /// \if KO
    /// <para>path에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for path.</para>
    /// \endif
    /// </param>
    /// <param name="options">
    /// \if KO
    /// <para>동작을 구성하는 설정입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The options that configure the operation.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Load Certificate 작업에서 생성한 <c>X509Certificate2</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>X509Certificate2</c> result produced by the load certificate operation.</para>
    /// \endif
    /// </returns>
    private static X509Certificate2 LoadCertificate(
        string path,
        CertificateMonitorOptions options)
    {
        string extension = Path.GetExtension(path).ToLowerInvariant();

        if (extension is ".pfx" or ".p12")
        {
            return string.IsNullOrEmpty(options.PfxPassword)
                ? new X509Certificate2(path)
                : new X509Certificate2(path, options.PfxPassword);
        }

        if (extension == ".pem")
        {
            return LoadCertificateFromPem(path);
        }

        return new X509Certificate2(path);
    }

    /// <summary>
    /// \if KO
    /// <para>Certificate From Pem 데이터를 불러옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Loads certificate from pem data.</para>
    /// \endif
    /// </summary>
    /// <param name="path">
    /// \if KO
    /// <para>path에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for path.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Load Certificate From Pem 작업에서 생성한 <c>X509Certificate2</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>X509Certificate2</c> result produced by the load certificate from pem operation.</para>
    /// \endif
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// \if KO
    /// <para>현재 객체 상태에서 Load Certificate From Pem 작업을 수행할 수 없는 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when the load certificate from pem operation is not valid for the current object state.</para>
    /// \endif
    /// </exception>
    private static X509Certificate2 LoadCertificateFromPem(string path)
    {
        string pem = File.ReadAllText(path);

        X509Certificate2Collection collection = new();
        collection.ImportFromPem(pem);

        if (collection.Count == 0)
        {
            throw new InvalidOperationException("No certificate block was found in the PEM file.");
        }

        X509Certificate2? serverCertificate = collection
            .OfType<X509Certificate2>()
            .Where(IsServerCertificate)
            .OrderByDescending(item => item.NotAfter)
            .FirstOrDefault();

        if (serverCertificate is not null)
        {
            return new X509Certificate2(serverCertificate.RawData);
        }

        X509Certificate2 first = collection
            .OfType<X509Certificate2>()
            .OrderByDescending(item => item.NotAfter)
            .First();

        return new X509Certificate2(first.RawData);
    }

    /// <summary>
    /// \if KO
    /// <para>Is Server Certificate 조건을 확인합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Determines whether is server certificate.</para>
    /// \endif
    /// </summary>
    /// <param name="certificate">
    /// \if KO
    /// <para>certificate에 사용할 <c>X509Certificate2</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>X509Certificate2</c> value used for certificate.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Is Server Certificate 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the is server certificate condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    private static bool IsServerCertificate(X509Certificate2 certificate)
    {
        foreach (X509Extension extension in certificate.Extensions)
        {
            if (extension is X509EnhancedKeyUsageExtension eku)
            {
                foreach (Oid oid in eku.EnhancedKeyUsages)
                {
                    if (oid.Value == "1.3.6.1.5.5.7.3.1")
                    {
                        return true;
                    }
                }
            }
        }

        return certificate.Subject.Contains("CN=", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// \if KO
    /// <para>Select Best Certificate 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the select best certificate operation.</para>
    /// \endif
    /// </summary>
    /// <param name="candidates">
    /// \if KO
    /// <para>candidates에 사용할 <c>IReadOnlyCollection&lt;CertificateStatusInfo&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IReadOnlyCollection&lt;CertificateStatusInfo&gt;</c> value used for candidates.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Select Best Certificate 작업에서 생성한 <c>CertificateStatusInfo</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CertificateStatusInfo</c> result produced by the select best certificate operation.</para>
    /// \endif
    /// </returns>
    private static CertificateStatusInfo SelectBestCertificate(
        IReadOnlyCollection<CertificateStatusInfo> candidates)
    {
        DateTimeOffset now = DateTimeOffset.Now;

        CertificateStatusInfo? validCertificate = candidates
            .Where(item => item.NotAfter is not null && item.NotAfter.Value > now)
            .OrderBy(item => GetSelectedFilePriority(item.CertificatePath))
            .ThenBy(item => item.NotAfter)
            .FirstOrDefault();

        if (validCertificate is not null)
        {
            return validCertificate;
        }

        return candidates
            .OrderByDescending(item => item.NotAfter ?? DateTimeOffset.MinValue)
            .First();
    }

    /// <summary>
    /// \if KO
    /// <para>Selected File Priority 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the selected file priority value.</para>
    /// \endif
    /// </summary>
    /// <param name="path">
    /// \if KO
    /// <para>path에 사용할 <c>string?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for path.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get Selected File Priority 작업에서 생성한 <c>int</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> result produced by the get selected file priority operation.</para>
    /// \endif
    /// </returns>
    private static int GetSelectedFilePriority(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return int.MaxValue;
        }

        string fileName = Path.GetFileName(path).ToLowerInvariant();

        if (fileName.Contains("-crt.pem", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (fileName.Contains("fullchain", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (fileName.EndsWith(".crt", StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        if (fileName.EndsWith(".cer", StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        if (fileName.EndsWith(".pfx", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".p12", StringComparison.OrdinalIgnoreCase))
        {
            return 4;
        }

        return 10;
    }

    /// <summary>
    /// \if KO
    /// <para>Resolve State 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the resolve state operation.</para>
    /// \endif
    /// </summary>
    /// <param name="remainingDays">
    /// \if KO
    /// <para>remaining Days에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for remaining days.</para>
    /// \endif
    /// </param>
    /// <param name="options">
    /// \if KO
    /// <para>동작을 구성하는 설정입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The options that configure the operation.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Resolve State 작업에서 생성한 <c>CertificateHealthState</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CertificateHealthState</c> result produced by the resolve state operation.</para>
    /// \endif
    /// </returns>
    private static CertificateHealthState ResolveState(
        int remainingDays,
        CertificateMonitorOptions options)
    {
        if (remainingDays < 0)
        {
            return CertificateHealthState.Expired;
        }

        if (remainingDays < options.CriticalDays)
        {
            return CertificateHealthState.Critical;
        }

        if (remainingDays < options.WarningDays)
        {
            return CertificateHealthState.Warning;
        }

        return CertificateHealthState.Ok;
    }

    /// <summary>
    /// \if KO
    /// <para>Message 값을 구성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Builds the message value.</para>
    /// \endif
    /// </summary>
    /// <param name="state">
    /// \if KO
    /// <para>state에 사용할 <c>CertificateHealthState</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CertificateHealthState</c> value used for state.</para>
    /// \endif
    /// </param>
    /// <param name="remainingDays">
    /// \if KO
    /// <para>remaining Days에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for remaining days.</para>
    /// \endif
    /// </param>
    /// <param name="certificatePath">
    /// \if KO
    /// <para>certificate Path에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for certificate path.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Build Message 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the build message operation.</para>
    /// \endif
    /// </returns>
    private static string BuildMessage(
        CertificateHealthState state,
        int remainingDays,
        string certificatePath)
    {
        string fileName = Path.GetFileName(certificatePath);

        return state switch
        {
            CertificateHealthState.Ok =>
                $"Certificate is valid. Remaining days: {remainingDays}. File: {fileName}",

            CertificateHealthState.Warning =>
                $"Certificate renewal should be checked. Remaining days: {remainingDays}. File: {fileName}",

            CertificateHealthState.Critical =>
                $"Certificate expires soon. Remaining days: {remainingDays}. File: {fileName}",

            CertificateHealthState.Expired =>
                $"Certificate expired. Expired days: {Math.Abs(remainingDays)}. File: {fileName}",

            _ =>
                $"Certificate state is unknown. Remaining days: {remainingDays}. File: {fileName}"
        };
    }

    /// <summary>
    /// \if KO
    /// <para>Failure 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the failure operation.</para>
    /// \endif
    /// </summary>
    /// <param name="directory">
    /// \if KO
    /// <para>directory에 사용할 <c>string?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for directory.</para>
    /// \endif
    /// </param>
    /// <param name="message">
    /// \if KO
    /// <para>처리할 메시지입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The message to process.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Failure 작업에서 생성한 <c>CertificateStatusInfo</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CertificateStatusInfo</c> result produced by the failure operation.</para>
    /// \endif
    /// </returns>
    private static CertificateStatusInfo Failure(string? directory, string message)
    {
        return new CertificateStatusInfo
        {
            IsSuccess = false,
            CertificateDirectory = directory ?? string.Empty,
            State = CertificateHealthState.Error,
            Message = message
        };
    }
}
