using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Wedding.Layouts.Contracts;
using WeddingPlatform.Models;
using WeddingPlatform.Services;
using Xunit;

namespace WeddingPlatform.LayoutWorkflow.Tests;

public sealed class LayoutWorkflowIntegrationTests
{
    [Fact]
    public async Task Approve_activate_new_version_and_rollback_replace_catalog_without_restart()
    {
        var testRoot = Path.Combine(
            Path.GetTempPath(),
            "WeddingLayoutWorkflowTests",
            Guid.NewGuid().ToString("N"));
        var publicDataRoot = Path.Combine(testRoot, "App_Data", "Wedding");
        Directory.CreateDirectory(publicDataRoot);

        var options = new WeddingOptions
        {
            DataPath = publicDataRoot,
            SuperAdminPassword = "layout-workflow-test-secret",
        };
        var tenants = new JsonTenantStore(options);
        await tenants.SaveAsync(new TenantConfig
        {
            Slug = "sample-tenant",
            OwnerUserId = "oauth-owner",
        });

        var tokens = new SuperAdminSessionTokenService(options);
        var audit = new CapturingAuditLog();
        var registry = new FileSystemWeddingLayoutCatalogRegistry(
            options,
            NullLogger<FileSystemWeddingLayoutCatalogRegistry>.Instance);
        var service = new FileSystemWeddingLayoutSubmissionService(
            options,
            tenants,
            tokens,
            audit,
            registry);

        try
        {
            await registry.StartAsync(CancellationToken.None);
            var actor = new WeddingCurrentUser(
                "oauth-owner",
                "Test",
                "owner@example.test",
                "Owner");
            var superAdminToken = tokens.CreateToken();
            await service.SetDefinitionTierAsync(
                "sample-layout",
                LayoutTier.Free,
                superAdminToken,
                "Test SuperAdmin",
                "Classify integration-test layout");

            var v1Submission = await SubmitAsync(
                service,
                actor,
                CreatePackage("1.0.0", "#AA7755"));
            var v1Approval = await service.ApproveAsync(
                v1Submission.SubmissionId,
                superAdminToken,
                "Test SuperAdmin");

            Assert.True(v1Approval.Reload.Succeeded);
            Assert.True(v1Approval.Activated);
            Assert.Equal(
                "1.0.0",
                registry.Current.FindDescriptor("sample-layout")?.CurrentVersion);

            var v11Submission = await SubmitAsync(
                service,
                actor,
                CreatePackage("1.1.0", "#446688"));
            var v11Approval = await service.ApproveAsync(
                v11Submission.SubmissionId,
                superAdminToken,
                "Test SuperAdmin");

            Assert.True(v11Approval.Reload.Succeeded);
            Assert.Equal(
                "1.1.0",
                registry.Current.FindDescriptor("sample-layout")?.CurrentVersion);
            Assert.Equal(2, registry.PublishedPackages.Count);

            var rollback = await service.RollbackAsync(
                "sample-layout",
                "1.0.0",
                superAdminToken,
                "Test SuperAdmin",
                "Regression found in 1.1.0");

            Assert.True(rollback.Reload.Succeeded);
            Assert.Equal("1.1.0", rollback.Previous?.Version);
            Assert.Equal(
                "1.0.0",
                registry.Current.FindDescriptor("sample-layout")?.CurrentVersion);
            Assert.Contains(
                audit.Entries,
                entry => entry.Action == "RollbackLayoutRelease");

            var releaseRoot = Path.Combine(
                testRoot,
                "App_Data",
                "LayoutPackages",
                "Releases",
                "sample-layout");
            Assert.True(File.Exists(
                Path.Combine(releaseRoot, "1.0.0", "manifest.json")));
            Assert.True(File.Exists(
                Path.Combine(releaseRoot, "1.1.0", "manifest.json")));
        }
        finally
        {
            service.Dispose();
            await registry.StopAsync(CancellationToken.None);
            registry.Dispose();
            if (Directory.Exists(testRoot))
            {
                Directory.Delete(testRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Tenant_submission_requires_owner_or_administrator()
    {
        var testRoot = Path.Combine(
            Path.GetTempPath(),
            "WeddingLayoutWorkflowTests",
            Guid.NewGuid().ToString("N"));
        var publicDataRoot = Path.Combine(testRoot, "App_Data", "Wedding");
        Directory.CreateDirectory(publicDataRoot);
        var options = new WeddingOptions
        {
            DataPath = publicDataRoot,
            SuperAdminPassword = "layout-workflow-test-secret",
        };
        var tenants = new JsonTenantStore(options);
        await tenants.SaveAsync(new TenantConfig
        {
            Slug = "sample-tenant",
            OwnerUserId = "oauth-owner",
        });

        var registry = new FileSystemWeddingLayoutCatalogRegistry(
            options,
            NullLogger<FileSystemWeddingLayoutCatalogRegistry>.Instance);
        var service = new FileSystemWeddingLayoutSubmissionService(
            options,
            tenants,
            new SuperAdminSessionTokenService(options),
            new CapturingAuditLog(),
            registry);
        try
        {
            var outsider = new WeddingCurrentUser(
                "oauth-outsider",
                "Test",
                "outsider@example.test",
                "Outsider");
            await using var stream = PackageStream(CreatePackage("1.0.0", "#AA7755"));

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.SubmitAsync("sample-tenant", outsider, stream));
        }
        finally
        {
            service.Dispose();
            registry.Dispose();
            if (Directory.Exists(testRoot))
            {
                Directory.Delete(testRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Super_admin_can_submit_for_existing_tenant_with_server_owned_actor_identity()
    {
        await using var harness = await WorkflowHarness.CreateAsync();
        var superAdminToken = harness.Tokens.CreateToken();
        await using var stream = PackageStream(
            CreatePackage("1.0.0", "#AA7755"));

        var submitted = await harness.Service.SubmitAsSuperAdminAsync(
            "sample-tenant",
            superAdminToken,
            "Test SuperAdmin",
            stream);

        Assert.Equal("sample-tenant", submitted.TenantSlug);
        Assert.Equal("super-admin", submitted.SubmittedByUserId);
        Assert.Equal("Test SuperAdmin", submitted.SubmittedByDisplayName);
        Assert.Equal(WeddingLayoutSubmissionStatus.Pending, submitted.Status);
        var own = await harness.Service.ListOwnAsync(
            "sample-tenant",
            harness.Actor);
        Assert.Contains(
            own,
            candidate => candidate.SubmissionId == submitted.SubmissionId);
        Assert.Contains(
            harness.Audit.Entries,
            entry =>
                entry.Action == "SubmitLayoutAsSuperAdmin"
                && entry.Slug == "sample-tenant"
                && entry.Detail.Contains(
                    submitted.SubmissionId,
                    StringComparison.Ordinal));
    }

    [Fact]
    public async Task Super_admin_submission_rejects_invalid_session_token()
    {
        await using var harness = await WorkflowHarness.CreateAsync();
        await using var stream = PackageStream(
            CreatePackage("1.0.0", "#AA7755"));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            harness.Service.SubmitAsSuperAdminAsync(
                "sample-tenant",
                "not-a-valid-token",
                "Test SuperAdmin",
                stream));

        Assert.Empty(await harness.Service.ListOwnAsync(
            "sample-tenant",
            harness.Actor));
    }

    [Fact]
    public async Task Super_admin_submission_requires_existing_tenant()
    {
        await using var harness = await WorkflowHarness.CreateAsync();
        await using var stream = PackageStream(
            CreatePackage("1.0.0", "#AA7755"));

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            harness.Service.SubmitAsSuperAdminAsync(
                "missing-tenant",
                harness.Tokens.CreateToken(),
                "Test SuperAdmin",
                stream));

        Assert.Contains("missing-tenant", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Super_admin_submission_reuses_immutable_identity_validation()
    {
        await using var harness = await WorkflowHarness.CreateAsync();
        await SubmitAsync(
            harness.Service,
            harness.Actor,
            CreatePackage("1.0.0", "#AA7755"));
        await using var duplicate = PackageStream(
            CreatePackage("1.0.0", "#446688"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Service.SubmitAsSuperAdminAsync(
                "sample-tenant",
                harness.Tokens.CreateToken(),
                "Test SuperAdmin",
                duplicate));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Reload_failure_after_pointer_commit_restores_previous_pointer_and_catalog(
        bool throwReloadException)
    {
        await using var harness = await WorkflowHarness.CreateAsync();
        var superAdminToken = harness.Tokens.CreateToken();
        var v1 = await SubmitAsync(
            harness.Service,
            harness.Actor,
            CreatePackage("1.0.0", "#AA7755"));
        await harness.Service.ApproveAsync(v1.SubmissionId, superAdminToken, "Test SuperAdmin");
        var v11 = await SubmitAsync(
            harness.Service,
            harness.Actor,
            CreatePackage("1.1.0", "#446688"));
        await harness.Service.ApproveAsync(
            v11.SubmissionId,
            superAdminToken,
            "Test SuperAdmin",
            activate: false);

        var injectedRegistry = new ScriptedRegistry(
            harness.Registry,
            (attempt, cancellationToken) =>
            {
                Assert.False(cancellationToken.CanBeCanceled);
                if (attempt == 1)
                {
                    if (throwReloadException)
                    {
                        throw new IOException("Injected reload rejection");
                    }

                    return Task.FromResult(new WeddingLayoutReloadResult(
                        false,
                        false,
                        harness.Registry.PublishedPackages.Count,
                        "Injected reload rejection"));
                }

                return harness.Registry.ReloadAsync(cancellationToken);
            });
        using var activationService = new FileSystemWeddingLayoutSubmissionService(
            harness.Options,
            harness.Tenants,
            harness.Tokens,
            harness.Audit,
            injectedRegistry);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            activationService.ActivateAsync(
                "sample-layout",
                "1.1.0",
                superAdminToken,
                "Test SuperAdmin"));

        Assert.Contains("Injected reload rejection", error.Message);
        Assert.Equal(2, injectedRegistry.ReloadCount);
        Assert.Equal(
            "1.0.0",
            harness.Registry.Current.FindDescriptor("sample-layout")?.CurrentVersion);
        Assert.Equal("1.0.0", ReadActivePointerVersion(harness.TestRoot));
    }

    [Fact]
    public async Task Caller_cancellation_at_commit_does_not_interrupt_catalog_reload()
    {
        await using var harness = await WorkflowHarness.CreateAsync();
        var superAdminToken = harness.Tokens.CreateToken();
        var v1 = await SubmitAsync(
            harness.Service,
            harness.Actor,
            CreatePackage("1.0.0", "#AA7755"));
        await harness.Service.ApproveAsync(v1.SubmissionId, superAdminToken, "Test SuperAdmin");
        var v11 = await SubmitAsync(
            harness.Service,
            harness.Actor,
            CreatePackage("1.1.0", "#446688"));
        await harness.Service.ApproveAsync(
            v11.SubmissionId,
            superAdminToken,
            "Test SuperAdmin",
            activate: false);

        using var callerCancellation = new CancellationTokenSource();
        var injectedRegistry = new ScriptedRegistry(
            harness.Registry,
            (_, reloadToken) =>
            {
                Assert.False(reloadToken.CanBeCanceled);
                callerCancellation.Cancel();
                return harness.Registry.ReloadAsync(reloadToken);
            });
        using var activationService = new FileSystemWeddingLayoutSubmissionService(
            harness.Options,
            harness.Tenants,
            harness.Tokens,
            harness.Audit,
            injectedRegistry);

        var result = await activationService.ActivateAsync(
            "sample-layout",
            "1.1.0",
            superAdminToken,
            "Test SuperAdmin",
            cancellationToken: callerCancellation.Token);

        Assert.True(callerCancellation.IsCancellationRequested);
        Assert.True(result.Reload.Succeeded);
        Assert.Equal(1, injectedRegistry.ReloadCount);
        Assert.Equal(
            "1.1.0",
            harness.Registry.Current.FindDescriptor("sample-layout")?.CurrentVersion);
        Assert.Equal("1.1.0", ReadActivePointerVersion(harness.TestRoot));
    }

    [Fact]
    public async Task Cold_start_ignores_only_service_owned_orphan_active_temporary_files()
    {
        await using var harness = await WorkflowHarness.CreateAsync();
        var superAdminToken = harness.Tokens.CreateToken();
        var submission = await SubmitAsync(
            harness.Service,
            harness.Actor,
            CreatePackage("1.0.0", "#AA7755"));
        await harness.Service.ApproveAsync(
            submission.SubmissionId,
            superAdminToken,
            "Test SuperAdmin");

        var activeRoot = Path.Combine(
            harness.TestRoot,
            "App_Data",
            "LayoutPackages",
            "Active");
        var orphanPath = Path.Combine(
            activeRoot,
            $".__wlp-active-sample-layout.{Guid.NewGuid():N}.tmp");
        await File.WriteAllTextAsync(orphanPath, "{\"partial\":");
        var legacyOrphanPath = Path.Combine(
            activeRoot,
            $".sample-layout.json.{Guid.NewGuid():N}.tmp");
        await File.WriteAllTextAsync(legacyOrphanPath, "{\"partial\":");

        using var coldRegistry = new FileSystemWeddingLayoutCatalogRegistry(
            harness.Options,
            NullLogger<FileSystemWeddingLayoutCatalogRegistry>.Instance);
        await coldRegistry.StartAsync(CancellationToken.None);

        Assert.Equal(
            "1.0.0",
            coldRegistry.Current.FindDescriptor("sample-layout")?.CurrentVersion);
        Assert.True(File.Exists(orphanPath));
        Assert.True(File.Exists(legacyOrphanPath));

        var lookalikePath = Path.Combine(activeRoot, "not-an-owned-temp.tmp");
        await File.WriteAllTextAsync(lookalikePath, "{}");
        var rejected = await coldRegistry.ReloadAsync(CancellationToken.None);

        Assert.False(rejected.Succeeded);
        Assert.Equal(
            "1.0.0",
            coldRegistry.Current.FindDescriptor("sample-layout")?.CurrentVersion);
        File.Delete(lookalikePath);
        await coldRegistry.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Archive_inactive_unreferenced_approved_release_removes_release_and_submission()
    {
        await using var harness = await WorkflowHarness.CreateAsync();
        var superAdminToken = harness.Tokens.CreateToken();
        var v1 = await SubmitAsync(
            harness.Service,
            harness.Actor,
            CreatePackage("1.0.0", "#AA7755"));
        await harness.Service.ApproveAsync(
            v1.SubmissionId,
            superAdminToken,
            "Test SuperAdmin");
        var v11 = await SubmitAsync(
            harness.Service,
            harness.Actor,
            CreatePackage("1.1.0", "#446688"));
        await harness.Service.ApproveAsync(
            v11.SubmissionId,
            superAdminToken,
            "Test SuperAdmin");

        var result = await harness.Service.ArchiveAsync(
            v1.SubmissionId,
            superAdminToken,
            "Test SuperAdmin",
            "Retire unreferenced release");

        Assert.True(result.PublishedReleaseArchived);
        Assert.False(result.ActivePointerRemoved);
        Assert.NotNull(result.Reload);
        Assert.True(result.Reload!.Succeeded);
        Assert.Null(harness.Registry.Current.FindRelease("sample-layout", "1.0.0"));
        Assert.NotNull(harness.Registry.Current.FindRelease("sample-layout", "1.1.0"));
        Assert.Equal(
            "1.1.0",
            harness.Registry.Current.FindDescriptor("sample-layout")?.CurrentVersion);
        Assert.DoesNotContain(
            await harness.Service.ListAllAsync(superAdminToken),
            submission => submission.SubmissionId == v1.SubmissionId);
        Assert.False(Directory.Exists(ReleaseDirectory(
            harness.TestRoot,
            "sample-layout",
            "1.0.0")));
        Assert.False(Directory.Exists(SubmissionDirectory(
            harness.TestRoot,
            v1.SubmissionId)));
    }

    [Fact]
    public async Task Archive_active_release_with_another_version_requires_explicit_version_change()
    {
        await using var harness = await WorkflowHarness.CreateAsync();
        var superAdminToken = harness.Tokens.CreateToken();
        var active = await SubmitAsync(
            harness.Service,
            harness.Actor,
            CreatePackage("1.0.0", "#AA7755"));
        await harness.Service.ApproveAsync(
            active.SubmissionId,
            superAdminToken,
            "Test SuperAdmin");
        var inactive = await SubmitAsync(
            harness.Service,
            harness.Actor,
            CreatePackage("1.1.0", "#446688"));
        await harness.Service.ApproveAsync(
            inactive.SubmissionId,
            superAdminToken,
            "Test SuperAdmin",
            activate: false);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Service.ArchiveAsync(
                active.SubmissionId,
                superAdminToken,
                "Test SuperAdmin",
                "Must not choose a replacement implicitly"));

        Assert.Equal("1.0.0", ReadActivePointerVersion(harness.TestRoot));
        Assert.NotNull(harness.Registry.Current.FindRelease("sample-layout", "1.0.0"));
        Assert.NotNull(harness.Registry.Current.FindRelease("sample-layout", "1.1.0"));
        Assert.Contains(
            await harness.Service.ListAllAsync(superAdminToken),
            submission => submission.SubmissionId == active.SubmissionId);
        Assert.True(Directory.Exists(ReleaseDirectory(
            harness.TestRoot,
            "sample-layout",
            "1.0.0")));
    }

    [Fact]
    public async Task Archive_only_active_release_without_tenant_references_removes_pointer_and_catalog_entry()
    {
        await using var harness = await WorkflowHarness.CreateAsync();
        var superAdminToken = harness.Tokens.CreateToken();
        var submission = await SubmitAsync(
            harness.Service,
            harness.Actor,
            CreatePackage("1.0.0", "#AA7755"));
        await harness.Service.ApproveAsync(
            submission.SubmissionId,
            superAdminToken,
            "Test SuperAdmin");

        var result = await harness.Service.ArchiveAsync(
            submission.SubmissionId,
            superAdminToken,
            "Test SuperAdmin",
            "Remove unused layout key");

        Assert.True(result.PublishedReleaseArchived);
        Assert.True(result.ActivePointerRemoved);
        Assert.NotNull(result.Reload);
        Assert.True(result.Reload!.Succeeded);
        Assert.Null(harness.Registry.Current.FindDescriptor("sample-layout"));
        Assert.Null(harness.Registry.Current.FindRelease("sample-layout", "1.0.0"));
        Assert.Empty(harness.Registry.PublishedPackages);
        Assert.False(File.Exists(ActivePointerPath(
            harness.TestRoot,
            "sample-layout")));
        Assert.DoesNotContain(
            await harness.Service.ListAllAsync(superAdminToken),
            candidate => candidate.SubmissionId == submission.SubmissionId);
    }

    [Fact]
    public async Task Archive_inactive_release_referenced_by_pinned_tenant_is_blocked()
    {
        await using var harness = await WorkflowHarness.CreateAsync();
        var superAdminToken = harness.Tokens.CreateToken();
        var pinned = await SubmitAsync(
            harness.Service,
            harness.Actor,
            CreatePackage("1.0.0", "#AA7755"));
        await harness.Service.ApproveAsync(
            pinned.SubmissionId,
            superAdminToken,
            "Test SuperAdmin");
        var active = await SubmitAsync(
            harness.Service,
            harness.Actor,
            CreatePackage("1.1.0", "#446688"));
        await harness.Service.ApproveAsync(
            active.SubmissionId,
            superAdminToken,
            "Test SuperAdmin");
        var tenant = await harness.Tenants.GetAsync("sample-tenant")
            ?? throw new InvalidOperationException("The test tenant was not found.");
        tenant.DesignSettings = new DesignSettings
        {
            LayoutMode = Wedding.Common.WeddingLayoutMode.Unknown,
            LayoutKey = "sample-layout",
            LayoutVersion = "1.0.0",
            FollowActiveLayoutVersion = false,
        };
        await harness.Tenants.SaveAsync(tenant);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Service.ArchiveAsync(
                pinned.SubmissionId,
                superAdminToken,
                "Test SuperAdmin",
                "Pinned releases must remain available"));

        Assert.Equal("1.1.0", ReadActivePointerVersion(harness.TestRoot));
        Assert.NotNull(harness.Registry.Current.FindRelease("sample-layout", "1.0.0"));
        Assert.Contains(
            await harness.Service.ListAllAsync(superAdminToken),
            submission => submission.SubmissionId == pinned.SubmissionId);
        Assert.True(Directory.Exists(ReleaseDirectory(
            harness.TestRoot,
            "sample-layout",
            "1.0.0")));
    }

    [Fact]
    public async Task Archived_release_identity_cannot_be_submitted_again()
    {
        await using var harness = await WorkflowHarness.CreateAsync();
        var superAdminToken = harness.Tokens.CreateToken();
        var archived = await SubmitAsync(
            harness.Service,
            harness.Actor,
            CreatePackage("1.0.0", "#AA7755"));
        await harness.Service.ApproveAsync(
            archived.SubmissionId,
            superAdminToken,
            "Test SuperAdmin");
        var active = await SubmitAsync(
            harness.Service,
            harness.Actor,
            CreatePackage("1.1.0", "#446688"));
        await harness.Service.ApproveAsync(
            active.SubmissionId,
            superAdminToken,
            "Test SuperAdmin");
        await harness.Service.ArchiveAsync(
            archived.SubmissionId,
            superAdminToken,
            "Test SuperAdmin",
            "Keep the release identity retired");

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await SubmitAsync(
                harness.Service,
                harness.Actor,
                CreatePackage("1.0.0", "#118855"));
        });

        Assert.Equal(
            "1.1.0",
            harness.Registry.Current.FindDescriptor("sample-layout")?.CurrentVersion);
        Assert.Single(await harness.Service.ListAllAsync(superAdminToken));
        Assert.False(Directory.Exists(ReleaseDirectory(
            harness.TestRoot,
            "sample-layout",
            "1.0.0")));
    }

    [Fact]
    public async Task Purge_archived_release_allows_same_owner_to_reuse_identity_and_preserves_policy()
    {
        await using var harness = await WorkflowHarness.CreateAsync();
        var superAdminToken = harness.Tokens.CreateToken();
        await harness.Service.SetDefinitionTierAsync(
            "sample-layout",
            LayoutTier.Premium,
            superAdminToken,
            "Test SuperAdmin",
            "Keep the key-level classification across release replacement");
        var policyBefore = (await harness.Service.ListDefinitionPoliciesAsync(
                superAdminToken))
            .Single(policy => policy.LayoutKey == "sample-layout");
        var retired = await SubmitAsync(
            harness.Service,
            harness.Actor,
            CreatePackage("1.0.0", "#AA7755"));
        await harness.Service.ApproveAsync(
            retired.SubmissionId,
            superAdminToken,
            "Test SuperAdmin");
        var active = await SubmitAsync(
            harness.Service,
            harness.Actor,
            CreatePackage("1.1.0", "#446688"));
        await harness.Service.ApproveAsync(
            active.SubmissionId,
            superAdminToken,
            "Test SuperAdmin");
        await harness.Service.ArchiveAsync(
            retired.SubmissionId,
            superAdminToken,
            "Test SuperAdmin",
            "Retire old release before permanent deletion");

        var archived = Assert.Single(
            await harness.Service.ListArchivedAsync(superAdminToken));
        Assert.Equal(retired.SubmissionId, archived.SubmissionId);
        Assert.False(archived.PayloadPurged);

        var purge = await harness.Service.PurgeArchivedAsync(
            retired.SubmissionId,
            superAdminToken,
            "Test SuperAdmin",
            "Permanently delete retired payload");

        Assert.False(purge.AlreadyPurged);
        Assert.True(purge.Archive.PayloadPurged);
        Assert.True(purge.DefinitionPolicyPreserved);
        Assert.Empty(await harness.Service.ListArchivedAsync(superAdminToken));
        Assert.Equal(
            policyBefore,
            (await harness.Service.ListDefinitionPoliciesAsync(superAdminToken))
            .Single(policy => policy.LayoutKey == "sample-layout"));
        Assert.Equal(
            "1.1.0",
            harness.Registry.Current.FindDescriptor("sample-layout")
                ?.CurrentVersion);

        var archiveDirectory = ArchivedReleaseDirectory(
            harness.TestRoot,
            "sample-layout",
            "1.0.0");
        Assert.False(Directory.Exists(archiveDirectory));
        var purgeHistoryDirectory = PurgedArchiveDirectory(
            harness.TestRoot,
            retired.SubmissionId);
        Assert.Equal(
            ["archive.json"],
            Directory.EnumerateFileSystemEntries(purgeHistoryDirectory)
                .Select(path => Path.GetFileName(path)!)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray());
        using (var tombstone = JsonDocument.Parse(
                   await File.ReadAllTextAsync(
                       Path.Combine(purgeHistoryDirectory, "archive.json"))))
        {
            Assert.True(
                tombstone.RootElement.GetProperty("payloadPurged").GetBoolean());
            Assert.Equal(
                retired.SubmissionId,
                tombstone.RootElement.GetProperty("submissionId").GetString());
        }

        await harness.Tenants.SaveAsync(new TenantConfig
        {
            Slug = "other-tenant",
            OwnerUserId = "other-owner",
        });
        var otherOwner = new WeddingCurrentUser(
            "other-owner",
            "Other",
            "other@example.test",
            "Other owner");
        await using (var foreignPackage = PackageStream(
                         CreatePackage("1.0.0", "#991144")))
        {
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                harness.Service.SubmitAsync(
                    "other-tenant",
                    otherOwner,
                    foreignPackage));
        }

        var replacement = await SubmitAsync(
            harness.Service,
            harness.Actor,
            CreatePackage("1.0.0", "#118855", LayoutTier.Free));
        Assert.NotEqual(retired.SubmissionId, replacement.SubmissionId);
        await harness.Service.ApproveAsync(
            replacement.SubmissionId,
            superAdminToken,
            "Test SuperAdmin",
            activate: false);
        Assert.Equal(
            Wedding.Common.WeddingLayoutTier.Premium,
            harness.Registry.Current.FindDescriptor("sample-layout")?.Tier);

        await harness.Service.ArchiveAsync(
            replacement.SubmissionId,
            superAdminToken,
            "Test SuperAdmin",
            "Retire the replacement release");
        Assert.True(Directory.Exists(archiveDirectory));
        var replacementPurge = await harness.Service.PurgeArchivedAsync(
            replacement.SubmissionId,
            superAdminToken,
            "Test SuperAdmin",
            "Permanently delete the replacement payload");

        Assert.True(replacementPurge.Archive.PayloadPurged);
        Assert.True(replacementPurge.DefinitionPolicyPreserved);
        Assert.False(Directory.Exists(archiveDirectory));
        Assert.True(Directory.Exists(PurgedArchiveDirectory(
            harness.TestRoot,
            replacement.SubmissionId)));
        Assert.Equal(
            policyBefore,
            (await harness.Service.ListDefinitionPoliciesAsync(superAdminToken))
            .Single(policy => policy.LayoutKey == "sample-layout"));
        Assert.Empty(await harness.Service.ListArchivedAsync(superAdminToken));
        Assert.True((await harness.Service.PurgeArchivedAsync(
            retired.SubmissionId,
            superAdminToken,
            "Test SuperAdmin",
            "Idempotent retry")).AlreadyPurged);
        Assert.Contains(
            harness.Audit.Entries,
            entry => entry.Action == "PurgeArchivedLayoutRelease"
                && entry.Detail.Contains(
                    retired.SubmissionId,
                    StringComparison.Ordinal));
        Assert.Contains(
            harness.Audit.Entries,
            entry => entry.Action == "PurgeArchivedLayoutRelease"
                && entry.Detail.Contains(
                    replacement.SubmissionId,
                    StringComparison.Ordinal));
    }

    [Fact]
    public async Task Archived_final_pending_submission_purge_removes_orphaned_policy()
    {
        await using var harness = await WorkflowHarness.CreateAsync();
        var superAdminToken = harness.Tokens.CreateToken();
        var pending = await SubmitAsync(
            harness.Service,
            harness.Actor,
            CreatePackage("1.0.0", "#AA7755"));
        await harness.Service.ArchiveAsync(
            pending.SubmissionId,
            superAdminToken,
            "Test SuperAdmin",
            "Withdraw test upload");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            SubmitAsync(
                harness.Service,
                harness.Actor,
                CreatePackage("1.0.0", "#118855")));

        var purge = await harness.Service.PurgeArchivedAsync(
            pending.SubmissionId,
            superAdminToken,
            "Test SuperAdmin",
            "Delete withdrawn package");

        Assert.True(purge.Archive.PayloadPurged);
        Assert.False(purge.DefinitionPolicyPreserved);
        Assert.Empty(await harness.Service.ListArchivedAsync(superAdminToken));
        var archiveDirectory = ArchivedSubmissionDirectory(
            harness.TestRoot,
            pending.SubmissionId);
        Assert.False(Directory.Exists(archiveDirectory));
        var purgeHistoryDirectory = PurgedArchiveDirectory(
            harness.TestRoot,
            pending.SubmissionId);
        Assert.Equal(
            ["archive.json"],
            Directory.EnumerateFileSystemEntries(purgeHistoryDirectory)
                .Select(path => Path.GetFileName(path)!)
                .ToArray());
        Assert.False(File.Exists(DefinitionPolicyPath(
            harness.TestRoot,
            "sample-layout")));
        Assert.False(harness.Registry.DefinitionPolicies.ContainsKey(
            "sample-layout"));
        Assert.DoesNotContain(
            await harness.Service.ListDefinitionPoliciesAsync(superAdminToken),
            policy => string.Equals(
                policy.LayoutKey,
                "sample-layout",
                StringComparison.OrdinalIgnoreCase));

        var replacement = await SubmitAsync(
            harness.Service,
            harness.Actor,
            CreatePackage("1.0.0", "#118855"));
        Assert.NotEqual(pending.SubmissionId, replacement.SubmissionId);
    }

    [Fact]
    public async Task Purge_preserves_policy_while_another_live_submission_uses_the_key()
    {
        await using var harness = await WorkflowHarness.CreateAsync();
        var superAdminToken = harness.Tokens.CreateToken();
        var retired = await SubmitAsync(
            harness.Service,
            harness.Actor,
            CreatePackage("1.0.0", "#AA7755"));
        await harness.Service.ArchiveAsync(
            retired.SubmissionId,
            superAdminToken,
            "Test SuperAdmin",
            "Retire the first pending version");
        var live = await SubmitAsync(
            harness.Service,
            harness.Actor,
            CreatePackage("1.0.1", "#446688"));

        var purge = await harness.Service.PurgeArchivedAsync(
            retired.SubmissionId,
            superAdminToken,
            "Test SuperAdmin",
            "Purge only the retired pending version");

        Assert.True(purge.DefinitionPolicyPreserved);
        Assert.True(File.Exists(DefinitionPolicyPath(
            harness.TestRoot,
            "sample-layout")));
        Assert.True(harness.Registry.DefinitionPolicies.ContainsKey(
            "sample-layout"));
        Assert.Contains(
            await harness.Service.ListAllAsync(superAdminToken),
            submission => submission.SubmissionId == live.SubmissionId);
    }

    [Fact]
    public async Task Purge_preserves_policy_until_the_last_recoverable_archive_is_deleted()
    {
        await using var harness = await WorkflowHarness.CreateAsync();
        var superAdminToken = harness.Tokens.CreateToken();
        var first = await SubmitAsync(
            harness.Service,
            harness.Actor,
            CreatePackage("1.0.0", "#AA7755"));
        await harness.Service.ArchiveAsync(
            first.SubmissionId,
            superAdminToken,
            "Test SuperAdmin",
            "Archive first pending version");
        var second = await SubmitAsync(
            harness.Service,
            harness.Actor,
            CreatePackage("1.0.1", "#446688"));
        await harness.Service.ArchiveAsync(
            second.SubmissionId,
            superAdminToken,
            "Test SuperAdmin",
            "Archive second pending version");

        var firstPurge = await harness.Service.PurgeArchivedAsync(
            first.SubmissionId,
            superAdminToken,
            "Test SuperAdmin",
            "Keep policy for the other recoverable archive");

        Assert.True(firstPurge.DefinitionPolicyPreserved);
        Assert.True(File.Exists(DefinitionPolicyPath(
            harness.TestRoot,
            "sample-layout")));
        Assert.Single(await harness.Service.ListArchivedAsync(superAdminToken));

        var finalPurge = await harness.Service.PurgeArchivedAsync(
            second.SubmissionId,
            superAdminToken,
            "Test SuperAdmin",
            "Delete the final recoverable archive");

        Assert.False(finalPurge.DefinitionPolicyPreserved);
        Assert.False(File.Exists(DefinitionPolicyPath(
            harness.TestRoot,
            "sample-layout")));
        Assert.False(harness.Registry.DefinitionPolicies.ContainsKey(
            "sample-layout"));
        Assert.Empty(await harness.Service.ListArchivedAsync(superAdminToken));
    }

    [Fact]
    public async Task Listing_policies_lazily_prunes_a_legacy_orphan_with_a_purge_receipt()
    {
        await using var harness = await WorkflowHarness.CreateAsync();
        var superAdminToken = harness.Tokens.CreateToken();
        var policyPath = DefinitionPolicyPath(
            harness.TestRoot,
            "sample-layout");
        var legacyPolicyJson = await File.ReadAllTextAsync(policyPath);
        var pending = await SubmitAsync(
            harness.Service,
            harness.Actor,
            CreatePackage("1.0.0", "#AA7755"));
        await harness.Service.ArchiveAsync(
            pending.SubmissionId,
            superAdminToken,
            "Test SuperAdmin",
            "Create a permanent-delete receipt");
        await harness.Service.PurgeArchivedAsync(
            pending.SubmissionId,
            superAdminToken,
            "Test SuperAdmin",
            "Delete the final payload");
        Assert.False(File.Exists(policyPath));

        // Simulate a policy orphan left by an older build.
        await File.WriteAllTextAsync(policyPath, legacyPolicyJson);
        var legacyReload = await harness.Registry.ReloadAsync();
        Assert.True(legacyReload.Succeeded);
        Assert.True(harness.Registry.DefinitionPolicies.ContainsKey(
            "sample-layout"));

        var policies = await harness.Service.ListDefinitionPoliciesAsync(
            superAdminToken);

        Assert.DoesNotContain(
            policies,
            policy => string.Equals(
                policy.LayoutKey,
                "sample-layout",
                StringComparison.OrdinalIgnoreCase));
        Assert.False(File.Exists(policyPath));
        Assert.False(harness.Registry.DefinitionPolicies.ContainsKey(
            "sample-layout"));
    }

    [Fact]
    public async Task Policy_reload_failure_after_committed_purge_restores_the_policy()
    {
        await using var harness = await WorkflowHarness.CreateAsync();
        var superAdminToken = harness.Tokens.CreateToken();
        var pending = await SubmitAsync(
            harness.Service,
            harness.Actor,
            CreatePackage("1.0.0", "#AA7755"));
        await harness.Service.ArchiveAsync(
            pending.SubmissionId,
            superAdminToken,
            "Test SuperAdmin",
            "Prepare final archived payload");

        var injectedRegistry = new ScriptedRegistry(
            harness.Registry,
            (attempt, cancellationToken) =>
            {
                Assert.False(cancellationToken.CanBeCanceled);
                if (attempt == 1)
                {
                    return Task.FromResult(new WeddingLayoutReloadResult(
                        false,
                        false,
                        harness.Registry.PublishedPackages.Count,
                        "Injected policy cleanup reload rejection"));
                }

                return harness.Registry.ReloadAsync(cancellationToken);
            });
        using var purgeService = new FileSystemWeddingLayoutSubmissionService(
            harness.Options,
            harness.Tenants,
            harness.Tokens,
            harness.Audit,
            injectedRegistry);

        var purge = await purgeService.PurgeArchivedAsync(
            pending.SubmissionId,
            superAdminToken,
            "Test SuperAdmin",
            "Purge payload while preserving policy on reload failure");

        Assert.True(purge.Archive.PayloadPurged);
        Assert.True(purge.DefinitionPolicyPreserved);
        Assert.Equal(2, injectedRegistry.ReloadCount);
        Assert.True(File.Exists(DefinitionPolicyPath(
            harness.TestRoot,
            "sample-layout")));
        Assert.True(harness.Registry.DefinitionPolicies.ContainsKey(
            "sample-layout"));
        Assert.True(Directory.Exists(PurgedArchiveDirectory(
            harness.TestRoot,
            pending.SubmissionId)));
    }

    [Fact]
    public async Task Legacy_purged_release_tombstone_is_migrated_before_identity_reuse()
    {
        await using var harness = await WorkflowHarness.CreateAsync();
        var superAdminToken = harness.Tokens.CreateToken();
        var original = await SubmitAsync(
            harness.Service,
            harness.Actor,
            CreatePackage("1.0.0", "#AA7755"));
        await harness.Service.ApproveAsync(
            original.SubmissionId,
            superAdminToken,
            "Test SuperAdmin");
        await harness.Service.ArchiveAsync(
            original.SubmissionId,
            superAdminToken,
            "Test SuperAdmin",
            "Prepare a legacy purge tombstone");
        await harness.Service.PurgeArchivedAsync(
            original.SubmissionId,
            superAdminToken,
            "Test SuperAdmin",
            "Permanently delete the original payload");

        var purgeHistoryDirectory = PurgedArchiveDirectory(
            harness.TestRoot,
            original.SubmissionId);
        var legacyArchiveDirectory = ArchivedReleaseDirectory(
            harness.TestRoot,
            "sample-layout",
            "1.0.0");
        Directory.CreateDirectory(
            Path.GetDirectoryName(legacyArchiveDirectory)
            ?? throw new InvalidOperationException(
                "The legacy archive parent is unavailable."));
        Directory.Move(purgeHistoryDirectory, legacyArchiveDirectory);
        var interruptedPurgeStaging = Path.Combine(
            harness.TestRoot,
            "App_Data",
            "LayoutPackages",
            "Staging",
            $"purge-{original.SubmissionId}");
        Directory.CreateDirectory(Path.Combine(
            interruptedPurgeStaging,
            "submission"));
        await File.WriteAllTextAsync(
            Path.Combine(
                interruptedPurgeStaging,
                "submission",
                "package.json"),
            "{}");

        var replacement = await SubmitAsync(
            harness.Service,
            harness.Actor,
            CreatePackage("1.0.0", "#118855"));

        Assert.NotEqual(original.SubmissionId, replacement.SubmissionId);
        Assert.False(Directory.Exists(legacyArchiveDirectory));
        Assert.True(Directory.Exists(purgeHistoryDirectory));
        Assert.False(Directory.Exists(interruptedPurgeStaging));

        await harness.Service.SetDefinitionTierAsync(
            "sample-layout",
            LayoutTier.Free,
            superAdminToken,
            "Test SuperAdmin",
            "Reclassify the reused key after its prior lifecycle was purged");
        await harness.Service.ApproveAsync(
            replacement.SubmissionId,
            superAdminToken,
            "Test SuperAdmin");
        await harness.Service.ArchiveAsync(
            replacement.SubmissionId,
            superAdminToken,
            "Test SuperAdmin",
            "Retire the replacement after lazy migration");
        await harness.Service.PurgeArchivedAsync(
            replacement.SubmissionId,
            superAdminToken,
            "Test SuperAdmin",
            "Permanently delete the replacement payload");

        Assert.False(Directory.Exists(legacyArchiveDirectory));
        Assert.True(Directory.Exists(PurgedArchiveDirectory(
            harness.TestRoot,
            replacement.SubmissionId)));
    }

    [Fact]
    public async Task Archive_and_purge_notify_other_consumers_after_the_live_record_is_removed()
    {
        await using var harness = await WorkflowHarness.CreateAsync();
        var superAdminToken = harness.Tokens.CreateToken();
        var changes =
            new System.Collections.Concurrent.ConcurrentQueue<
                WeddingLayoutSubmissionsChangedEventArgs>();
        harness.Service.Changed += (_, args) => changes.Enqueue(args);

        var pending = await SubmitAsync(
            harness.Service,
            harness.Actor,
            CreatePackage("1.0.0", "#AA7755"));

        Assert.True(changes.TryDequeue(out var submitted));
        Assert.Equal(WeddingLayoutSubmissionChangeKind.Submitted, submitted.Kind);
        Assert.Equal("sample-tenant", submitted.TenantSlug);
        Assert.Equal(pending.SubmissionId, submitted.SubmissionId);

        await harness.Service.ArchiveAsync(
            pending.SubmissionId,
            superAdminToken,
            "Test SuperAdmin",
            "Remove from the owner's home list");

        Assert.True(changes.TryDequeue(out var archived));
        Assert.Equal(WeddingLayoutSubmissionChangeKind.Archived, archived.Kind);
        Assert.Equal("sample-tenant", archived.TenantSlug);
        Assert.Equal(pending.SubmissionId, archived.SubmissionId);
        Assert.DoesNotContain(
            await harness.Service.ListOwnAsync("sample-tenant", harness.Actor),
            submission => submission.SubmissionId == pending.SubmissionId);

        await harness.Service.PurgeArchivedAsync(
            pending.SubmissionId,
            superAdminToken,
            "Test SuperAdmin",
            "Permanently remove the archived payload");

        Assert.True(changes.TryDequeue(out var purged));
        Assert.Equal(WeddingLayoutSubmissionChangeKind.Purged, purged.Kind);
        Assert.Equal("sample-tenant", purged.TenantSlug);
        Assert.Equal(pending.SubmissionId, purged.SubmissionId);
        Assert.Empty(changes);
    }

    [Fact]
    public async Task Archive_and_purge_three_versions_never_leave_owner_list_records()
    {
        await using var harness = await WorkflowHarness.CreateAsync();
        var superAdminToken = harness.Tokens.CreateToken();
        var v100 = await SubmitAsync(
            harness.Service,
            harness.Actor,
            CreatePackage("1.0.0", "#AA7755"));
        await harness.Service.ApproveAsync(
            v100.SubmissionId,
            superAdminToken,
            "Test SuperAdmin");
        var v101 = await SubmitAsync(
            harness.Service,
            harness.Actor,
            CreatePackage("1.0.1", "#446688"));
        await harness.Service.ApproveAsync(
            v101.SubmissionId,
            superAdminToken,
            "Test SuperAdmin");
        var v102 = await SubmitAsync(
            harness.Service,
            harness.Actor,
            CreatePackage("1.0.2", "#228866"));
        await harness.Service.ApproveAsync(
            v102.SubmissionId,
            superAdminToken,
            "Test SuperAdmin");

        Assert.Equal(
            ["1.0.2", "1.0.1", "1.0.0"],
            (await harness.Service.ListOwnAsync("sample-tenant", harness.Actor))
            .Select(submission => submission.LayoutVersion)
            .ToArray());

        await harness.Service.RollbackAsync(
            "sample-layout",
            "1.0.1",
            superAdminToken,
            "Test SuperAdmin",
            "Keep the middle version active while retiring newer payload");
        Assert.Equal("1.0.1", ReadActivePointerVersion(harness.TestRoot));

        await harness.Service.ArchiveAsync(
            v102.SubmissionId,
            superAdminToken,
            "Test SuperAdmin",
            "Retire inactive 1.0.2");
        Assert.Equal(
            ["1.0.1", "1.0.0"],
            (await harness.Service.ListOwnAsync("sample-tenant", harness.Actor))
            .Select(submission => submission.LayoutVersion)
            .ToArray());
        await harness.Service.PurgeArchivedAsync(
            v102.SubmissionId,
            superAdminToken,
            "Test SuperAdmin",
            "Purge inactive 1.0.2 payload");
        Assert.Equal(
            ["1.0.1", "1.0.0"],
            (await harness.Service.ListOwnAsync("sample-tenant", harness.Actor))
            .Select(submission => submission.LayoutVersion)
            .ToArray());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Service.ArchiveAsync(
                v101.SubmissionId,
                superAdminToken,
                "Test SuperAdmin",
                "An active release cannot be retired while 1.0.0 remains"));
        Assert.Contains(
            await harness.Service.ListOwnAsync("sample-tenant", harness.Actor),
            submission => submission.SubmissionId == v101.SubmissionId);

        await harness.Service.RollbackAsync(
            "sample-layout",
            "1.0.0",
            superAdminToken,
            "Test SuperAdmin",
            "Move the active pointer before retiring 1.0.1");
        await harness.Service.ArchiveAsync(
            v101.SubmissionId,
            superAdminToken,
            "Test SuperAdmin",
            "Retire inactive 1.0.1");
        await harness.Service.PurgeArchivedAsync(
            v101.SubmissionId,
            superAdminToken,
            "Test SuperAdmin",
            "Purge inactive 1.0.1 payload");
        Assert.Equal(
            ["1.0.0"],
            (await harness.Service.ListOwnAsync("sample-tenant", harness.Actor))
            .Select(submission => submission.LayoutVersion)
            .ToArray());

        var soleActiveArchive = await harness.Service.ArchiveAsync(
            v100.SubmissionId,
            superAdminToken,
            "Test SuperAdmin",
            "Retire the final active release");
        Assert.True(soleActiveArchive.ActivePointerRemoved);
        Assert.Empty(
            await harness.Service.ListOwnAsync("sample-tenant", harness.Actor));
        await harness.Service.PurgeArchivedAsync(
            v100.SubmissionId,
            superAdminToken,
            "Test SuperAdmin",
            "Purge final 1.0.0 payload");
        Assert.Empty(
            await harness.Service.ListOwnAsync("sample-tenant", harness.Actor));
        Assert.False(File.Exists(DefinitionPolicyPath(
            harness.TestRoot,
            "sample-layout")));
        Assert.False(harness.Registry.DefinitionPolicies.ContainsKey(
            "sample-layout"));
    }

    [Fact]
    public async Task Purge_rechecks_tenant_pins_created_after_archive()
    {
        await using var harness = await WorkflowHarness.CreateAsync();
        var superAdminToken = harness.Tokens.CreateToken();
        var retired = await SubmitAsync(
            harness.Service,
            harness.Actor,
            CreatePackage("1.0.0", "#AA7755"));
        await harness.Service.ApproveAsync(
            retired.SubmissionId,
            superAdminToken,
            "Test SuperAdmin");
        var active = await SubmitAsync(
            harness.Service,
            harness.Actor,
            CreatePackage("1.1.0", "#446688"));
        await harness.Service.ApproveAsync(
            active.SubmissionId,
            superAdminToken,
            "Test SuperAdmin");
        await harness.Service.ArchiveAsync(
            retired.SubmissionId,
            superAdminToken,
            "Test SuperAdmin",
            "Archive before a later tenant pin");

        var tenant = await harness.Tenants.GetAsync("sample-tenant")
            ?? throw new InvalidOperationException("The test tenant was not found.");
        tenant.DesignSettings = new DesignSettings
        {
            LayoutMode = Wedding.Common.WeddingLayoutMode.Unknown,
            LayoutKey = "sample-layout",
            LayoutVersion = "1.0.0",
            FollowActiveLayoutVersion = false,
        };
        await harness.Tenants.SaveAsync(tenant);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Service.PurgeArchivedAsync(
                retired.SubmissionId,
                superAdminToken,
                "Test SuperAdmin",
                "Pinned payload must remain recoverable"));

        var archived = Assert.Single(
            await harness.Service.ListArchivedAsync(superAdminToken));
        Assert.False(archived.PayloadPurged);
        Assert.True(Directory.Exists(Path.Combine(
            ArchivedReleaseDirectory(
                harness.TestRoot,
                "sample-layout",
                "1.0.0"),
            "release")));
    }

    [Fact]
    public async Task Purge_requires_archive_first()
    {
        await using var harness = await WorkflowHarness.CreateAsync();
        var superAdminToken = harness.Tokens.CreateToken();
        var pending = await SubmitAsync(
            harness.Service,
            harness.Actor,
            CreatePackage("1.0.0", "#AA7755"));

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            harness.Service.PurgeArchivedAsync(
                pending.SubmissionId,
                superAdminToken,
                "Test SuperAdmin",
                "Direct deletion is forbidden"));

        Assert.Contains(
            await harness.Service.ListAllAsync(superAdminToken),
            candidate => candidate.SubmissionId == pending.SubmissionId);
        Assert.True(Directory.Exists(SubmissionDirectory(
            harness.TestRoot,
            pending.SubmissionId)));
    }

    [Fact]
    public async Task Approve_rejects_custom_layout_without_server_tier_policy()
    {
        await using var harness = await WorkflowHarness.CreateAsync(
            assignDefaultPolicy: false);
        var superAdminToken = harness.Tokens.CreateToken();
        var submission = await SubmitAsync(
            harness.Service,
            harness.Actor,
            CreatePackage("1.0.0", "#AA7755", LayoutTier.Premium));

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await harness.Service.ApproveAsync(
                submission.SubmissionId,
                superAdminToken,
                "Test SuperAdmin");
        });

        var stored = Assert.Single(
            await harness.Service.ListAllAsync(superAdminToken));
        Assert.Equal(WeddingLayoutSubmissionStatus.Pending, stored.Status);
        Assert.Empty(harness.Registry.PublishedPackages);
        Assert.Null(harness.Registry.Current.FindDescriptor("sample-layout"));
        Assert.DoesNotContain(
            await harness.Service.ListDefinitionPoliciesAsync(superAdminToken),
            policy => string.Equals(
                policy.LayoutKey,
                "sample-layout",
                StringComparison.OrdinalIgnoreCase));
        Assert.False(Directory.Exists(ReleaseDirectory(
            harness.TestRoot,
            "sample-layout",
            "1.0.0")));
    }

    [Fact]
    public async Task Legacy_manifest_seed_is_not_a_policy_until_super_admin_confirms_it()
    {
        await using var harness = await WorkflowHarness.CreateAsync(
            assignDefaultPolicy: false);
        var policyDirectory = Path.Combine(
            harness.TestRoot,
            "App_Data",
            "LayoutPackages",
            "Policies");
        Directory.CreateDirectory(policyDirectory);
        var policyPath = Path.Combine(policyDirectory, "sample-layout.json");
        await File.WriteAllTextAsync(
            policyPath,
            JsonSerializer.Serialize(
                new LayoutDefinitionPolicy
                {
                    SchemaVersion =
                        LayoutDefinitionPolicy.SupportedSchemaVersion,
                    LayoutKey = "sample-layout",
                    Tier = LayoutTier.Premium,
                    ClassifiedBy = "legacy-migration",
                    ClassifiedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
                    Reason = "Untrusted author-controlled compatibility seed.",
                    Revision = 1,
                },
                LayoutPackageJson.CreateOptions(indented: true)));

        var initialReload = await harness.Registry.ReloadAsync();

        Assert.True(initialReload.Succeeded);
        Assert.Empty(harness.Registry.DefinitionPolicies);

        var superAdminToken = harness.Tokens.CreateToken();
        var explicitClassification =
            await harness.Service.SetDefinitionTierAsync(
                "sample-layout",
                LayoutTier.Premium,
                superAdminToken,
                "Test SuperAdmin",
                "Explicitly classify the legacy LayoutKey");

        Assert.False(explicitClassification.Reclassified);
        Assert.Equal("Test SuperAdmin", explicitClassification.Policy.ClassifiedBy);
        Assert.Equal(2, explicitClassification.Policy.Revision);
        Assert.Equal(
            LayoutTier.Premium,
            harness.Registry.DefinitionPolicies["sample-layout"].Tier);
    }

    [Fact]
    public async Task Server_free_policy_overrides_premium_manifest_across_versions()
    {
        await using var harness = await WorkflowHarness.CreateAsync();
        var superAdminToken = harness.Tokens.CreateToken();
        var v1 = await SubmitAsync(
            harness.Service,
            harness.Actor,
            CreatePackage("1.0.0", "#AA7755", LayoutTier.Premium));
        await harness.Service.ApproveAsync(
            v1.SubmissionId,
            superAdminToken,
            "Test SuperAdmin");
        var v11 = await SubmitAsync(
            harness.Service,
            harness.Actor,
            CreatePackage("1.1.0", "#446688", LayoutTier.Premium));
        await harness.Service.ApproveAsync(
            v11.SubmissionId,
            superAdminToken,
            "Test SuperAdmin");

        var descriptor = harness.Registry.Current.FindDescriptor("sample-layout");
        Assert.NotNull(descriptor);
        Assert.Equal(Wedding.Common.WeddingLayoutTier.Free, descriptor.Tier);
        Assert.Equal("1.1.0", descriptor.CurrentVersion);
        Assert.All(
            harness.Registry.PublishedPackages.Values,
            package => Assert.Equal(LayoutTier.Premium, package.Manifest.Tier));
    }

    [Fact]
    public async Task Ordinary_upload_and_approval_do_not_change_definition_policy()
    {
        await using var harness = await WorkflowHarness.CreateAsync();
        var superAdminToken = harness.Tokens.CreateToken();
        var before = (await harness.Service.ListDefinitionPoliciesAsync(
                superAdminToken))
            .Single(policy => string.Equals(
                policy.LayoutKey,
                "sample-layout",
                StringComparison.OrdinalIgnoreCase));
        var submission = await SubmitAsync(
            harness.Service,
            harness.Actor,
            CreatePackage("1.0.0", "#AA7755", LayoutTier.Premium));
        await harness.Service.ApproveAsync(
            submission.SubmissionId,
            superAdminToken,
            "Test SuperAdmin");
        var after = (await harness.Service.ListDefinitionPoliciesAsync(
                superAdminToken))
            .Single(policy => string.Equals(
                policy.LayoutKey,
                "sample-layout",
                StringComparison.OrdinalIgnoreCase));

        Assert.Equal(LayoutTier.Free, before.Tier);
        Assert.Equal(before.Tier, after.Tier);
        Assert.Equal(before.Revision, after.Revision);
    }

    [Fact]
    public async Task Super_admin_reclassification_is_the_only_operation_that_changes_effective_tier()
    {
        await using var harness = await WorkflowHarness.CreateAsync();
        var superAdminToken = harness.Tokens.CreateToken();
        var submission = await SubmitAsync(
            harness.Service,
            harness.Actor,
            CreatePackage("1.0.0", "#AA7755", LayoutTier.Free));
        await harness.Service.ApproveAsync(
            submission.SubmissionId,
            superAdminToken,
            "Test SuperAdmin");
        var before = (await harness.Service.ListDefinitionPoliciesAsync(
                superAdminToken))
            .Single(policy => string.Equals(
                policy.LayoutKey,
                "sample-layout",
                StringComparison.OrdinalIgnoreCase));

        var change = await harness.Service.SetDefinitionTierAsync(
            "sample-layout",
            LayoutTier.Premium,
            superAdminToken,
            "Test SuperAdmin",
            "Reclassify verified definition");

        Assert.True(change.Reclassified);
        Assert.True(change.Reload.Succeeded);
        Assert.Equal(LayoutTier.Premium, change.Policy.Tier);
        Assert.Equal(before.Revision + 1, change.Policy.Revision);
        Assert.Equal(
            Wedding.Common.WeddingLayoutTier.Premium,
            harness.Registry.Current.FindDescriptor("sample-layout")?.Tier);
    }

    [Fact]
    public async Task Archive_preserves_definition_policy_for_later_releases()
    {
        await using var harness = await WorkflowHarness.CreateAsync();
        var superAdminToken = harness.Tokens.CreateToken();
        var approved = await SubmitAsync(
            harness.Service,
            harness.Actor,
            CreatePackage("1.0.0", "#AA7755", LayoutTier.Premium));
        await harness.Service.ApproveAsync(
            approved.SubmissionId,
            superAdminToken,
            "Test SuperAdmin");
        var policyBefore = (await harness.Service.ListDefinitionPoliciesAsync(
                superAdminToken))
            .Single(policy => string.Equals(
                policy.LayoutKey,
                "sample-layout",
                StringComparison.OrdinalIgnoreCase));
        var archive = await harness.Service.ArchiveAsync(
            approved.SubmissionId,
            superAdminToken,
            "Test SuperAdmin",
            "Archive release without deleting its classification");

        var policyAfterArchive =
            (await harness.Service.ListDefinitionPoliciesAsync(superAdminToken))
            .Single(policy => string.Equals(
                policy.LayoutKey,
                "sample-layout",
                StringComparison.OrdinalIgnoreCase));
        Assert.True(archive.PublishedReleaseArchived);
        Assert.True(archive.ActivePointerRemoved);
        Assert.Equal(policyBefore.Tier, policyAfterArchive.Tier);
        Assert.Equal(policyBefore.Revision, policyAfterArchive.Revision);

        var later = await SubmitAsync(
            harness.Service,
            harness.Actor,
            CreatePackage("1.0.1", "#446688", LayoutTier.Premium));
        await harness.Service.ApproveAsync(
            later.SubmissionId,
            superAdminToken,
            "Test SuperAdmin");

        Assert.Equal(
            Wedding.Common.WeddingLayoutTier.Free,
            harness.Registry.Current.FindDescriptor("sample-layout")?.Tier);
        Assert.Equal(
            LayoutTier.Free,
            (await harness.Service.ListDefinitionPoliciesAsync(superAdminToken))
            .Single(policy => string.Equals(
                policy.LayoutKey,
                "sample-layout",
                StringComparison.OrdinalIgnoreCase))
            .Tier);
    }

    [Fact]
    public async Task Built_in_definition_tier_is_virtual_and_cannot_be_reclassified()
    {
        await using var harness = await WorkflowHarness.CreateAsync();
        var superAdminToken = harness.Tokens.CreateToken();
        var before = harness.Registry.Current.FindDescriptor(
            Wedding.Common.WeddingLayoutKeys.OnePage)
            ?? throw new InvalidOperationException(
                "The built-in one-page layout is missing.");
        var policy = (await harness.Service.ListDefinitionPoliciesAsync(
                superAdminToken))
            .Single(candidate => string.Equals(
                candidate.LayoutKey,
                Wedding.Common.WeddingLayoutKeys.OnePage,
                StringComparison.OrdinalIgnoreCase));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Service.SetDefinitionTierAsync(
                Wedding.Common.WeddingLayoutKeys.OnePage,
                LayoutTier.Premium,
                superAdminToken,
                "Test SuperAdmin",
                "Built-in tiers must remain application-owned"));

        var after = harness.Registry.Current.FindDescriptor(
            Wedding.Common.WeddingLayoutKeys.OnePage);
        Assert.NotNull(after);
        Assert.Equal(0, policy.Revision);
        Assert.Equal(LayoutTier.Free, policy.Tier);
        Assert.Equal(before.Tier, after.Tier);
    }

    private static async Task<WeddingLayoutSubmissionRecord> SubmitAsync(
        IWeddingLayoutSubmissionService service,
        WeddingCurrentUser actor,
        LayoutPackage package)
    {
        await using var stream = PackageStream(package);
        return await service.SubmitAsync("sample-tenant", actor, stream);
    }

    private static MemoryStream PackageStream(LayoutPackage package) =>
        new(JsonSerializer.SerializeToUtf8Bytes(
            package,
            LayoutPackageJson.CreateOptions(indented: true)));

    private static LayoutPackage CreatePackage(
        string version,
        string primaryColor,
        LayoutTier tier = LayoutTier.Free) =>
        new()
        {
            Manifest = new LayoutManifest
            {
                SchemaVersion = LayoutSchema.CurrentVersion,
                Key = "sample-layout",
                Version = version,
                Label = "Sample layout",
                Description = "A safe integration-test layout.",
                Tier = tier,
            },
            Definition = new LayoutDefinition
            {
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
                ],
                Root = new LayoutBlock
                {
                    Id = "page",
                    Kind = LayoutBlockKind.Page,
                    Binding = LayoutBindingKey.Invitation,
                    Children =
                    [
                        new LayoutBlock
                        {
                            Id = "hero",
                            Kind = LayoutBlockKind.Hero,
                            Binding = LayoutBindingKey.Invitation,
                            Children =
                            [
                                new LayoutBlock
                                {
                                    Id = "couple-name",
                                    Kind = LayoutBlockKind.Heading,
                                    Binding = LayoutBindingKey.CoupleName,
                                },
                            ],
                        },
                        new LayoutBlock
                        {
                            Id = "invitation-section",
                            Kind = LayoutBlockKind.Section,
                            Binding = LayoutBindingKey.Invitation,
                            Children =
                            [
                                new LayoutBlock
                                {
                                    Id = "invitation-copy",
                                    Kind = LayoutBlockKind.Text,
                                    Binding = LayoutBindingKey.Subtitle,
                                },
                            ],
                        },
                    ],
                },
            },
        };

    private static string? ReadActivePointerVersion(string testRoot)
    {
        var path = ActivePointerPath(testRoot, "sample-layout");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.GetProperty("version").GetString();
    }

    private static string ActivePointerPath(string testRoot, string layoutKey) =>
        Path.Combine(
            testRoot,
            "App_Data",
            "LayoutPackages",
            "Active",
            layoutKey + ".json");

    private static string ReleaseDirectory(
        string testRoot,
        string layoutKey,
        string version) =>
        Path.Combine(
            testRoot,
            "App_Data",
            "LayoutPackages",
            "Releases",
            layoutKey,
            version);

    private static string SubmissionDirectory(string testRoot, string submissionId) =>
        Path.Combine(
            testRoot,
            "App_Data",
            "LayoutPackages",
            "Submissions",
            submissionId);

    private static string ArchivedSubmissionDirectory(
        string testRoot,
        string submissionId) =>
        Path.Combine(
            testRoot,
            "App_Data",
            "LayoutPackages",
            "Archive",
            "Submissions",
            submissionId);

    private static string ArchivedReleaseDirectory(
        string testRoot,
        string layoutKey,
        string version) =>
        Path.Combine(
            testRoot,
            "App_Data",
            "LayoutPackages",
            "Archive",
            "Releases",
            layoutKey,
            version);

    private static string PurgedArchiveDirectory(
        string testRoot,
        string submissionId) =>
        Path.Combine(
            testRoot,
            "App_Data",
            "LayoutPackages",
            "Archive",
            "Purged",
            submissionId);

    private static string DefinitionPolicyPath(
        string testRoot,
        string layoutKey) =>
        Path.Combine(
            testRoot,
            "App_Data",
            "LayoutPackages",
            "Policies",
            layoutKey + ".json");

    private static void CopyDirectory(string sourceDirectory, string destinationDirectory)
    {
        Directory.CreateDirectory(destinationDirectory);
        foreach (var sourceFile in Directory.EnumerateFiles(sourceDirectory))
        {
            File.Copy(
                sourceFile,
                Path.Combine(
                    destinationDirectory,
                    Path.GetFileName(sourceFile)));
        }

        foreach (var sourceChild in Directory.EnumerateDirectories(sourceDirectory))
        {
            CopyDirectory(
                sourceChild,
                Path.Combine(
                    destinationDirectory,
                    Path.GetFileName(sourceChild)));
        }
    }

    private sealed class WorkflowHarness : IAsyncDisposable
    {
        private WorkflowHarness(
            string testRoot,
            WeddingOptions options,
            JsonTenantStore tenants,
            SuperAdminSessionTokenService tokens,
            CapturingAuditLog audit,
            FileSystemWeddingLayoutCatalogRegistry registry,
            FileSystemWeddingLayoutSubmissionService service,
            WeddingCurrentUser actor)
        {
            TestRoot = testRoot;
            Options = options;
            Tenants = tenants;
            Tokens = tokens;
            Audit = audit;
            Registry = registry;
            Service = service;
            Actor = actor;
        }

        public string TestRoot { get; }

        public WeddingOptions Options { get; }

        public JsonTenantStore Tenants { get; }

        public SuperAdminSessionTokenService Tokens { get; }

        public CapturingAuditLog Audit { get; }

        public FileSystemWeddingLayoutCatalogRegistry Registry { get; }

        public FileSystemWeddingLayoutSubmissionService Service { get; }

        public WeddingCurrentUser Actor { get; }

        public static async Task<WorkflowHarness> CreateAsync(
            bool assignDefaultPolicy = true)
        {
            var testRoot = Path.Combine(
                Path.GetTempPath(),
                "WeddingLayoutWorkflowTests",
                Guid.NewGuid().ToString("N"));
            var publicDataRoot = Path.Combine(testRoot, "App_Data", "Wedding");
            Directory.CreateDirectory(publicDataRoot);
            var options = new WeddingOptions
            {
                DataPath = publicDataRoot,
                SuperAdminPassword = "layout-workflow-test-secret",
            };
            var tenants = new JsonTenantStore(options);
            await tenants.SaveAsync(new TenantConfig
            {
                Slug = "sample-tenant",
                OwnerUserId = "oauth-owner",
            });

            var tokens = new SuperAdminSessionTokenService(options);
            var audit = new CapturingAuditLog();
            var registry = new FileSystemWeddingLayoutCatalogRegistry(
                options,
                NullLogger<FileSystemWeddingLayoutCatalogRegistry>.Instance);
            var service = new FileSystemWeddingLayoutSubmissionService(
                options,
                tenants,
                tokens,
                audit,
                registry);
            if (assignDefaultPolicy)
            {
                await service.SetDefinitionTierAsync(
                    "sample-layout",
                    LayoutTier.Free,
                    tokens.CreateToken(),
                    "Test SuperAdmin",
                    "Classify integration-test layout");
            }

            return new WorkflowHarness(
                testRoot,
                options,
                tenants,
                tokens,
                audit,
                registry,
                service,
                new WeddingCurrentUser(
                    "oauth-owner",
                    "Test",
                    "owner@example.test",
                    "Owner"));
        }

        public async ValueTask DisposeAsync()
        {
            Service.Dispose();
            await Registry.StopAsync(CancellationToken.None);
            Registry.Dispose();
            if (Directory.Exists(TestRoot))
            {
                Directory.Delete(TestRoot, recursive: true);
            }
        }
    }

    private sealed class ScriptedRegistry(
        IWeddingLayoutCatalogRegistry inner,
        Func<int, CancellationToken, Task<WeddingLayoutReloadResult>> reload) :
        IWeddingLayoutCatalogRegistry
    {
        private int _reloadCount;

        public Wedding.Common.WeddingLayoutCatalog Current => inner.Current;

        public IReadOnlyDictionary<Wedding.Common.WeddingLayoutReleaseId, WeddingLayoutPublishedPackage>
            PublishedPackages => inner.PublishedPackages;

        public IReadOnlyDictionary<string, LayoutDefinitionPolicy> DefinitionPolicies =>
            inner.DefinitionPolicies;

        public string LayoutPackagesRoot => inner.LayoutPackagesRoot;

        public int ReloadCount => Volatile.Read(ref _reloadCount);

        public event EventHandler<WeddingLayoutCatalogChangedEventArgs>? Changed
        {
            add => inner.Changed += value;
            remove => inner.Changed -= value;
        }

        public Task<WeddingLayoutReloadResult> ReloadAsync(
            CancellationToken cancellationToken = default) =>
            reload(Interlocked.Increment(ref _reloadCount), cancellationToken);
    }

    private sealed class CapturingAuditLog : ISuperAdminAuditLog
    {
        public List<(string Action, string Slug, string Detail)> Entries { get; } = [];

        public Task WriteAsync(
            string action,
            string slug,
            string? detail = null,
            CancellationToken ct = default)
        {
            Entries.Add((action, slug, detail ?? ""));
            return Task.CompletedTask;
        }
    }
}
