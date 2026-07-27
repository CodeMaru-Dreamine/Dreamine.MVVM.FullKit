using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Wedding.Common;
using Wedding.Layouts.Contracts;
using WeddingPlatform.Models;
using WeddingPlatform.Services;

namespace Wedding.Layouts.PipelineSmoke;

internal static class Program
{
    private const string TemporaryDirectoryPrefix = "Wedding.Layouts.PipelineSmoke-";
    private const string TenantSlug = "smoke-tenant";
    private const string LayoutKey = "smoke-dynamic-layout";

    public static async Task<int> Main()
    {
        var tempRoot = Path.Combine(
            Path.GetTempPath(),
            TemporaryDirectoryPrefix + Guid.NewGuid().ToString("N"));

        try
        {
            await RunAsync(tempRoot);
            Console.WriteLine();
            Console.WriteLine("PIPELINE SMOKE PASSED");
            return 0;
        }
        catch (Exception exception)
        {
            await Console.Error.WriteLineAsync();
            await Console.Error.WriteLineAsync("PIPELINE SMOKE FAILED");
            Console.Error.WriteLine(exception);
            return 1;
        }
        finally
        {
            SafeDeleteTemporaryDirectory(tempRoot);
        }
    }

    private static async Task RunAsync(string tempRoot)
    {
        var publicWeddingRoot = Path.Combine(tempRoot, "App_Data", "Wedding");
        Directory.CreateDirectory(publicWeddingRoot);
        var testSigningMaterial = Convert.ToHexString(Guid.NewGuid().ToByteArray());

        var options = new WeddingOptions
        {
            DataPath = publicWeddingRoot,
            SuperAdminPassword = testSigningMaterial,
        };
        var actor = new WeddingCurrentUser(
            "oauth-pipeline-owner",
            "Smoke",
            "owner@example.invalid",
            "Pipeline Owner");
        var tenant = new TenantConfig
        {
            Slug = TenantSlug,
            OwnerUserId = actor.Id,
            OwnerEmail = actor.Email,
            OwnerDisplayName = actor.DisplayName,
            HasPremiumPlan = true,
        };
        var tenants = new InMemoryTenantStore(tempRoot, tenant);
        var audit = new InMemorySuperAdminAuditLog();
        var tokenService = new SuperAdminSessionTokenService(options);
        var superAdminToken = tokenService.CreateToken();

        Assert(
            tokenService.ValidateToken(superAdminToken),
            "real signed super-admin token validates");

        using var registry = new FileSystemWeddingLayoutCatalogRegistry(
            options,
            NullLogger<FileSystemWeddingLayoutCatalogRegistry>.Instance);
        await registry.StartAsync(CancellationToken.None);
        try
        {
            using var submissions = new FileSystemWeddingLayoutSubmissionService(
                options,
                tenants,
                tokenService,
                audit,
                registry);

            var changedEvents = 0;
            registry.Changed += (_, _) => Interlocked.Increment(ref changedEvents);

            Assert(
                registry.PublishedPackages.Count == 0,
                "registry starts with built-in layouts and no uploaded releases");

            var classification = await submissions.SetDefinitionTierAsync(
                LayoutKey,
                LayoutTier.Premium,
                superAdminToken,
                "Pipeline SuperAdmin",
                "Classify the pipeline smoke layout before release approval");
            Assert(
                classification.Policy.LayoutKey == LayoutKey
                && classification.Policy.Tier == LayoutTier.Premium
                && classification.Policy.Revision == 1
                && !classification.Reclassified
                && classification.Reload.Succeeded,
                "super-admin explicitly classifies the LayoutKey before approval");

            var package100 = CreatePackage(
                "1.0.0",
                "#B8875B",
                presentation: LayoutPresentationMode.FlipCard);
            AssertWpfCompatibleRoundTrip(package100);
            var submission100 = await SubmitAsync(submissions, actor, package100);
            Assert(
                submission100.Status == WeddingLayoutSubmissionStatus.Pending,
                "tenant upload stores v1.0.0 as pending");

            var approval100 = await submissions.ApproveAsync(
                submission100.SubmissionId,
                superAdminToken,
                "Pipeline SuperAdmin",
                activate: true);
            Assert(
                approval100.Submission.Status == WeddingLayoutSubmissionStatus.Approved
                && approval100.Activated
                && approval100.Reload.Succeeded,
                "approval publishes and activates v1.0.0");
            AssertCurrent(registry, "1.0.0");
            Assert(
                registry.PublishedPackages.ContainsKey(
                    new WeddingLayoutReleaseId(LayoutKey, "1.0.0")),
                "same registry instance exposes published v1.0.0 immediately");

            var package110 = CreatePackage(
                "1.1.0",
                "#7A5CB8",
                presentation: LayoutPresentationMode.PagedBook);
            AssertWpfCompatibleRoundTrip(package110);
            var submission110 = await SubmitAsync(submissions, actor, package110);
            var approval110 = await submissions.ApproveAsync(
                submission110.SubmissionId,
                superAdminToken,
                "Pipeline SuperAdmin",
                activate: false);
            Assert(
                approval110.Submission.Status == WeddingLayoutSubmissionStatus.Approved
                && !approval110.Activated
                && approval110.Reload.Succeeded,
                "approval publishes v1.1.0 without moving the active pointer");
            AssertCurrent(registry, "1.0.0");
            Assert(
                registry.PublishedPackages.ContainsKey(
                    new WeddingLayoutReleaseId(LayoutKey, "1.1.0")),
                "inactive v1.1.0 is retained in the immutable release registry");

            var release110DefinitionPath = Path.Combine(
                registry.LayoutPackagesRoot,
                "Releases",
                LayoutKey,
                "1.1.0",
                "layout.json");
            var release110BeforeActivation =
                await File.ReadAllBytesAsync(release110DefinitionPath);

            var activation110 = await submissions.ActivateAsync(
                LayoutKey,
                "1.1.0",
                superAdminToken,
                "Pipeline SuperAdmin",
                "Smoke activate v1.1.0");
            Assert(
                activation110.Reload.Succeeded
                && activation110.Previous?.Version == "1.0.0",
                "active pointer moves atomically from v1.0.0 to v1.1.0");
            AssertCurrent(registry, "1.1.0");

            var rollback100 = await submissions.RollbackAsync(
                LayoutKey,
                "1.0.0",
                superAdminToken,
                "Pipeline SuperAdmin",
                "Smoke rollback to v1.0.0");
            Assert(
                rollback100.Reload.Succeeded
                && rollback100.Previous?.Version == "1.1.0",
                "rollback atomically restores the v1.0.0 active pointer");
            AssertCurrent(registry, "1.0.0");
            Assert(
                registry.PublishedPackages.ContainsKey(
                    new WeddingLayoutReleaseId(LayoutKey, "1.1.0")),
                "rollback keeps immutable v1.1.0 published for later reactivation");
            Assert(
                release110BeforeActivation.SequenceEqual(
                    await File.ReadAllBytesAsync(release110DefinitionPath)),
                "activation and rollback do not mutate the v1.1.0 release");

            var submissionsBeforeUnsafeUpload =
                await submissions.ListAllAsync(superAdminToken);
            var unsafeRejected = false;
            try
            {
                await SubmitAsync(
                    submissions,
                    actor,
                    CreatePackage(
                        "2.0.0",
                        "#B85C66",
                        "<script>alert('unsafe')</script>"));
            }
            catch (LayoutPackageValidationException)
            {
                unsafeRejected = true;
            }

            Assert(
                unsafeRejected,
                "unsafe executable-looking authored text is rejected by validation");
            Assert(
                (await submissions.ListAllAsync(superAdminToken)).Count
                == submissionsBeforeUnsafeUpload.Count,
                "rejected unsafe upload leaves no submission record");

            var activePointerPath = Path.Combine(
                registry.LayoutPackagesRoot,
                "Active",
                LayoutKey + ".json");
            var validPointerBytes = await File.ReadAllBytesAsync(activePointerPath);
            await File.WriteAllTextAsync(activePointerPath, "{ invalid-pointer");

            var invalidReload = await registry.ReloadAsync();
            Assert(
                !invalidReload.Succeeded && !invalidReload.Changed,
                "invalid active pointer is rejected during reload");
            AssertCurrent(registry, "1.0.0");
            Assert(
                registry.PublishedPackages.ContainsKey(
                    new WeddingLayoutReleaseId(LayoutKey, "1.1.0")),
                "last-known-good cache remains intact after invalid pointer reload");

            await File.WriteAllBytesAsync(activePointerPath, validPointerBytes);
            var restoredReload = await registry.ReloadAsync();
            Assert(
                restoredReload.Succeeded,
                "restored active pointer reloads successfully");
            AssertCurrent(registry, "1.0.0");

            Assert(
                changedEvents >= 3,
                "cache invalidation raised catalog-change notifications without restart");
            Assert(
                audit.Entries.Any(entry =>
                    entry.Action == "ClassifyLayoutDefinition")
                && audit.Entries.Any(entry =>
                    entry.Action == "ApproveLayoutSubmission")
                && audit.Entries.Any(entry =>
                    entry.Action == "RollbackLayoutRelease"),
                "classification, approval, and rollback emit super-admin audit events");
        }
        finally
        {
            await registry.StopAsync(CancellationToken.None);
        }
    }

    private static async Task<WeddingLayoutSubmissionRecord> SubmitAsync(
        IWeddingLayoutSubmissionService submissions,
        WeddingCurrentUser actor,
        LayoutPackage package)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(
            package,
            LayoutPackageJson.CreateOptions(indented: true));
        await using var stream = new MemoryStream(bytes, writable: false);
        return await submissions.SubmitAsync(
            TenantSlug,
            actor,
            stream,
            CancellationToken.None);
    }

    private static void AssertWpfCompatibleRoundTrip(LayoutPackage package)
    {
        var options = LayoutPackageJson.CreateOptions(indented: true);
        var json = JsonSerializer.Serialize(package, options);
        var roundTripped = JsonSerializer.Deserialize<LayoutPackage>(json, options);
        Assert(
            roundTripped is not null
            && LayoutPackageValidator.Validate(roundTripped).IsValid
            && roundTripped.Definition.Presentation == package.Definition.Presentation
            && roundTripped.Definition.Transition == package.Definition.Transition,
            $"Contracts JSON round-trip validates {package.Manifest.Key}@{package.Manifest.Version}");
    }

    private static void AssertCurrent(
        IWeddingLayoutCatalogRegistry registry,
        string expectedVersion)
    {
        var descriptor = registry.Current.FindDescriptor(LayoutKey);
        Assert(
            descriptor?.CurrentVersion == expectedVersion,
            $"same registry instance resolves active {LayoutKey}@{expectedVersion}");
        Assert(
            registry.Current.FindRelease(LayoutKey, expectedVersion)?.IsImplemented == true,
            $"dynamic catalog release {LayoutKey}@{expectedVersion} is renderable");
    }

    private static LayoutPackage CreatePackage(
        string version,
        string primaryColor,
        string headingText = "",
        LayoutPresentationMode presentation = LayoutPresentationMode.Flow) =>
        new()
        {
            Manifest = new LayoutManifest
            {
                SchemaVersion = LayoutSchema.CurrentVersion,
                Key = LayoutKey,
                Version = version,
                Label = "Pipeline smoke layout",
                Description =
                    "A WPF-compatible block layout used by the executable pipeline smoke test.",
                Tier = LayoutTier.Premium,
            },
            Definition = new LayoutDefinition
            {
                Presentation = presentation,
                Transition = new LayoutTransitionDefinition
                {
                    Kind = presentation switch
                    {
                        LayoutPresentationMode.FlipCard =>
                            LayoutTransitionKind.FlipCard,
                        LayoutPresentationMode.PagedBook =>
                            LayoutTransitionKind.PageTurn,
                        _ => LayoutTransitionKind.None,
                    },
                    DurationMilliseconds = 680,
                    EnableSwipe = true,
                    EnableKeyboard = true,
                    ShowNavigation = true,
                },
                SectionOrder =
                [
                    LayoutSectionKey.Hero,
                    LayoutSectionKey.Invitation,
                ],
                StyleTokens =
                [
                    new LayoutStyleTokenValue
                    {
                        Token = LayoutStyleToken.PrimaryColor,
                        Value = primaryColor,
                    },
                    new LayoutStyleTokenValue
                    {
                        Token = LayoutStyleToken.BackgroundColor,
                        Value = "#FFFCF8",
                    },
                ],
                Root = new LayoutBlock
                {
                    Id = "page",
                    Kind = LayoutBlockKind.Page,
                    Binding = LayoutBindingKey.Invitation,
                    ContainerSettings = new LayoutContainerSettings(),
                    Children =
                    [
                        new LayoutBlock
                        {
                            Id = "hero",
                            Kind = LayoutBlockKind.Hero,
                            Binding = LayoutBindingKey.Invitation,
                            ContainerSettings = new LayoutContainerSettings(),
                            Children =
                            [
                                new LayoutBlock
                                {
                                    Id = "couple-name",
                                    Kind = LayoutBlockKind.Heading,
                                    Binding = LayoutBindingKey.CoupleName,
                                    Text = headingText,
                                    TextSettings = new LayoutTextSettings
                                    {
                                        Size = LayoutTextSize.Display,
                                        Weight = LayoutTextWeight.Semibold,
                                        Alignment = LayoutAlignment.Center,
                                    },
                                },
                            ],
                        },
                        new LayoutBlock
                        {
                            Id = "invitation-section",
                            Kind = LayoutBlockKind.Section,
                            Binding = LayoutBindingKey.Invitation,
                            ContainerSettings = new LayoutContainerSettings(),
                            Children =
                            [
                                new LayoutBlock
                                {
                                    Id = "invitation-copy",
                                    Kind = LayoutBlockKind.Text,
                                    Binding = LayoutBindingKey.Subtitle,
                                    TextSettings = new LayoutTextSettings
                                    {
                                        Size = LayoutTextSize.Body,
                                        Alignment = LayoutAlignment.Center,
                                    },
                                },
                            ],
                        },
                    ],
                },
            },
        };

    private static void Assert(bool condition, string description)
    {
        if (!condition)
        {
            throw new InvalidOperationException("Assertion failed: " + description);
        }

        Console.WriteLine("[PASS] " + description);
    }

    private static void SafeDeleteTemporaryDirectory(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var systemTemp = Path.TrimEndingDirectorySeparator(
            Path.GetFullPath(Path.GetTempPath()));
        var parent = Directory.GetParent(fullPath)?.FullName;
        var directoryName = Path.GetFileName(fullPath);
        var suffix = directoryName.StartsWith(
            TemporaryDirectoryPrefix,
            StringComparison.Ordinal)
            ? directoryName[TemporaryDirectoryPrefix.Length..]
            : "";

        if (!string.Equals(parent, systemTemp, StringComparison.OrdinalIgnoreCase)
            || !Guid.TryParseExact(suffix, "N", out _))
        {
            throw new InvalidOperationException(
                $"Refusing to delete unexpected smoke-test path '{fullPath}'.");
        }

        if (Directory.Exists(fullPath))
        {
            Directory.Delete(fullPath, recursive: true);
        }
    }

    private sealed class InMemoryTenantStore : ITenantStore
    {
        private readonly string _root;
        private readonly ConcurrentDictionary<string, TenantConfig> _tenants =
            new(StringComparer.OrdinalIgnoreCase);

        public InMemoryTenantStore(string root, params TenantConfig[] tenants)
        {
            _root = root;
            foreach (var tenant in tenants)
            {
                _tenants[tenant.Slug] = tenant;
            }
        }

        public Task<TenantConfig?> GetAsync(
            string slug,
            CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            _tenants.TryGetValue(slug, out var tenant);
            return Task.FromResult(tenant);
        }

        public Task<IReadOnlyList<TenantConfig>> GetAllAsync(
            CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<TenantConfig>>(
                _tenants.Values.ToArray());
        }

        public Task SaveAsync(
            TenantConfig config,
            CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            _tenants[config.Slug] = config;
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(
            string slug,
            CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(_tenants.ContainsKey(slug));
        }

        public Task DeleteAsync(
            string slug,
            CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            _tenants.TryRemove(slug, out _);
            return Task.CompletedTask;
        }

        public string GetTenantDataPath(string slug) =>
            Path.Combine(_root, "Tenants", slug);
    }

    private sealed class InMemorySuperAdminAuditLog : ISuperAdminAuditLog
    {
        private readonly ConcurrentQueue<AuditEntry> _entries = new();

        public IReadOnlyList<AuditEntry> Entries => _entries.ToArray();

        public Task WriteAsync(
            string action,
            string slug,
            string? detail = null,
            CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            _entries.Enqueue(new AuditEntry(action, slug, detail ?? ""));
            return Task.CompletedTask;
        }
    }

    private sealed record AuditEntry(string Action, string Slug, string Detail);
}
