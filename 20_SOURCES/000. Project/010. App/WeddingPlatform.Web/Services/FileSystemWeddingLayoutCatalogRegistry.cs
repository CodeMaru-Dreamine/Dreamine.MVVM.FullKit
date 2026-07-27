using System.Collections.ObjectModel;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wedding.Common;
using Wedding.Layouts.Contracts;

namespace WeddingPlatform.Services;

/// <summary>
/// Loads approved immutable declarative releases and atomically selected active
/// pointers into a last-known-good runtime snapshot.
/// </summary>
public sealed class FileSystemWeddingLayoutCatalogRegistry :
    IWeddingLayoutCatalogRegistry,
    IHostedService,
    IDisposable
{
    private const string ManifestFileName = "manifest.json";
    private const string DefinitionFileName = "layout.json";
    private const string ApprovalFileName = "approval.json";
    private const string DefinitionPolicyMigrationMarkerFileName =
        ".definition-policy-migration-v1.complete";
    private const int MaximumPublishedReleases = 500;
    private const int MaximumDefinitionPolicies = 500;
    private const int MaximumManifestBytes = 64 * 1024;
    private const int MaximumDefinitionBytes = 256 * 1024;
    private const int MaximumMetadataBytes = 64 * 1024;
    private static readonly TimeSpan WatchDebounce = TimeSpan.FromMilliseconds(600);
    private static readonly byte[] DefinitionPolicyMigrationMarkerContent =
        Encoding.UTF8.GetBytes("layout-definition-policy-migration-v1\n");

    private static readonly JsonSerializerOptions PackageJsonOptions =
        LayoutPackageJson.CreateOptions(indented: true);

    private static readonly JsonSerializerOptions WorkflowJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        WriteIndented = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) },
    };

    private static readonly IReadOnlyDictionary<WeddingLayoutReleaseId, WeddingLayoutPublishedPackage>
        EmptyPackages =
            new ReadOnlyDictionary<WeddingLayoutReleaseId, WeddingLayoutPublishedPackage>(
                new Dictionary<WeddingLayoutReleaseId, WeddingLayoutPublishedPackage>());
    private static readonly IReadOnlyDictionary<string, LayoutDefinitionPolicy>
        EmptyPolicies =
            new ReadOnlyDictionary<string, LayoutDefinitionPolicy>(
                new Dictionary<string, LayoutDefinitionPolicy>(
                    StringComparer.OrdinalIgnoreCase));

    private readonly ILogger<FileSystemWeddingLayoutCatalogRegistry> _logger;
    private readonly string _layoutPackagesRoot;
    private readonly string _releasesRoot;
    private readonly string _activeRoot;
    private readonly string _policiesRoot;
    private readonly SemaphoreSlim _reloadGate = new(1, 1);
    private readonly object _watcherGate = new();
    private readonly CancellationTokenSource _lifetimeCancellation = new();

    private RuntimeState _state = new(
        WeddingLayoutCatalog.Instance,
        EmptyPackages,
        EmptyPolicies,
        EmptyFingerprint);
    private FileSystemWatcher? _watcher;
    private CancellationTokenSource? _debounceCancellation;
    private bool _started;
    private bool _disposed;

    public FileSystemWeddingLayoutCatalogRegistry(
        WeddingOptions options,
        ILogger<FileSystemWeddingLayoutCatalogRegistry> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var publicWeddingRoot = Path.GetFullPath(options.ResolvedDataPath);
        var appDataRoot = Directory.GetParent(
                Path.TrimEndingDirectorySeparator(publicWeddingRoot))
            ?.FullName
            ?? throw new InvalidOperationException(
                $"Cannot resolve the App_Data parent of '{publicWeddingRoot}'.");

        _layoutPackagesRoot = Path.GetFullPath(
            Path.Combine(appDataRoot, "LayoutPackages"));
        _releasesRoot = Path.Combine(_layoutPackagesRoot, "Releases");
        _activeRoot = Path.Combine(_layoutPackagesRoot, "Active");
        _policiesRoot = Path.Combine(_layoutPackagesRoot, "Policies");

        if (IsSameOrChildPath(_layoutPackagesRoot, publicWeddingRoot))
        {
            throw new InvalidOperationException(
                "Layout packages must not be stored below the publicly mapped Wedding data directory.");
        }
    }

    public WeddingLayoutCatalog Current => Volatile.Read(ref _state).Catalog;

    public IReadOnlyDictionary<WeddingLayoutReleaseId, WeddingLayoutPublishedPackage>
        PublishedPackages => Volatile.Read(ref _state).Packages;

    public IReadOnlyDictionary<string, LayoutDefinitionPolicy> DefinitionPolicies =>
        Volatile.Read(ref _state).Policies;

    public string LayoutPackagesRoot => _layoutPackagesRoot;

    public string ReleasesRoot => _releasesRoot;

    public event EventHandler<WeddingLayoutCatalogChangedEventArgs>? Changed;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        EnsureStorageRoots();

        lock (_watcherGate)
        {
            if (!_started)
            {
                _started = true;
                RestartWatcherNoLock();
            }
        }

        var result = await ReloadAsync(cancellationToken).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            _logger.LogWarning(
                "Initial runtime layout catalog load failed; built-in/last-known-good snapshot remains active: {Error}",
                result.Error);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        lock (_watcherGate)
        {
            _started = false;
            _debounceCancellation?.Cancel();
            _debounceCancellation?.Dispose();
            _debounceCancellation = null;
            DisposeWatcherNoLock();
        }

        return Task.CompletedTask;
    }

    public async Task<WeddingLayoutReloadResult> ReloadAsync(
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await _reloadGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            RuntimeState candidate;
            try
            {
                candidate = await BuildCandidateAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                var retained = Volatile.Read(ref _state);
                _logger.LogWarning(
                    ex,
                    "Runtime layout catalog reload was rejected. Retaining last-known-good snapshot with {Count} uploaded releases.",
                    retained.Packages.Count);
                return new WeddingLayoutReloadResult(
                    false,
                    false,
                    retained.Packages.Count,
                    ex.Message);
            }

            var previous = Volatile.Read(ref _state);
            if (string.Equals(
                    previous.Fingerprint,
                    candidate.Fingerprint,
                    StringComparison.Ordinal))
            {
                return new WeddingLayoutReloadResult(
                    true,
                    false,
                    previous.Packages.Count,
                    null);
            }

            Interlocked.Exchange(ref _state, candidate);
            RaiseChanged(previous.Catalog, candidate.Catalog);
            _logger.LogInformation(
                "Runtime layout catalog snapshot replaced without restart: {DescriptorCount} layouts, {UploadedReleaseCount} uploaded releases.",
                candidate.Catalog.Descriptors.Count,
                candidate.Packages.Count);
            return new WeddingLayoutReloadResult(
                true,
                true,
                candidate.Packages.Count,
                null);
        }
        finally
        {
            _reloadGate.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _lifetimeCancellation.Cancel();
        lock (_watcherGate)
        {
            _started = false;
            _debounceCancellation?.Cancel();
            _debounceCancellation?.Dispose();
            _debounceCancellation = null;
            DisposeWatcherNoLock();
        }

        _reloadGate.Dispose();
        _lifetimeCancellation.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task<RuntimeState> BuildCandidateAsync(
        CancellationToken cancellationToken)
    {
        EnsureStorageRoots();
        using var fingerprint = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var packages = await LoadPublishedPackagesAsync(fingerprint, cancellationToken)
            .ConfigureAwait(false);
        var policies = await LoadDefinitionPoliciesAsync(fingerprint, cancellationToken)
            .ConfigureAwait(false);
        var activePointers = await LoadActivePointersAsync(fingerprint, cancellationToken)
            .ConfigureAwait(false);

        foreach (var pointer in activePointers.Values)
        {
            if (!packages.ContainsKey(new WeddingLayoutReleaseId(
                    pointer.Key,
                    pointer.Version)))
            {
                throw new InvalidDataException(
                    $"Active pointer '{pointer.Key}@{pointer.Version}' references a missing release.");
            }
        }

        var descriptors = WeddingLayoutCatalog.Instance.Descriptors.ToList();
        var releases = WeddingLayoutCatalog.Instance.Releases.ToList();
        foreach (var group in packages.Values
                     .GroupBy(x => x.Manifest.Key, StringComparer.OrdinalIgnoreCase)
                     .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (!policies.TryGetValue(group.Key, out var policy))
            {
                throw new InvalidDataException(
                    $"Published layout '{group.Key}' has no server-owned definition policy.");
            }

            if (!activePointers.TryGetValue(group.Key, out var active))
            {
                // An approved but not-yet-activated release remains available to
                // the review/rollback workflow but is intentionally absent from
                // the user-facing catalog.
                continue;
            }

            var currentPackage = group.Single(x =>
                string.Equals(
                    x.Manifest.Version,
                    active.Version,
                    StringComparison.Ordinal));
            descriptors.Add(new WeddingLayoutDescriptor(
                currentPackage.Manifest.Key,
                WeddingLayoutMode.Unknown,
                currentPackage.Manifest.Label,
                currentPackage.Manifest.Description,
                ToWeddingTier(policy.Tier),
                currentPackage.Manifest.Version,
                false));

            foreach (var package in group.OrderBy(
                         x => x.Manifest.Version,
                         StringComparer.Ordinal))
            {
                releases.Add(new WeddingLayoutRelease(
                    package.Manifest.Key,
                    package.Manifest.Version,
                    WeddingLayoutMode.Unknown,
                    true,
                    $"w-layout-package w-layout-package-{package.Manifest.Key}",
                    ContainsBlock(package.Definition.Root, LayoutBlockKind.Navigation),
                    Array.AsReadOnly(
                        package.Definition.SectionOrder
                            .Select(ToSectionStorageKey)
                            .ToArray())));
            }
        }

        return new RuntimeState(
            new WeddingLayoutCatalog(descriptors, releases),
            new ReadOnlyDictionary<WeddingLayoutReleaseId, WeddingLayoutPublishedPackage>(
                packages),
            new ReadOnlyDictionary<string, LayoutDefinitionPolicy>(policies),
            Convert.ToHexString(fingerprint.GetHashAndReset()));
    }

    private async Task<Dictionary<WeddingLayoutReleaseId, WeddingLayoutPublishedPackage>>
        LoadPublishedPackagesAsync(
            IncrementalHash fingerprint,
            CancellationToken cancellationToken)
    {
        var allFiles = Directory
            .EnumerateFiles(_releasesRoot, "*", SearchOption.AllDirectories)
            .OrderBy(
                path => Path.GetRelativePath(_layoutPackagesRoot, path),
                StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (allFiles.Length > MaximumPublishedReleases * 3)
        {
            throw new InvalidDataException(
                $"Published layout storage contains too many files ({allFiles.Length}).");
        }

        var manifestPaths = allFiles
            .Where(path => string.Equals(
                Path.GetFileName(path),
                ManifestFileName,
                StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (manifestPaths.Length > MaximumPublishedReleases)
        {
            throw new InvalidDataException(
                $"Published layout storage exceeds the {MaximumPublishedReleases} release limit.");
        }

        var expectedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var packages = new Dictionary<WeddingLayoutReleaseId, WeddingLayoutPublishedPackage>();
        foreach (var manifestPath in manifestPaths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var packageDirectory = ValidatePackageDirectory(manifestPath);
            var definitionPath = Path.Combine(packageDirectory, DefinitionFileName);
            var approvalPath = Path.Combine(packageDirectory, ApprovalFileName);
            foreach (var path in new[] { manifestPath, definitionPath, approvalPath })
            {
                expectedFiles.Add(Path.GetFullPath(path));
                if (File.Exists(path))
                {
                    RejectReparsePoint(path);
                }
            }

            var manifestBytes = await ReadBoundedFileAsync(
                    manifestPath,
                    MaximumManifestBytes,
                    cancellationToken)
                .ConfigureAwait(false);
            var definitionBytes = await ReadBoundedFileAsync(
                    definitionPath,
                    MaximumDefinitionBytes,
                    cancellationToken)
                .ConfigureAwait(false);
            var approvalBytes = await ReadBoundedFileAsync(
                    approvalPath,
                    MaximumMetadataBytes,
                    cancellationToken)
                .ConfigureAwait(false);
            AppendFingerprint(fingerprint, manifestPath, manifestBytes);
            AppendFingerprint(fingerprint, definitionPath, definitionBytes);
            AppendFingerprint(fingerprint, approvalPath, approvalBytes);

            var manifest = Deserialize<LayoutManifest>(
                manifestBytes,
                PackageJsonOptions,
                manifestPath);
            var definition = Deserialize<LayoutDefinition>(
                definitionBytes,
                PackageJsonOptions,
                definitionPath);
            var approval = Deserialize<WeddingLayoutReleaseApproval>(
                approvalBytes,
                WorkflowJsonOptions,
                approvalPath);
            var published = ValidateAndFreezePackage(
                new LayoutPackage
                {
                    Manifest = manifest,
                    Definition = definition,
                },
                approval,
                packageDirectory);
            if (!packages.TryAdd(published.ReleaseId, published))
            {
                throw PackageError(
                    packageDirectory,
                    $"Duplicate release identity '{published.ReleaseId}'.");
            }
        }

        foreach (var file in allFiles)
        {
            if (!expectedFiles.Contains(Path.GetFullPath(file)))
            {
                throw new InvalidDataException(
                    $"Unexpected file '{RelativePath(file)}'. Published releases may contain only manifest.json, layout.json and approval.json.");
            }
        }

        return packages;
    }

    private async Task<Dictionary<string, LayoutDefinitionPolicy>>
        LoadDefinitionPoliciesAsync(
            IncrementalHash fingerprint,
            CancellationToken cancellationToken)
    {
        if (Directory.EnumerateDirectories(_policiesRoot).Any())
        {
            throw new InvalidDataException(
                "Layout definition policy storage may not contain subdirectories.");
        }

        var files = Directory
            .EnumerateFiles(_policiesRoot, "*", SearchOption.TopDirectoryOnly)
            .Where(path =>
                !WeddingLayoutDefinitionPolicyFileNames.IsOwnedTemporaryFileName(path))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Take(MaximumDefinitionPolicies + 2)
            .ToArray();
        var policyFiles = files
            .Where(path => !string.Equals(
                Path.GetFileName(path),
                DefinitionPolicyMigrationMarkerFileName,
                StringComparison.Ordinal))
            .ToArray();
        if (policyFiles.Length > MaximumDefinitionPolicies)
        {
            throw new InvalidDataException(
                $"Layout definition policy storage exceeds the {MaximumDefinitionPolicies}-item limit.");
        }

        var markerPath = Path.Combine(
            _policiesRoot,
            DefinitionPolicyMigrationMarkerFileName);
        if (File.Exists(markerPath))
        {
            var markerBytes = await ReadBoundedFileAsync(
                    markerPath,
                    DefinitionPolicyMigrationMarkerContent.Length,
                    cancellationToken)
                .ConfigureAwait(false);
            if (!markerBytes.AsSpan().SequenceEqual(
                    DefinitionPolicyMigrationMarkerContent))
            {
                throw new InvalidDataException(
                    "The legacy layout definition policy migration marker is invalid.");
            }

            // Older builds created this marker after copying author-controlled
            // Manifest.Tier values. It is accepted for storage compatibility
            // only; a missing marker never authorizes automatic classification.
            AppendFingerprint(fingerprint, markerPath, markerBytes);
        }

        var policies = new Dictionary<string, LayoutDefinitionPolicy>(
            StringComparer.OrdinalIgnoreCase);
        foreach (var path in policyFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RejectReparsePoint(path);
            if (!string.Equals(
                    Path.GetExtension(path),
                    ".json",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    $"Unexpected definition policy file '{RelativePath(path)}'.");
            }

            var bytes = await ReadBoundedFileAsync(
                    path,
                    MaximumMetadataBytes,
                    cancellationToken)
                .ConfigureAwait(false);
            AppendFingerprint(fingerprint, path, bytes);
            var policy = Deserialize<LayoutDefinitionPolicy>(
                bytes,
                WorkflowJsonOptions,
                path);
            ValidateDefinitionPolicy(policy, path);
            if (string.Equals(
                    policy.ClassifiedBy,
                    "legacy-migration",
                    StringComparison.Ordinal))
            {
                _logger.LogWarning(
                    "Ignoring unconfirmed legacy tier seed for layout {LayoutKey}. A super-administrator must classify this LayoutKey explicitly.",
                    policy.LayoutKey);
                continue;
            }

            if (WeddingLayoutCatalog.Instance.FindDescriptor(policy.LayoutKey) is not null)
            {
                throw new InvalidDataException(
                    $"Built-in layout '{policy.LayoutKey}' cannot have a persisted definition policy.");
            }

            if (!policies.TryAdd(policy.LayoutKey, policy))
            {
                throw new InvalidDataException(
                    $"Layout '{policy.LayoutKey}' has more than one definition policy.");
            }
        }

        return policies;
    }

    private static void ValidateDefinitionPolicy(
        LayoutDefinitionPolicy policy,
        string path)
    {
        if (policy.SchemaVersion != LayoutDefinitionPolicy.SupportedSchemaVersion
            || !WeddingLayoutKeys.IsValid(policy.LayoutKey)
            || !string.Equals(
                Path.GetFileNameWithoutExtension(path),
                policy.LayoutKey,
                StringComparison.Ordinal)
            || !Enum.IsDefined(policy.Tier)
            || string.IsNullOrWhiteSpace(policy.ClassifiedBy)
            || policy.ClassifiedBy.Length > 120
            || policy.ClassifiedBy.Any(char.IsControl)
            || policy.ClassifiedBy.Contains('<')
            || policy.ClassifiedBy.Contains('>')
            || policy.ClassifiedAtUtc == default
            || policy.ClassifiedAtUtc > DateTimeOffset.UtcNow.AddMinutes(5)
            || string.IsNullOrWhiteSpace(policy.Reason)
            || policy.Reason.Length > 300
            || policy.Reason.Any(char.IsControl)
            || policy.Reason.Contains('<')
            || policy.Reason.Contains('>')
            || policy.Revision < 1)
        {
            throw new InvalidDataException(
                $"Layout definition policy '{Path.GetFileName(path)}' is invalid.");
        }
    }

    private async Task<Dictionary<string, WeddingLayoutActiveReleasePointer>>
        LoadActivePointersAsync(
            IncrementalHash fingerprint,
            CancellationToken cancellationToken)
    {
        if (Directory.EnumerateDirectories(_activeRoot).Any())
        {
            throw new InvalidDataException(
                "Active layout pointer storage may not contain subdirectories.");
        }

        var files = Directory
            .EnumerateFiles(_activeRoot, "*", SearchOption.TopDirectoryOnly)
            .Where(path =>
                !WeddingLayoutActivePointerFileNames.IsOwnedTemporaryFileName(path))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Take(MaximumPublishedReleases + 1)
            .ToArray();
        if (files.Length > MaximumPublishedReleases)
        {
            throw new InvalidDataException(
                $"Active pointer storage exceeds the {MaximumPublishedReleases}-item limit.");
        }

        var pointers = new Dictionary<string, WeddingLayoutActiveReleasePointer>(
            StringComparer.OrdinalIgnoreCase);
        foreach (var path in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RejectReparsePoint(path);
            if (!string.Equals(
                    Path.GetExtension(path),
                    ".json",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    $"Unexpected active pointer file '{RelativePath(path)}'.");
            }

            var bytes = await ReadBoundedFileAsync(
                    path,
                    MaximumMetadataBytes,
                    cancellationToken)
                .ConfigureAwait(false);
            AppendFingerprint(fingerprint, path, bytes);
            var pointer = Deserialize<WeddingLayoutActiveReleasePointer>(
                bytes,
                WorkflowJsonOptions,
                path);
            ValidateActivePointer(pointer, path);
            if (!pointers.TryAdd(pointer.Key, pointer))
            {
                throw new InvalidDataException(
                    $"Layout '{pointer.Key}' has more than one active pointer.");
            }
        }

        return pointers;
    }

    private WeddingLayoutPublishedPackage ValidateAndFreezePackage(
        LayoutPackage package,
        WeddingLayoutReleaseApproval approval,
        string packageDirectory)
    {
        var validation = LayoutPackageValidator.Validate(package);
        if (!validation.IsValid)
        {
            throw PackageError(
                packageDirectory,
                string.Join(
                    "; ",
                    validation.Errors.Take(10).Select(error =>
                        $"{error.Path} [{error.Code}] {error.Message}")));
        }

        var canonical = LayoutPackageCanonicalizer.Canonicalize(package);
        if (WeddingLayoutCatalog.Instance.FindDescriptor(canonical.Manifest.Key) is not null)
        {
            throw PackageError(
                packageDirectory,
                $"Uploaded key '{canonical.Manifest.Key}' collides with a built-in layout.");
        }

        ValidateDirectoryIdentity(
            packageDirectory,
            canonical.Manifest.Key,
            canonical.Manifest.Version);
        ValidateApproval(approval, canonical, packageDirectory);
        return new WeddingLayoutPublishedPackage(canonical, approval);
    }

    private static void ValidateApproval(
        WeddingLayoutReleaseApproval approval,
        LayoutPackage package,
        string packageDirectory)
    {
        if (approval.SchemaVersion != WeddingLayoutReleaseApproval.SupportedSchemaVersion
            || !IsSubmissionId(approval.SubmissionId)
            || string.IsNullOrWhiteSpace(approval.ApprovedBy)
            || approval.ApprovedBy.Length > 120
            || approval.ApprovedBy.Any(char.IsControl)
            || approval.ApprovedBy.Contains('<')
            || approval.ApprovedBy.Contains('>')
            || approval.ApprovedAtUtc == default
            || approval.ApprovedAtUtc > DateTimeOffset.UtcNow.AddMinutes(5)
            || !WeddingLayoutKeys.IsValid(approval.OwnerTenantSlug)
            || approval.PackageSha256.Length != 64
            || approval.PackageSha256.Any(character => !Uri.IsHexDigit(character)))
        {
            throw PackageError(packageDirectory, "Approval metadata is invalid.");
        }

        var canonicalBytes = JsonSerializer.SerializeToUtf8Bytes(
            package,
            PackageJsonOptions);
        var canonicalHash = Convert.ToHexString(SHA256.HashData(canonicalBytes));
        if (!string.Equals(
                canonicalHash,
                approval.PackageSha256,
                StringComparison.Ordinal))
        {
            throw PackageError(
                packageDirectory,
                "The immutable package content does not match its approval hash.");
        }
    }

    private static void ValidateActivePointer(
        WeddingLayoutActiveReleasePointer pointer,
        string path)
    {
        if (pointer.SchemaVersion != WeddingLayoutActiveReleasePointer.SupportedSchemaVersion
            || !WeddingLayoutKeys.IsValid(pointer.Key)
            || !WeddingLayoutVersion.IsValid(pointer.Version)
            || !string.Equals(
                Path.GetFileNameWithoutExtension(path),
                pointer.Key,
                StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(pointer.ActivatedBy)
            || pointer.ActivatedBy.Length > 120
            || pointer.ActivatedBy.Any(char.IsControl)
            || pointer.ActivatedAtUtc == default
            || pointer.ActivatedAtUtc > DateTimeOffset.UtcNow.AddMinutes(5)
            || string.IsNullOrWhiteSpace(pointer.Reason)
            || pointer.Reason.Length > 300
            || pointer.Reason.Any(char.IsControl))
        {
            throw new InvalidDataException(
                $"Active layout pointer '{Path.GetFileName(path)}' is invalid.");
        }
    }

    private string ValidatePackageDirectory(string manifestPath)
    {
        var fullManifestPath = Path.GetFullPath(manifestPath);
        if (!IsSameOrChildPath(fullManifestPath, _releasesRoot))
        {
            throw new InvalidDataException(
                $"Manifest '{fullManifestPath}' is outside the release root.");
        }

        var packageDirectory = Path.GetDirectoryName(fullManifestPath)
            ?? throw new InvalidDataException(
                $"Manifest '{fullManifestPath}' has no directory.");
        var keyDirectory = Directory.GetParent(packageDirectory)
            ?? throw new InvalidDataException(
                $"Manifest '{fullManifestPath}' has no key directory.");
        if (!string.Equals(
                Directory.GetParent(keyDirectory.FullName)?.FullName,
                Path.TrimEndingDirectorySeparator(_releasesRoot),
                PathComparison))
        {
            throw new InvalidDataException(
                $"Manifest '{RelativePath(fullManifestPath)}' must be stored as <key>/<version>/manifest.json.");
        }

        RejectReparsePoint(keyDirectory.FullName);
        RejectReparsePoint(packageDirectory);
        return packageDirectory;
    }

    private static void ValidateDirectoryIdentity(
        string packageDirectory,
        string key,
        string version)
    {
        if (!string.Equals(
                Path.GetFileName(Directory.GetParent(packageDirectory)?.FullName),
                key,
                StringComparison.Ordinal)
            || !string.Equals(
                Path.GetFileName(packageDirectory),
                version,
                StringComparison.Ordinal))
        {
            throw PackageError(
                packageDirectory,
                $"Directory identity must be '{key}/{version}'.");
        }
    }

    private void EnsureStorageRoots()
    {
        EnsureDirectory(_layoutPackagesRoot);
        EnsureDirectory(_releasesRoot);
        EnsureDirectory(_activeRoot);
        EnsureDirectory(_policiesRoot);
    }

    private static void EnsureDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        RejectReparsePoint(path);
    }

    private static T Deserialize<T>(
        byte[] bytes,
        JsonSerializerOptions options,
        string path)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(bytes, options)
                ?? throw new InvalidDataException(
                    $"Layout file '{Path.GetFileName(path)}' is empty.");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException(
                $"Layout file '{Path.GetFileName(path)}' is invalid: {ex.Message}",
                ex);
        }
    }

    private static async Task<byte[]> ReadBoundedFileAsync(
        string path,
        int maximumBytes,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"Required layout release file '{Path.GetFileName(path)}' was not found.",
                path);
        }

        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read | FileShare.Delete,
            16 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        if (stream.Length > maximumBytes)
        {
            throw new InvalidDataException(
                $"File '{Path.GetFileName(path)}' exceeds the {maximumBytes}-byte limit.");
        }

        using var destination = new MemoryStream((int)Math.Min(stream.Length, maximumBytes));
        var buffer = new byte[16 * 1024];
        while (true)
        {
            var read = await stream.ReadAsync(buffer, cancellationToken)
                .ConfigureAwait(false);
            if (read == 0)
            {
                return destination.ToArray();
            }

            if (destination.Length + read > maximumBytes)
            {
                throw new InvalidDataException(
                    $"File '{Path.GetFileName(path)}' exceeds the {maximumBytes}-byte limit.");
            }

            await destination.WriteAsync(
                    buffer.AsMemory(0, read),
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private void AppendFingerprint(
        IncrementalHash fingerprint,
        string path,
        byte[] content)
    {
        fingerprint.AppendData(Encoding.UTF8.GetBytes(
            RelativePath(path).Replace('\\', '/')));
        fingerprint.AppendData([0]);
        fingerprint.AppendData(content);
        fingerprint.AppendData([0]);
    }

    private void RestartWatcherNoLock()
    {
        DisposeWatcherNoLock();
        EnsureStorageRoots();
        _watcher = new FileSystemWatcher(_layoutPackagesRoot, "*")
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName
                | NotifyFilters.DirectoryName
                | NotifyFilters.LastWrite
                | NotifyFilters.Size
                | NotifyFilters.CreationTime,
            EnableRaisingEvents = false,
        };
        _watcher.Changed += OnPackageFileChanged;
        _watcher.Created += OnPackageFileChanged;
        _watcher.Deleted += OnPackageFileChanged;
        _watcher.Renamed += OnPackageFileRenamed;
        _watcher.Error += OnWatcherError;
        _watcher.EnableRaisingEvents = true;
    }

    private void DisposeWatcherNoLock()
    {
        if (_watcher is null)
        {
            return;
        }

        _watcher.EnableRaisingEvents = false;
        _watcher.Changed -= OnPackageFileChanged;
        _watcher.Created -= OnPackageFileChanged;
        _watcher.Deleted -= OnPackageFileChanged;
        _watcher.Renamed -= OnPackageFileRenamed;
        _watcher.Error -= OnWatcherError;
        _watcher.Dispose();
        _watcher = null;
    }

    private void OnPackageFileChanged(object sender, FileSystemEventArgs args)
    {
        if (ShouldReloadForPath(args.FullPath))
        {
            ScheduleWatcherReload();
        }
    }

    private void OnPackageFileRenamed(object sender, RenamedEventArgs args)
    {
        if (ShouldReloadForPath(args.FullPath) || ShouldReloadForPath(args.OldFullPath))
        {
            ScheduleWatcherReload();
        }
    }

    private bool ShouldReloadForPath(string path) =>
        IsSameOrChildPath(path, _releasesRoot)
        || IsSameOrChildPath(path, _activeRoot)
        || IsSameOrChildPath(path, _policiesRoot);

    private void OnWatcherError(object sender, ErrorEventArgs args)
    {
        _logger.LogWarning(
            args.GetException(),
            "Runtime layout package watcher failed and will be recreated.");
        lock (_watcherGate)
        {
            if (_started && !_disposed)
            {
                RestartWatcherNoLock();
            }
        }

        ScheduleWatcherReload();
    }

    private void ScheduleWatcherReload()
    {
        CancellationTokenSource debounce;
        lock (_watcherGate)
        {
            if (!_started || _disposed)
            {
                return;
            }

            _debounceCancellation?.Cancel();
            _debounceCancellation?.Dispose();
            debounce = CancellationTokenSource.CreateLinkedTokenSource(
                _lifetimeCancellation.Token);
            _debounceCancellation = debounce;
        }

        _ = DebouncedReloadAsync(debounce);
    }

    private async Task DebouncedReloadAsync(CancellationTokenSource debounce)
    {
        try
        {
            await Task.Delay(WatchDebounce, debounce.Token).ConfigureAwait(false);
            var result = await ReloadAsync(debounce.Token).ConfigureAwait(false);
            if (!result.Succeeded)
            {
                _logger.LogWarning(
                    "Watched layout package change was rejected; last-known-good snapshot remains active: {Error}",
                    result.Error);
            }
        }
        catch (OperationCanceledException) when (debounce.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error while reloading watched layout packages.");
        }
        finally
        {
            lock (_watcherGate)
            {
                if (ReferenceEquals(_debounceCancellation, debounce))
                {
                    _debounceCancellation = null;
                }
            }

            debounce.Dispose();
        }
    }

    private void RaiseChanged(
        WeddingLayoutCatalog previous,
        WeddingLayoutCatalog current)
    {
        var handlers = Changed;
        if (handlers is null)
        {
            return;
        }

        var args = new WeddingLayoutCatalogChangedEventArgs(
            previous,
            current,
            DateTimeOffset.UtcNow);
        foreach (EventHandler<WeddingLayoutCatalogChangedEventArgs> handler
                 in handlers.GetInvocationList())
        {
            try
            {
                handler(this, args);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "A runtime layout catalog Changed subscriber failed.");
            }
        }
    }

    private string RelativePath(string path) =>
        Path.GetRelativePath(_layoutPackagesRoot, path);

    private static bool ContainsBlock(LayoutBlock block, LayoutBlockKind kind) =>
        block.Kind == kind || block.Children.Any(child => ContainsBlock(child, kind));

    private static WeddingLayoutTier ToWeddingTier(LayoutTier tier) =>
        tier == LayoutTier.Premium
            ? WeddingLayoutTier.Premium
            : WeddingLayoutTier.Free;

    private static string ToSectionStorageKey(LayoutSectionKey section) =>
        section switch
        {
            LayoutSectionKey.Hero => "hero",
            LayoutSectionKey.Invitation => "info",
            LayoutSectionKey.Calendar => "calendar",
            LayoutSectionKey.Gallery => "gallery",
            LayoutSectionKey.Story => "story",
            LayoutSectionKey.Video => "video",
            LayoutSectionKey.Location => "details",
            LayoutSectionKey.Accounts => "gift",
            LayoutSectionKey.Guestbook => "guestbook",
            LayoutSectionKey.Contact => "contact",
            _ => throw new InvalidDataException($"Unsupported section '{section}'."),
        };

    private static bool IsSubmissionId(string? value) =>
        value is { Length: 32 }
        && value.All(character =>
            character is >= '0' and <= '9'
            or >= 'a' and <= 'f');

    private static void RejectReparsePoint(string path)
    {
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidDataException(
                $"Reparse points are not allowed in published layout storage: '{path}'.");
        }
    }

    private static InvalidDataException PackageError(
        string packageDirectory,
        string message) =>
        new($"Invalid layout package '{Path.GetFileName(Directory.GetParent(packageDirectory)?.FullName)}/{Path.GetFileName(packageDirectory)}': {message}");

    private static bool IsSameOrChildPath(string candidate, string root)
    {
        var relative = Path.GetRelativePath(
            Path.GetFullPath(root),
            Path.GetFullPath(candidate));
        return relative == "."
            || (!relative.Equals("..", StringComparison.Ordinal)
                && !relative.StartsWith(
                    $"..{Path.DirectorySeparatorChar}",
                    StringComparison.Ordinal)
                && !Path.IsPathRooted(relative));
    }

    private void ThrowIfDisposed() =>
        ObjectDisposedException.ThrowIf(_disposed, this);

    private static StringComparison PathComparison =>
        OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

    private static string EmptyFingerprint =>
        Convert.ToHexString(SHA256.HashData(Array.Empty<byte>()));

    private sealed record RuntimeState(
        WeddingLayoutCatalog Catalog,
        IReadOnlyDictionary<WeddingLayoutReleaseId, WeddingLayoutPublishedPackage> Packages,
        IReadOnlyDictionary<string, LayoutDefinitionPolicy> Policies,
        string Fingerprint);
}
