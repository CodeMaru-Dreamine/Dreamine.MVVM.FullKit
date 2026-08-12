using System.IO;
using System.Text;
using Dreamine.Secs.Abstractions.Enums;
using Dreamine.Secs.Abstractions.Hsms;
using Dreamine.Secs.Abstractions.Interfaces;
using Dreamine.Secs.Abstractions.Model;
using Dreamine.SecsGem.Interop.Runtime.Persistence;
using Dreamine.SecsGem.Interop.Runtime.Profiles;
using Dreamine.SecsGem.Interop.Runtime.Templates;
using Xunit;

namespace Dreamine.SecsGem.Interop.Wpf.Tests;

public sealed class ProfileTemplatePersistenceTests
{
    [Fact]
    public async Task ConnectionProfileV1RoundTripsWithoutCredentialsAndMapsEveryRuntimeLimit()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "connection.json");
        var profile = ValidProfile() with
        {
            Role = SecsRole.Equipment,
            Mode = SecsConnectionMode.Passive,
            Host = "::1",
            Port = 7100,
            SessionId = 23,
            AutoReconnect = false,
            Timers = new ConnectionTimerProfileV1(46, 11, 6, 12, 7),
            ReconnectPolicy = new OperationalReconnectPolicyV1(2, 60, 2),
            SafetyLimits = new ConnectionSafetyLimitsV1(1_048_576, 1_000_000, 24, 4096),
            LogPolicyId = ConnectionLogPolicyIds.HeaderOnlyV1
        };
        var store = ConnectionProfileStore.Create();

        await store.SaveAsync(path, profile);
        var loaded = await store.LoadAsync(path);
        var json = await File.ReadAllTextAsync(path);
        var options = loaded.ToHsmsSessionOptions();

        Assert.Equal(profile, loaded);
        Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("credential", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", json, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(SecsRole.Equipment, options.Role);
        Assert.Equal(SecsConnectionMode.Passive, options.Mode);
        Assert.Equal((ushort)23, options.SessionId.Value);
        Assert.Equal(1_048_576, options.MaximumFrameLength);
        Assert.Equal(1_000_000, options.MaximumMessageLength);
        Assert.Equal(24, options.MaximumNestingDepth);
        Assert.Equal(4096, options.MaximumListItemCount);
        Assert.Equal(TimeSpan.FromSeconds(46), options.Timers.T3);
    }

    [Fact]
    public void ConnectionProfileValidationAndApplyDiffAreExplicitAndFailSafe()
    {
        var current = ValidProfile();
        var logOnly = current with { LogPolicyId = ConnectionLogPolicyIds.ExcludedV1 };
        var endpoint = logOnly with { Port = 7101 };

        var immediate = ConnectionProfileApplyDiff.Compare(current, logOnly);
        var recreate = ConnectionProfileApplyDiff.Compare(current, endpoint);

        Assert.Equal(ConnectionProfileApplyDisposition.ImmediateOnly, immediate.Disposition);
        Assert.Equal([nameof(SingleConnectionProfileV1.LogPolicyId)], immediate.ImmediateChanges);
        Assert.False(immediate.RequiresSessionRecreation);
        Assert.Equal(ConnectionProfileApplyDisposition.RecreateRequired, recreate.Disposition);
        Assert.Contains(nameof(SingleConnectionProfileV1.Port), recreate.RecreateRequiredChanges);
        Assert.True(recreate.RequiresSessionRecreation);

        Assert.Throws<ConnectionProfileValidationException>(() =>
            (current with { Role = SecsRole.Unspecified }).Validate());
        Assert.Throws<ConnectionProfileValidationException>(() =>
            (current with { Port = 0 }).Validate());
        Assert.Throws<ConnectionProfileValidationException>(() =>
            (current with { SessionId = 32768 }).Validate());
        Assert.Throws<ConnectionProfileValidationException>(() =>
            (current with { Host = "user:secret@host" }).Validate());
        Assert.Throws<ConnectionProfileValidationException>(() =>
            (current with { LogPolicyId = "missing-policy" }).Validate());
        Assert.Throws<ConnectionProfileValidationException>(() =>
            (current with { Mode = SecsConnectionMode.Passive, AutoReconnect = true }).Validate());
    }

    [Fact]
    public async Task VersionedStoreRejectsNewerUnknownOversizedAndCredentialBearingDocuments()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "connection.json");
        var store = ConnectionProfileStore.Create(new JsonPersistenceLimits(512, 16, 128));

        await File.WriteAllTextAsync(path,
            "{\"schema\":\"dreamine.secs.connection-profile\",\"version\":2}");
        var newer = await Assert.ThrowsAsync<JsonSchemaVersionException>(() => store.LoadAsync(path));
        Assert.Equal(2, newer.ActualVersion);

        await File.WriteAllTextAsync(path,
            "{\"schema\":\"another-schema\",\"version\":1}");
        await Assert.ThrowsAsync<JsonSchemaVersionException>(() => store.LoadAsync(path));

        await File.WriteAllTextAsync(path, new string('x', 513));
        await Assert.ThrowsAsync<JsonInputLimitException>(() => store.LoadAsync(path));

        await File.WriteAllTextAsync(path,
            "{\"schema\":\"dreamine.secs.connection-profile\",\"version\":1,\"password\":\"secret\"}");
        await Assert.ThrowsAsync<JsonPersistenceException>(() => store.LoadAsync(path));
    }

    [Fact]
    public async Task AtomicValidationFailureAndCancellationPreserveTheLastGoodProfile()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "connection.json");
        var store = ConnectionProfileStore.Create();
        var original = ValidProfile();
        await store.SaveAsync(path, original);
        var before = await File.ReadAllBytesAsync(path);

        await Assert.ThrowsAsync<ConnectionProfileValidationException>(() =>
            store.SaveAsync(path, original with { Port = -1 }));
        Assert.Equal(before, await File.ReadAllBytesAsync(path));

        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            store.SaveAsync(path, original with { Port = 7102 }, cancelled.Token));
        Assert.Equal(before, await File.ReadAllBytesAsync(path));
        Assert.Empty(Directory.EnumerateFiles(directory.Path, "*.tmp"));
    }

    [Fact]
    public async Task TemplateCatalogV1RoundTripsEveryCodecFormatIncludingJis8AndEmptyBodyDistinction()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "templates.json");
        var allFormats = new SecsItemTemplateNode(SecsItemFormat.List);
        allFormats.AddChild(new(SecsItemFormat.Binary, ["0", "0xFF"]));
        allFormats.AddChild(new(SecsItemFormat.Boolean, ["true", "false"]));
        allFormats.AddChild(new(SecsItemFormat.Ascii, ["ASCII"]));
        allFormats.AddChild(new(SecsItemFormat.Jis8, ["0x1B", "0x24", "0x42"]));
        allFormats.AddChild(new(SecsItemFormat.Int8, ["-128", "127"]));
        allFormats.AddChild(new(SecsItemFormat.Int16, ["-32768", "32767"]));
        allFormats.AddChild(new(SecsItemFormat.Int32, ["-2147483648", "2147483647"]));
        allFormats.AddChild(new(SecsItemFormat.Int64, ["-9223372036854775808", "9223372036854775807"]));
        allFormats.AddChild(new(SecsItemFormat.UInt8, ["0", "255"]));
        allFormats.AddChild(new(SecsItemFormat.UInt16, ["0", "65535"]));
        allFormats.AddChild(new(SecsItemFormat.UInt32, ["0", "4294967295"]));
        allFormats.AddChild(new(SecsItemFormat.UInt64, ["0", "18446744073709551615"]));
        allFormats.AddChild(new(SecsItemFormat.Float32, ["1.25", "-3.5"]));
        allFormats.AddChild(new(SecsItemFormat.Float64, ["1.25", "-3.5"]));
        var catalog = new MessageTemplateCatalogV1
        {
            Templates =
            [
                Template("No body", 1, 1, true, null),
                Template("Empty list", 1, 3, true, new SecsItemTemplateNode(SecsItemFormat.List)),
                Template("All formats", 3, 5, false, allFormats)
            ]
        };
        var store = MessageTemplateCatalogStore.Create();

        await store.SaveAsync(path, catalog);
        var loaded = await store.LoadAsync(path);

        Assert.Equal(3, loaded.Templates.Count);
        Assert.Null(loaded.Templates[0].Root);
        Assert.Empty(Assert.IsType<SecsListItem>(loaded.Templates[1].BuildItem()).Items);
        var list = Assert.IsType<SecsListItem>(loaded.Templates[2].BuildItem());
        Assert.Equal(Enum.GetValues<SecsItemFormat>().Length - 1, list.Items.Count);
        Assert.Equal(new byte[] { 0x1B, 0x24, 0x42 }, Assert.IsType<SecsJis8Item>(list.Items[3]).Values.ToArray());
        loaded.ValidateForSend();
    }

    [Fact]
    public void TemplateEditingSupportsAddRemoveReorderDeepCloneAndReceivedMessageCopy()
    {
        var root = new SecsItemTemplateNode(SecsItemFormat.List);
        var first = new SecsItemTemplateNode(SecsItemFormat.Ascii, ["first"]);
        var second = new SecsItemTemplateNode(SecsItemFormat.UInt16, ["2"]);
        root.AddChild(first);
        root.AddChild(second);

        Assert.True(root.MoveChildUp(1));
        Assert.Same(second, root.Children[0]);
        Assert.True(root.MoveChildDown(0));
        Assert.Same(second, root.Children[1]);
        var clone = root.CloneDeep();
        clone.Children[0].Values[0] = "changed";
        Assert.Equal("first", root.Children[0].Values[0]);
        Assert.True(root.RemoveChild(second));
        Assert.Single(root.Children);

        var message = new SecsMessage(new(27), new(6), new(11), true, new(0x12345678),
            new SecsListItem(new SecsJis8Item(0x41, 0x42), new SecsAsciiItem("COPY")));
        var copied = MessageTemplateV1.FromReceivedMessage(
            "Copied", MessageTemplateDirection.EquipmentToHost, message);
        var rebuilt = copied.BuildMessage(new SecsSessionId(27), new SecsSystemBytes(99));

        Assert.Equal((ushort)27, rebuilt.SessionId.Value);
        Assert.Equal((uint)99, rebuilt.SystemBytes.Value);
        Assert.Equal(MessageTemplateKind.Primary, copied.Kind);
        Assert.True(copied.WaitBit);
        var rebuiltRoot = Assert.IsType<SecsListItem>(rebuilt.Item);
        Assert.Equal(new byte[] { 0x41, 0x42 }, Assert.IsType<SecsJis8Item>(rebuiltRoot.Items[0]).Values.ToArray());
    }

    [Fact]
    public void MultipleLegacyRootsRequireAnExplicitDecision()
    {
        SecsItemTemplateNode[] roots =
        [
            new(SecsItemFormat.Ascii, ["A"]),
            new(SecsItemFormat.UInt8, ["1"])
        ];

        Assert.Throws<TemplateValidationException>(() =>
            SecsItemTemplateNode.ImportLegacyRoots(roots, MultipleRootHandling.Reject));
        var wrapped = Assert.IsType<SecsItemTemplateNode>(
            SecsItemTemplateNode.ImportLegacyRoots(roots, MultipleRootHandling.WrapInList));

        Assert.Equal(SecsItemFormat.List, wrapped.Format);
        Assert.Equal(2, wrapped.Children.Count);
        roots[0].Values[0] = "mutated";
        Assert.Equal("A", wrapped.Children[0].Values[0]);
        Assert.Null(SecsItemTemplateNode.ImportLegacyRoots([], MultipleRootHandling.Reject));
    }

    [Fact]
    public void TemplateValidationRejectsParityWaitBitShapeOverflowAndNodeLimitsWithConcretePaths()
    {
        var secondaryWithWait = Template("bad", 1, 2, true, null) with { Kind = MessageTemplateKind.Secondary };
        var primaryWithEvenFunction = Template("bad", 1, 2, false, null);
        var overflow = Template("bad", 1, 1, false,
            new SecsItemTemplateNode(SecsItemFormat.UInt8, ["256"]));
        var atomicWithChild = new SecsItemTemplateNode(SecsItemFormat.Ascii, ["x"]);
        atomicWithChild.Children.Add(new SecsItemTemplateNode(SecsItemFormat.UInt8, ["1"]));
        var sensitive = new SecsItemTemplateNode(SecsItemFormat.Ascii, ["secret"]) { IsSensitive = true };
        var unsafeLogging = Template("sensitive", 1, 1, false, sensitive) with
        {
            BodyLogPolicy = TemplateBodyLogPolicy.FullBodyExplicit
        };

        Assert.Throws<TemplateValidationException>(() => secondaryWithWait.Validate());
        Assert.Throws<TemplateValidationException>(() => primaryWithEvenFunction.Validate());
        var overflowError = Assert.Throws<TemplateValidationException>(() => overflow.Validate());
        Assert.Contains("root.values[0]", overflowError.Message, StringComparison.Ordinal);
        Assert.Throws<TemplateValidationException>(() =>
            (Template("bad", 1, 1, false, atomicWithChild)).Validate());
        Assert.Throws<TemplateValidationException>(() => unsafeLogging.Validate());

        var root = new SecsItemTemplateNode(SecsItemFormat.List);
        root.AddChild(new SecsItemTemplateNode(SecsItemFormat.UInt8, ["1"]));
        root.AddChild(new SecsItemTemplateNode(SecsItemFormat.UInt8, ["2"]));
        var limited = Template("limited", 1, 1, false, root);
        Assert.Throws<TemplateValidationException>(() => limited.Validate(new MessageTemplateLimits(1, 8, 1024, 8)));

        var cyclic = new SecsItemTemplateNode(SecsItemFormat.List);
        cyclic.Children.Add(cyclic);
        Assert.Throws<TemplateValidationException>(() => cyclic.CloneDeep());
    }

    [Fact]
    public async Task CatalogStoreRejectsJsonDepthAndNodeCountBeforeMaterializingTemplates()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "templates.json");
        var depthStore = MessageTemplateCatalogStore.Create(
            persistenceLimits: new JsonPersistenceLimits(4096, 4, 100));
        await File.WriteAllTextAsync(path,
            "{\"schema\":\"dreamine.secs.message-template-catalog\",\"version\":1,\"templates\":[[[[[1]]]]]}");
        await Assert.ThrowsAsync<JsonInputLimitException>(() => depthStore.LoadAsync(path));

        var nodeStore = MessageTemplateCatalogStore.Create(
            persistenceLimits: new JsonPersistenceLimits(4096, 32, 8));
        await File.WriteAllTextAsync(path,
            "{\"schema\":\"dreamine.secs.message-template-catalog\",\"version\":1,\"templates\":[1,2,3,4,5,6,7,8,9]}");
        await Assert.ThrowsAsync<JsonInputLimitException>(() => nodeStore.LoadAsync(path));
    }

    [Fact]
    public async Task PendingPrimaryReplyCapturesSourceMetadataAndDelegatesOneValidatedReplyOnly()
    {
        var primary = new SecsMessage(new(27), new(6), new(11), true, new(0x12345678),
            new SecsListItem(new SecsAsciiItem("SOURCE")));
        var identity = new SecsConnectionIdentity(
            "provider", Guid.NewGuid(), 4, new SecsSessionId(27), SecsRole.Host, SecsConnectionMode.Passive);
        var context = new FakePrimaryContext(identity, primary);

        var pending = PendingPrimaryReply.Capture(context, "Inbound S6F11");
        var reply = pending.CreateSecondaryDraft("Reply S6F12") with
        {
            Root = new SecsItemTemplateNode(SecsItemFormat.Binary, ["0"])
        };

        Assert.Same(identity, pending.SourceIdentity);
        Assert.Equal((ushort)27, pending.SessionId.Value);
        Assert.Equal((byte)6, pending.Stream.Value);
        Assert.Equal((byte)11, pending.PrimaryFunction.Value);
        Assert.Equal((byte)12, pending.SecondaryFunction.Value);
        Assert.Equal(0x12345678u, pending.SystemBytes.Value);
        Assert.Equal(MessageTemplateDirection.EquipmentToHost, pending.InboundPrimary.Direction);
        Assert.Equal("SOURCE", Assert.IsType<SecsAsciiItem>(
            Assert.IsType<SecsListItem>(pending.InboundPrimary.BuildItem()).Items[0]).Value);

        await pending.ReplyAsync(reply);

        Assert.True(pending.ReplyAttempted);
        Assert.Equal(1, context.ReplyCount);
        Assert.Equal(new byte[] { 0 }, Assert.IsType<SecsBinaryItem>(context.LastReplyItem).Values.ToArray());
        await Assert.ThrowsAsync<InvalidOperationException>(() => pending.ReplyAsync(reply).AsTask());
        Assert.Equal(1, context.ReplyCount);
    }

    [Fact]
    public async Task PendingPrimaryReplyRejectsWrongDialogueBeforeOwnershipAndPropagatesStaleFailure()
    {
        var primary = new SecsMessage(new(2), new(1), new(13), true, new(81));
        var identity = new SecsConnectionIdentity(
            "provider", Guid.NewGuid(), 9, new SecsSessionId(2), SecsRole.Equipment, SecsConnectionMode.Passive);
        var context = new FakePrimaryContext(identity, primary);
        var pending = PendingPrimaryReply.Capture(context);
        var wrong = pending.CreateSecondaryDraft("Wrong") with { Function = 16 };

        await Assert.ThrowsAsync<TemplateValidationException>(() => pending.ReplyAsync(wrong).AsTask());
        Assert.False(pending.ReplyAttempted);
        Assert.Equal(0, context.ReplyCount);

        context.ReplyFailure = new InvalidOperationException("The captured context is stale after reconnect.");
        var valid = pending.CreateSecondaryDraft("Valid");
        var stale = await Assert.ThrowsAsync<InvalidOperationException>(() => pending.ReplyAsync(valid).AsTask());

        Assert.Contains("stale", stale.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(pending.ReplyAttempted);
        Assert.Equal(1, context.ReplyCount);
        await Assert.ThrowsAsync<InvalidOperationException>(() => pending.ReplyAsync(valid).AsTask());
        Assert.Equal(1, context.ReplyCount);
    }

    [Fact]
    public void TemplateBodyLogPolicyMapsToPreCopyWireCaptureRulesByLocalRole()
    {
        var header = Template("Header", 1, 1, true, null);
        var excluded = Template("Excluded", 5, 1, true, null) with
        {
            Direction = MessageTemplateDirection.EquipmentToHost,
            BodyLogPolicy = TemplateBodyLogPolicy.Excluded
        };
        var full = Template("Full", 6, 11, true, new SecsItemTemplateNode(SecsItemFormat.Ascii, ["PUBLIC"])) with
        {
            Direction = MessageTemplateDirection.EquipmentToHost,
            BodyLogPolicy = TemplateBodyLogPolicy.FullBodyExplicit
        };

        var options = MessageTemplateWireCaptureAdapter.CreateObservationOptions(
            SecsRole.Host, [header, excluded, full], queueCapacity: 32, maximumFullBodyBytes: 1024);

        Assert.Equal(HsmsWireCaptureMode.HeaderOnly, options.DefaultCaptureMode);
        Assert.Equal(1038, options.MaximumCapturedBytes);
        Assert.Contains(options.CaptureRules, rule => rule.Stream == 1 && rule.Function == 1 &&
            rule.Direction == HsmsWireDirection.Outbound && rule.Mode == HsmsWireCaptureMode.HeaderOnly);
        Assert.Contains(options.CaptureRules, rule => rule.Stream == 5 && rule.Function == 1 &&
            rule.Direction == HsmsWireDirection.Inbound && rule.Mode == HsmsWireCaptureMode.Excluded);
        Assert.Contains(options.CaptureRules, rule => rule.Stream == 6 && rule.Function == 11 &&
            rule.Direction == HsmsWireDirection.Inbound && rule.Mode == HsmsWireCaptureMode.FullFrame &&
            rule.MaximumCapturedBytes == 1038);

        full.Root!.IsSensitive = true;
        Assert.Throws<TemplateValidationException>(() => MessageTemplateWireCaptureAdapter.CreateObservationOptions(
            SecsRole.Host, [full], queueCapacity: 1, maximumFullBodyBytes: 1024));
    }

    private static SingleConnectionProfileV1 ValidProfile() => new()
    {
        Role = SecsRole.Host,
        Mode = SecsConnectionMode.Active,
        Host = "127.0.0.1",
        Port = 7000,
        SessionId = 7,
        AutoReconnect = true,
        Timers = new ConnectionTimerProfileV1(45, 10, 5, 10, 5),
        ReconnectPolicy = new OperationalReconnectPolicyV1(1, 30, 2),
        SafetyLimits = new ConnectionSafetyLimitsV1(16 * 1024 * 1024, 16 * 1024 * 1024 - 10, 64, 65_535),
        LogPolicyId = ConnectionLogPolicyIds.HeaderOnlyV1
    };

    private static MessageTemplateV1 Template(string name, byte stream, byte function, bool waitBit,
        SecsItemTemplateNode? root) => new()
    {
        Name = name,
        Description = name,
        Stream = stream,
        Function = function,
        WaitBit = waitBit,
        Kind = MessageTemplateKind.Primary,
        Direction = MessageTemplateDirection.HostToEquipment,
        BodyLogPolicy = TemplateBodyLogPolicy.HeaderOnly,
        Root = root
    };

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"dreamine-profile-template-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }

    private sealed class FakePrimaryContext(
        SecsConnectionIdentity connectionIdentity,
        SecsMessage primary) : ISecsPrimaryContext
    {
        public SecsConnectionIdentity ConnectionIdentity { get; } = connectionIdentity;
        public SecsMessage Primary { get; } = primary;
        public bool CanReply { get; init; } = true;
        public int ReplyCount { get; private set; }
        public SecsItem? LastReplyItem { get; private set; }
        public Exception? ReplyFailure { get; set; }

        public ValueTask ReplyAsync(SecsItem? item = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ReplyCount++;
            LastReplyItem = item;
            return ReplyFailure is null
                ? ValueTask.CompletedTask
                : ValueTask.FromException(ReplyFailure);
        }
    }
}
