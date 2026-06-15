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
/// \brief 파일 시스템의 X509 인증서를 읽어 만료 상태를 계산합니다.
/// </summary>
public sealed class X509CertificateMonitorService : ICertificateMonitorService
{
    /// <inheritdoc />
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
