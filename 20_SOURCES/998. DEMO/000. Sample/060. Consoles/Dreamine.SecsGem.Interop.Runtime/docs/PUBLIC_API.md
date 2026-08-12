# Public API Inventory

Assembly: `Dreamine.SecsGem.Interop.Runtime`

This inventory is generated from the compiled Release assembly. It is an audit artifact, not an additional compatibility promise.

Exported types: **66**

## Types

### `public sealed class Dreamine.SecsGem.Interop.Runtime.Logging.InteropWireLogRunHealth`

- `Dreamine.SecsGem.Interop.Runtime.Logging.InteropWireLogRunHealth <Clone>$()`
- `InteropWireLogRunHealth(System.Int64 SourceDropped, System.Int64 RecorderDropped, System.Int64 Written, System.Boolean FlushCompleted, System.String Failure)`
- `System.Boolean Equals(Dreamine.SecsGem.Interop.Runtime.Logging.InteropWireLogRunHealth other)`
- `System.Boolean Equals(System.Object obj)`
- `System.Boolean FlushCompleted { get; set; }`
- `System.Boolean IsEvidenceEligible { get; }`
- `System.Int32 GetHashCode()`
- `System.Int64 RecorderDropped { get; set; }`
- `System.Int64 SourceDropped { get; set; }`
- `System.Int64 Written { get; set; }`
- `System.String Failure { get; set; }`
- `System.String ToString()`
- `System.Void Deconstruct(out System.Int64 SourceDropped, out System.Int64 RecorderDropped, out System.Int64 Written, out System.Boolean FlushCompleted, out System.String Failure)`

### `public sealed class Dreamine.SecsGem.Interop.Runtime.Logging.InteropWireLogSession`

- `Dreamine.SecsGem.Interop.Runtime.Logging.InteropWireLogRunHealth Health { get; }`
- `Dreamine.SecsGem.Interop.Runtime.Logging.InteropWireLogSession Start(Dreamine.Secs.Abstractions.Interfaces.ISecsMessageSession session, Dreamine.SecsGem.Interop.Runtime.Logging.InteropWireLogSessionOptions options)`
- `System.Collections.Generic.IReadOnlyList<System.String> FinalizedSegments { get; }`
- `System.Threading.Tasks.Task StopAsync()`
- `System.Threading.Tasks.ValueTask DisposeAsync()`

### `public sealed class Dreamine.SecsGem.Interop.Runtime.Logging.InteropWireLogSessionOptions`

- `Dreamine.Secs.Abstractions.Hsms.HsmsWireObservationOptions CreateObservationOptions()`
- `InteropWireLogSessionOptions(System.String rootDirectory)`
- `System.Int32 ObservationQueueCapacity { get; set; }`
- `System.Int32 RecorderQueueCapacity { get; set; }`
- `System.Int32 RetainedSegments { get; set; }`
- `System.Int64 MaximumSegmentBytes { get; set; }`
- `System.String LogPolicyId { get; set; }`
- `System.String RootDirectory { get; }`
- `System.TimeSpan ShutdownTimeout { get; set; }`

### `public interface Dreamine.SecsGem.Interop.Runtime.Persistence.IVersionedJsonDocument`

- `System.Int32 Version { get; }`
- `System.String Schema { get; }`

### `public sealed class Dreamine.SecsGem.Interop.Runtime.Persistence.JsonInputLimitException`

- `JsonInputLimitException(System.String message)`
- `JsonInputLimitException(System.String message, System.Exception innerException)`

### `public class Dreamine.SecsGem.Interop.Runtime.Persistence.JsonPersistenceException`

- `JsonPersistenceException(System.String message)`
- `JsonPersistenceException(System.String message, System.Exception innerException)`

### `public sealed class Dreamine.SecsGem.Interop.Runtime.Persistence.JsonPersistenceLimits`

- `Dreamine.SecsGem.Interop.Runtime.Persistence.JsonPersistenceLimits <Clone>$()`
- `JsonPersistenceLimits(System.Int32 MaximumFileSizeBytes, System.Int32 MaximumJsonDepth, System.Int32 MaximumNodeCount)`
- `System.Boolean Equals(Dreamine.SecsGem.Interop.Runtime.Persistence.JsonPersistenceLimits other)`
- `System.Boolean Equals(System.Object obj)`
- `System.Int32 GetHashCode()`
- `System.Int32 MaximumFileSizeBytes { get; set; }`
- `System.Int32 MaximumJsonDepth { get; set; }`
- `System.Int32 MaximumNodeCount { get; set; }`
- `System.String ToString()`
- `System.Void Deconstruct(out System.Int32 MaximumFileSizeBytes, out System.Int32 MaximumJsonDepth, out System.Int32 MaximumNodeCount)`
- `System.Void Validate()`

### `public sealed class Dreamine.SecsGem.Interop.Runtime.Persistence.JsonSchemaVersionException`

- `JsonSchemaVersionException(System.String expectedSchema, System.Int32 expectedVersion, System.String actualSchema, System.Nullable<System.Int32> actualVersion)`
- `System.Int32 ExpectedVersion { get; }`
- `System.Nullable<System.Int32> ActualVersion { get; }`
- `System.String ActualSchema { get; }`
- `System.String ExpectedSchema { get; }`

### `public sealed class Dreamine.SecsGem.Interop.Runtime.Persistence.VersionedJsonFileStore<TDocument>`

- `System.Threading.Tasks.Task SaveAsync(System.String path, TDocument document, System.Threading.CancellationToken cancellationToken)`
- `System.Threading.Tasks.Task<TDocument> LoadAsync(System.String path, System.Threading.CancellationToken cancellationToken)`
- `VersionedJsonFileStore(System.String schema, System.Int32 version, System.Action<TDocument> validate, Dreamine.SecsGem.Interop.Runtime.Persistence.JsonPersistenceLimits limits)`

### `public static class Dreamine.SecsGem.Interop.Runtime.Profiles.ConnectionLogPolicyIds`

- `const System.String ExcludedV1 = "excluded-v1"`
- `const System.String FullBodyExplicitV1 = "full-body-explicit-v1"`
- `const System.String HeaderOnlyV1 = "header-only-v1"`

### `public sealed class Dreamine.SecsGem.Interop.Runtime.Profiles.ConnectionProfileApplyDiff`

- `ConnectionProfileApplyDiff(Dreamine.SecsGem.Interop.Runtime.Profiles.ConnectionProfileApplyDisposition Disposition, System.Collections.Generic.IReadOnlyList<System.String> ImmediateChanges, System.Collections.Generic.IReadOnlyList<System.String> RecreateRequiredChanges)`
- `Dreamine.SecsGem.Interop.Runtime.Profiles.ConnectionProfileApplyDiff <Clone>$()`
- `Dreamine.SecsGem.Interop.Runtime.Profiles.ConnectionProfileApplyDiff Compare(Dreamine.SecsGem.Interop.Runtime.Profiles.SingleConnectionProfileV1 current, Dreamine.SecsGem.Interop.Runtime.Profiles.SingleConnectionProfileV1 next)`
- `Dreamine.SecsGem.Interop.Runtime.Profiles.ConnectionProfileApplyDisposition Disposition { get; set; }`
- `System.Boolean Equals(Dreamine.SecsGem.Interop.Runtime.Profiles.ConnectionProfileApplyDiff other)`
- `System.Boolean Equals(System.Object obj)`
- `System.Boolean RequiresSessionRecreation { get; }`
- `System.Collections.Generic.IReadOnlyList<System.String> ImmediateChanges { get; set; }`
- `System.Collections.Generic.IReadOnlyList<System.String> RecreateRequiredChanges { get; set; }`
- `System.Int32 GetHashCode()`
- `System.String ToString()`
- `System.Void Deconstruct(out Dreamine.SecsGem.Interop.Runtime.Profiles.ConnectionProfileApplyDisposition Disposition, out System.Collections.Generic.IReadOnlyList<System.String> ImmediateChanges, out System.Collections.Generic.IReadOnlyList<System.String> RecreateRequiredChanges)`

### `public enum Dreamine.SecsGem.Interop.Runtime.Profiles.ConnectionProfileApplyDisposition`

- `const Dreamine.SecsGem.Interop.Runtime.Profiles.ConnectionProfileApplyDisposition ImmediateOnly = 1`
- `const Dreamine.SecsGem.Interop.Runtime.Profiles.ConnectionProfileApplyDisposition NoChanges = 0`
- `const Dreamine.SecsGem.Interop.Runtime.Profiles.ConnectionProfileApplyDisposition RecreateRequired = 2`

### `public static class Dreamine.SecsGem.Interop.Runtime.Profiles.ConnectionProfileStore`

- `Dreamine.SecsGem.Interop.Runtime.Persistence.VersionedJsonFileStore<Dreamine.SecsGem.Interop.Runtime.Profiles.SingleConnectionProfileV1> Create(Dreamine.SecsGem.Interop.Runtime.Persistence.JsonPersistenceLimits persistenceLimits, System.Collections.Generic.IEnumerable<System.String> additionalLogPolicyIds)`

### `public sealed class Dreamine.SecsGem.Interop.Runtime.Profiles.ConnectionProfileValidationException`

- `ConnectionProfileValidationException(System.String message)`
- `ConnectionProfileValidationException(System.String message, System.Exception innerException)`

### `public sealed class Dreamine.SecsGem.Interop.Runtime.Profiles.ConnectionSafetyLimitsV1`

- `ConnectionSafetyLimitsV1(System.Int32 MaximumFrameLength, System.Int32 MaximumMessageLength, System.Int32 MaximumNestingDepth, System.Int32 MaximumListItemCount)`
- `Dreamine.SecsGem.Interop.Runtime.Profiles.ConnectionSafetyLimitsV1 <Clone>$()`
- `System.Boolean Equals(Dreamine.SecsGem.Interop.Runtime.Profiles.ConnectionSafetyLimitsV1 other)`
- `System.Boolean Equals(System.Object obj)`
- `System.Int32 GetHashCode()`
- `System.Int32 MaximumFrameLength { get; set; }`
- `System.Int32 MaximumListItemCount { get; set; }`
- `System.Int32 MaximumMessageLength { get; set; }`
- `System.Int32 MaximumNestingDepth { get; set; }`
- `System.String ToString()`
- `System.Void Deconstruct(out System.Int32 MaximumFrameLength, out System.Int32 MaximumMessageLength, out System.Int32 MaximumNestingDepth, out System.Int32 MaximumListItemCount)`

### `public sealed class Dreamine.SecsGem.Interop.Runtime.Profiles.ConnectionTimerProfileV1`

- `ConnectionTimerProfileV1(System.Int32 T3Seconds, System.Int32 T5Seconds, System.Int32 T6Seconds, System.Int32 T7Seconds, System.Int32 T8Seconds)`
- `Dreamine.SecsGem.Interop.Runtime.Profiles.ConnectionTimerProfileV1 <Clone>$()`
- `System.Boolean Equals(Dreamine.SecsGem.Interop.Runtime.Profiles.ConnectionTimerProfileV1 other)`
- `System.Boolean Equals(System.Object obj)`
- `System.Int32 GetHashCode()`
- `System.Int32 T3Seconds { get; set; }`
- `System.Int32 T5Seconds { get; set; }`
- `System.Int32 T6Seconds { get; set; }`
- `System.Int32 T7Seconds { get; set; }`
- `System.Int32 T8Seconds { get; set; }`
- `System.String ToString()`
- `System.Void Deconstruct(out System.Int32 T3Seconds, out System.Int32 T5Seconds, out System.Int32 T6Seconds, out System.Int32 T7Seconds, out System.Int32 T8Seconds)`

### `public sealed class Dreamine.SecsGem.Interop.Runtime.Profiles.OperationalReconnectPolicyV1`

- `Dreamine.SecsGem.Interop.Runtime.Profiles.OperationalReconnectPolicyV1 <Clone>$()`
- `OperationalReconnectPolicyV1(System.Int32 InitialDelaySeconds, System.Int32 MaximumDelaySeconds, System.Double BackoffMultiplier)`
- `System.Boolean Equals(Dreamine.SecsGem.Interop.Runtime.Profiles.OperationalReconnectPolicyV1 other)`
- `System.Boolean Equals(System.Object obj)`
- `System.Double BackoffMultiplier { get; set; }`
- `System.Int32 GetHashCode()`
- `System.Int32 InitialDelaySeconds { get; set; }`
- `System.Int32 MaximumDelaySeconds { get; set; }`
- `System.String ToString()`
- `System.Void Deconstruct(out System.Int32 InitialDelaySeconds, out System.Int32 MaximumDelaySeconds, out System.Double BackoffMultiplier)`

### `public sealed class Dreamine.SecsGem.Interop.Runtime.Profiles.SingleConnectionProfileV1`

- `Dreamine.Secs.Abstractions.Enums.SecsConnectionMode Mode { get; set; }`
- `Dreamine.Secs.Abstractions.Enums.SecsRole Role { get; set; }`
- `Dreamine.Secs.Abstractions.Hsms.HsmsSessionOptions ToHsmsSessionOptions()`
- `Dreamine.SecsGem.Interop.Runtime.Profiles.ConnectionSafetyLimitsV1 SafetyLimits { get; set; }`
- `Dreamine.SecsGem.Interop.Runtime.Profiles.ConnectionTimerProfileV1 Timers { get; set; }`
- `Dreamine.SecsGem.Interop.Runtime.Profiles.OperationalReconnectPolicyV1 ReconnectPolicy { get; set; }`
- `Dreamine.SecsGem.Interop.Runtime.Profiles.SingleConnectionProfileV1 <Clone>$()`
- `SingleConnectionProfileV1()`
- `System.Boolean AutoReconnect { get; set; }`
- `System.Boolean Equals(Dreamine.SecsGem.Interop.Runtime.Profiles.SingleConnectionProfileV1 other)`
- `System.Boolean Equals(System.Object obj)`
- `System.Int32 GetHashCode()`
- `System.Int32 Port { get; set; }`
- `System.Int32 Version { get; set; }`
- `System.String Host { get; set; }`
- `System.String LogPolicyId { get; set; }`
- `System.String Schema { get; set; }`
- `System.String ToString()`
- `System.UInt16 SessionId { get; set; }`
- `System.Void Validate()`
- `System.Void Validate(System.Collections.Generic.IReadOnlySet<System.String> knownLogPolicyIds)`
- `const System.Int32 CurrentVersion = 1`
- `const System.String SchemaId = "dreamine.secs.connection-profile"`

### `public sealed class Dreamine.SecsGem.Interop.Runtime.Responders.ConfigurableResponderV1`

- `ConfigurableResponderV1(Dreamine.Secs.Abstractions.Interfaces.ISecsMessageSession session, Dreamine.SecsGem.Interop.Runtime.Responders.ResponderConfigurationV1 configuration, System.TimeProvider timeProvider)`
- `Dreamine.SecsGem.Interop.Runtime.Responders.ResponderFaultEventArgs LastFault { get; }`
- `System.Boolean IsEnabled { get; }`
- `System.Int32 ActiveHandlerCount { get; }`
- `System.Threading.Tasks.Task<Dreamine.SecsGem.Interop.Runtime.Responders.ResponderShutdownResultV1> DisableAsync(System.Threading.CancellationToken cancellationToken)`
- `System.Threading.Tasks.ValueTask DisposeAsync()`
- `System.Void Enable()`
- `event System.EventHandler<Dreamine.SecsGem.Interop.Runtime.Responders.ResponderFaultEventArgs> Faulted`

### `public sealed class Dreamine.SecsGem.Interop.Runtime.Responders.ResponderConfigurationV1`

- `ResponderConfigurationV1()`
- `System.Collections.Generic.List<Dreamine.SecsGem.Interop.Runtime.Responders.ResponderRuleV1> Rules { get; set; }`
- `System.Int32 ShutdownTimeoutMilliseconds { get; set; }`
- `System.Int32 Version { get; set; }`
- `System.String Schema { get; set; }`
- `System.Void Validate()`
- `const System.Int32 CurrentSchemaVersion = 1`
- `const System.Int32 MaximumRules = 1024`
- `const System.Int32 MaximumShutdownTimeoutMilliseconds = 30000`
- `const System.String SchemaName = "dreamine.secs-gem.responder"`

### `public sealed class Dreamine.SecsGem.Interop.Runtime.Responders.ResponderFaultEventArgs`

- `ResponderFaultEventArgs(System.String ruleId, System.Exception exception, System.DateTimeOffset observedAtUtc)`
- `System.DateTimeOffset ObservedAtUtc { get; }`
- `System.Exception Exception { get; }`
- `System.String RuleId { get; }`

### `public sealed class Dreamine.SecsGem.Interop.Runtime.Responders.ResponderFileStoreV1`

- `ResponderFileStoreV1()`
- `System.Threading.Tasks.Task SaveAsync(System.String path, Dreamine.SecsGem.Interop.Runtime.Responders.ResponderConfigurationV1 configuration, System.Threading.CancellationToken cancellationToken)`
- `System.Threading.Tasks.Task<Dreamine.SecsGem.Interop.Runtime.Responders.ResponderConfigurationV1> LoadAsync(System.String path, System.Threading.CancellationToken cancellationToken)`

### `public enum Dreamine.SecsGem.Interop.Runtime.Responders.ResponderInvocationModeV1`

- `const Dreamine.SecsGem.Interop.Runtime.Responders.ResponderInvocationModeV1 Once = 0`
- `const Dreamine.SecsGem.Interop.Runtime.Responders.ResponderInvocationModeV1 Repeat = 1`

### `public enum Dreamine.SecsGem.Interop.Runtime.Responders.ResponderReplyModeV1`

- `const Dreamine.SecsGem.Interop.Runtime.Responders.ResponderReplyModeV1 Delayed = 1`
- `const Dreamine.SecsGem.Interop.Runtime.Responders.ResponderReplyModeV1 Immediate = 0`
- `const Dreamine.SecsGem.Interop.Runtime.Responders.ResponderReplyModeV1 NoReply = 2`

### `public sealed class Dreamine.SecsGem.Interop.Runtime.Responders.ResponderRuleV1`

- `Dreamine.SecsGem.Interop.Runtime.Responders.ResponderInvocationModeV1 InvocationMode { get; set; }`
- `Dreamine.SecsGem.Interop.Runtime.Responders.ResponderReplyModeV1 ReplyMode { get; set; }`
- `Dreamine.SecsGem.Interop.Runtime.Templates.SecsItemTemplateNode ReplyBody { get; set; }`
- `ResponderRuleV1()`
- `System.Boolean Enabled { get; set; }`
- `System.Boolean ReplyExpected { get; set; }`
- `System.Byte PrimaryFunction { get; set; }`
- `System.Byte Stream { get; set; }`
- `System.Int32 DelayMilliseconds { get; set; }`
- `System.String Id { get; set; }`

### `public sealed class Dreamine.SecsGem.Interop.Runtime.Responders.ResponderShutdownResultV1`

- `Dreamine.SecsGem.Interop.Runtime.Responders.ResponderShutdownResultV1 <Clone>$()`
- `Dreamine.SecsGem.Interop.Runtime.Responders.ResponderShutdownStatusV1 Status { get; set; }`
- `ResponderShutdownResultV1(Dreamine.SecsGem.Interop.Runtime.Responders.ResponderShutdownStatusV1 Status, System.Int32 RemainingHandlerCount, System.String ErrorMessage)`
- `System.Boolean Equals(Dreamine.SecsGem.Interop.Runtime.Responders.ResponderShutdownResultV1 other)`
- `System.Boolean Equals(System.Object obj)`
- `System.Int32 GetHashCode()`
- `System.Int32 RemainingHandlerCount { get; set; }`
- `System.String ErrorMessage { get; set; }`
- `System.String ToString()`
- `System.Void Deconstruct(out Dreamine.SecsGem.Interop.Runtime.Responders.ResponderShutdownStatusV1 Status, out System.Int32 RemainingHandlerCount, out System.String ErrorMessage)`

### `public enum Dreamine.SecsGem.Interop.Runtime.Responders.ResponderShutdownStatusV1`

- `const Dreamine.SecsGem.Interop.Runtime.Responders.ResponderShutdownStatusV1 AlreadyStopped = 1`
- `const Dreamine.SecsGem.Interop.Runtime.Responders.ResponderShutdownStatusV1 Cancelled = 3`
- `const Dreamine.SecsGem.Interop.Runtime.Responders.ResponderShutdownStatusV1 Completed = 0`
- `const Dreamine.SecsGem.Interop.Runtime.Responders.ResponderShutdownStatusV1 TimedOut = 2`

### `public sealed class Dreamine.SecsGem.Interop.Runtime.Scenarios.ConnectScenarioStepV1`

- `ConnectScenarioStepV1()`

### `public sealed class Dreamine.SecsGem.Interop.Runtime.Scenarios.DelayScenarioStepV1`

- `DelayScenarioStepV1()`
- `System.Int32 DelayMilliseconds { get; set; }`

### `public sealed class Dreamine.SecsGem.Interop.Runtime.Scenarios.DisconnectScenarioStepV1`

- `DisconnectScenarioStepV1()`

### `public sealed class Dreamine.SecsGem.Interop.Runtime.Scenarios.ExpectScenarioStepV1`

- `Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioMessageMatcherV1 Matcher { get; set; }`
- `Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioMessageSourceV1 Source { get; set; }`
- `ExpectScenarioStepV1()`

### `public sealed class Dreamine.SecsGem.Interop.Runtime.Scenarios.LinktestScenarioStepV1`

- `LinktestScenarioStepV1()`

### `public sealed class Dreamine.SecsGem.Interop.Runtime.Scenarios.RepeatScenarioStepV1`

- `RepeatScenarioStepV1()`
- `System.Collections.Generic.List<Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioStepV1> Steps { get; set; }`
- `System.Int32 Count { get; set; }`

### `public enum Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioBindingKindV1`

- `const Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioBindingKindV1 CurrentConnection = 0`
- `const Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioBindingKindV1 NamedEquipment = 1`

### `public enum Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioBodyMatchV1`

- `const Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioBodyMatchV1 Absent = 3`
- `const Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioBodyMatchV1 Exact = 1`
- `const Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioBodyMatchV1 Ignore = 0`
- `const Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioBodyMatchV1 Structural = 2`

### `public enum Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioCorrelationV1`

- `const Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioCorrelationV1 Exact = 2`
- `const Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioCorrelationV1 Ignore = 0`
- `const Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioCorrelationV1 LastSent = 1`

### `public sealed class Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioDefinitionV1`

- `Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioExecutionBindingV1 Binding { get; set; }`
- `ScenarioDefinitionV1()`
- `System.Collections.Generic.List<Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioStepV1> Steps { get; set; }`
- `System.Int32 RunTimeoutMilliseconds { get; set; }`
- `System.Int32 Version { get; set; }`
- `System.String Id { get; set; }`
- `System.String Name { get; set; }`
- `System.String Schema { get; set; }`
- `System.Void Validate()`
- `const System.Int32 CurrentSchemaVersion = 1`
- `const System.String SchemaName = "dreamine.secs-gem.scenario"`

### `public sealed class Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioExecutionBindingV1`

- `Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioBindingKindV1 Kind { get; set; }`
- `Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioExecutionBindingV1 CurrentConnection()`
- `Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioExecutionBindingV1 NamedEquipment(System.String equipmentName)`
- `ScenarioExecutionBindingV1()`
- `System.String Target { get; set; }`
- `const System.String CurrentConnectionTarget = "$current"`

### `public static class Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioExitCodesV1`

- `const System.Int32 Cancelled = 130`
- `const System.Int32 Failed = 1`
- `const System.Int32 Invalid = 2`
- `const System.Int32 Passed = 0`
- `const System.Int32 TimedOut = 3`

### `public sealed class Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioFileStoreV1`

- `ScenarioFileStoreV1()`
- `System.Threading.Tasks.Task SaveAsync(System.String path, Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioDefinitionV1 scenario, System.Threading.CancellationToken cancellationToken)`
- `System.Threading.Tasks.Task<Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioDefinitionV1> LoadAsync(System.String path, System.Threading.CancellationToken cancellationToken)`

### `public static class Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioLimitsV1`

- `const System.Int32 MaximumAtomicValues = 65535`
- `const System.Int32 MaximumDefinedSteps = 1024`
- `const System.Int32 MaximumExpandedSteps = 10000`
- `const System.Int32 MaximumFileSizeBytes = 1048576`
- `const System.Int32 MaximumInboundQueueCapacity = 4096`
- `const System.Int32 MaximumItemDepth = 64`
- `const System.Int32 MaximumItemNodes = 10000`
- `const System.Int32 MaximumJsonDepth = 64`
- `const System.Int32 MaximumJsonNodes = 100000`
- `const System.Int32 MaximumMessageBodyBytes = 16777216`
- `const System.Int32 MaximumRepeatCount = 1000`
- `const System.Int32 MaximumRepeatDepth = 8`
- `const System.Int32 MaximumRunTimeoutMilliseconds = 3600000`
- `const System.Int32 MaximumStepTimeoutMilliseconds = 300000`
- `const System.Int32 MaximumTextCharacters = 1048576`

### `public sealed class Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioMessageMatcherV1`

- `Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioBodyMatchV1 BodyMatch { get; set; }`
- `Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioCorrelationV1 Correlation { get; set; }`
- `Dreamine.SecsGem.Interop.Runtime.Templates.SecsItemTemplateNode Body { get; set; }`
- `ScenarioMessageMatcherV1()`
- `System.Nullable<System.Boolean> ReplyExpected { get; set; }`
- `System.Nullable<System.Byte> Function { get; set; }`
- `System.Nullable<System.Byte> Stream { get; set; }`
- `System.Nullable<System.UInt16> SessionId { get; set; }`
- `System.Nullable<System.UInt32> SystemBytes { get; set; }`

### `public enum Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioMessageSourceV1`

- `const Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioMessageSourceV1 LastReply = 0`
- `const Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioMessageSourceV1 NextMessage = 1`

### `public sealed class Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioRunResultV1`

- `Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioRunResultV1 <Clone>$()`
- `Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioRunStatusV1 Status { get; set; }`
- `ScenarioRunResultV1(Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioRunStatusV1 Status, System.Int32 ExitCode, System.DateTimeOffset StartedAtUtc, System.DateTimeOffset CompletedAtUtc, System.Collections.Generic.IReadOnlyList<Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioStepResultV1> Steps, System.String ErrorCode, System.String ErrorMessage, System.Int64 DroppedInboundMessageCount)`
- `System.Boolean Equals(Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioRunResultV1 other)`
- `System.Boolean Equals(System.Object obj)`
- `System.Collections.Generic.IReadOnlyList<Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioStepResultV1> Steps { get; set; }`
- `System.DateTimeOffset CompletedAtUtc { get; set; }`
- `System.DateTimeOffset StartedAtUtc { get; set; }`
- `System.Int32 ExitCode { get; set; }`
- `System.Int32 GetHashCode()`
- `System.Int64 DroppedInboundMessageCount { get; set; }`
- `System.String ErrorCode { get; set; }`
- `System.String ErrorMessage { get; set; }`
- `System.String ToString()`
- `System.Void Deconstruct(out Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioRunStatusV1 Status, out System.Int32 ExitCode, out System.DateTimeOffset StartedAtUtc, out System.DateTimeOffset CompletedAtUtc, out System.Collections.Generic.IReadOnlyList<Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioStepResultV1> Steps, out System.String ErrorCode, out System.String ErrorMessage, out System.Int64 DroppedInboundMessageCount)`

### `public enum Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioRunStatusV1`

- `const Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioRunStatusV1 Cancelled = 4`
- `const Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioRunStatusV1 Failed = 2`
- `const Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioRunStatusV1 Invalid = 1`
- `const Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioRunStatusV1 Passed = 0`
- `const Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioRunStatusV1 TimedOut = 3`

### `public sealed class Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioRunnerV1`

- `ScenarioRunnerV1(System.TimeProvider timeProvider, System.Int32 inboundQueueCapacity)`
- `System.Threading.Tasks.Task<Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioRunResultV1> RunAsync(Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioDefinitionV1 scenario, Dreamine.Secs.Abstractions.Interfaces.ISecsMessageSession session, System.Threading.CancellationToken cancellationToken)`

### `public sealed class Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioStepResultV1`

- `Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioStepResultV1 <Clone>$()`
- `Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioStepStatusV1 Status { get; set; }`
- `ScenarioStepResultV1(System.String Path, Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioStepStatusV1 Status, System.DateTimeOffset StartedAtUtc, System.DateTimeOffset CompletedAtUtc, System.String ErrorCode, System.String ErrorMessage)`
- `System.Boolean Equals(Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioStepResultV1 other)`
- `System.Boolean Equals(System.Object obj)`
- `System.DateTimeOffset CompletedAtUtc { get; set; }`
- `System.DateTimeOffset StartedAtUtc { get; set; }`
- `System.Int32 GetHashCode()`
- `System.String ErrorCode { get; set; }`
- `System.String ErrorMessage { get; set; }`
- `System.String Path { get; set; }`
- `System.String ToString()`
- `System.Void Deconstruct(out System.String Path, out Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioStepStatusV1 Status, out System.DateTimeOffset StartedAtUtc, out System.DateTimeOffset CompletedAtUtc, out System.String ErrorCode, out System.String ErrorMessage)`

### `public enum Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioStepStatusV1`

- `const Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioStepStatusV1 Cancelled = 3`
- `const Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioStepStatusV1 Failed = 1`
- `const Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioStepStatusV1 Passed = 0`
- `const Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioStepStatusV1 TimedOut = 2`

### `public abstract class Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioStepV1`

- `System.Int32 TimeoutMilliseconds { get; set; }`
- `System.String Id { get; set; }`
- `System.String Target { get; set; }`

### `public enum Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioWaitStateV1`

- `const Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioWaitStateV1 Connected = 0`
- `const Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioWaitStateV1 Selected = 1`

### `public sealed class Dreamine.SecsGem.Interop.Runtime.Scenarios.SelectScenarioStepV1`

- `SelectScenarioStepV1()`

### `public sealed class Dreamine.SecsGem.Interop.Runtime.Scenarios.SendScenarioStepV1`

- `Dreamine.SecsGem.Interop.Runtime.Templates.SecsItemTemplateNode Body { get; set; }`
- `SendScenarioStepV1()`
- `System.Byte PrimaryFunction { get; set; }`
- `System.Byte Stream { get; set; }`
- `System.Nullable<System.Byte> SecondaryFunction { get; set; }`

### `public sealed class Dreamine.SecsGem.Interop.Runtime.Scenarios.SeparateScenarioStepV1`

- `SeparateScenarioStepV1()`

### `public sealed class Dreamine.SecsGem.Interop.Runtime.Scenarios.WaitForStateScenarioStepV1`

- `Dreamine.SecsGem.Interop.Runtime.Scenarios.ScenarioWaitStateV1 State { get; set; }`
- `WaitForStateScenarioStepV1()`

### `public static class Dreamine.SecsGem.Interop.Runtime.Templates.MessageTemplateCatalogStore`

- `Dreamine.SecsGem.Interop.Runtime.Persistence.VersionedJsonFileStore<Dreamine.SecsGem.Interop.Runtime.Templates.MessageTemplateCatalogV1> Create(Dreamine.SecsGem.Interop.Runtime.Templates.MessageTemplateLimits templateLimits, Dreamine.SecsGem.Interop.Runtime.Persistence.JsonPersistenceLimits persistenceLimits)`

### `public sealed class Dreamine.SecsGem.Interop.Runtime.Templates.MessageTemplateCatalogV1`

- `Dreamine.SecsGem.Interop.Runtime.Templates.MessageTemplateV1 CloneTemplateAt(System.Int32 index, System.String newName, Dreamine.SecsGem.Interop.Runtime.Templates.MessageTemplateLimits limits)`
- `MessageTemplateCatalogV1()`
- `System.Boolean MoveTemplateDown(System.Int32 index)`
- `System.Boolean MoveTemplateUp(System.Int32 index)`
- `System.Collections.Generic.List<Dreamine.SecsGem.Interop.Runtime.Templates.MessageTemplateV1> Templates { get; set; }`
- `System.Int32 Version { get; set; }`
- `System.String Schema { get; set; }`
- `System.Void AddTemplate(Dreamine.SecsGem.Interop.Runtime.Templates.MessageTemplateV1 template, Dreamine.SecsGem.Interop.Runtime.Templates.MessageTemplateLimits limits)`
- `System.Void RemoveTemplateAt(System.Int32 index)`
- `System.Void Validate(Dreamine.SecsGem.Interop.Runtime.Templates.MessageTemplateLimits limits)`
- `System.Void ValidateForSend(Dreamine.SecsGem.Interop.Runtime.Templates.MessageTemplateLimits limits)`
- `const System.Int32 CurrentVersion = 1`
- `const System.String SchemaId = "dreamine.secs.message-template-catalog"`

### `public enum Dreamine.SecsGem.Interop.Runtime.Templates.MessageTemplateDirection`

- `const Dreamine.SecsGem.Interop.Runtime.Templates.MessageTemplateDirection EquipmentToHost = 2`
- `const Dreamine.SecsGem.Interop.Runtime.Templates.MessageTemplateDirection HostToEquipment = 1`
- `const Dreamine.SecsGem.Interop.Runtime.Templates.MessageTemplateDirection Unspecified = 0`

### `public enum Dreamine.SecsGem.Interop.Runtime.Templates.MessageTemplateKind`

- `const Dreamine.SecsGem.Interop.Runtime.Templates.MessageTemplateKind Primary = 1`
- `const Dreamine.SecsGem.Interop.Runtime.Templates.MessageTemplateKind Secondary = 2`
- `const Dreamine.SecsGem.Interop.Runtime.Templates.MessageTemplateKind Unspecified = 0`

### `public sealed class Dreamine.SecsGem.Interop.Runtime.Templates.MessageTemplateLimits`

- `Dreamine.SecsGem.Interop.Runtime.Templates.MessageTemplateLimits <Clone>$()`
- `MessageTemplateLimits(System.Int32 MaximumNodeCount, System.Int32 MaximumTreeDepth, System.Int32 MaximumEncodedItemBytes, System.Int32 MaximumListItemCount)`
- `System.Boolean Equals(Dreamine.SecsGem.Interop.Runtime.Templates.MessageTemplateLimits other)`
- `System.Boolean Equals(System.Object obj)`
- `System.Int32 GetHashCode()`
- `System.Int32 MaximumEncodedItemBytes { get; set; }`
- `System.Int32 MaximumListItemCount { get; set; }`
- `System.Int32 MaximumNodeCount { get; set; }`
- `System.Int32 MaximumTreeDepth { get; set; }`
- `System.String ToString()`
- `System.Void Deconstruct(out System.Int32 MaximumNodeCount, out System.Int32 MaximumTreeDepth, out System.Int32 MaximumEncodedItemBytes, out System.Int32 MaximumListItemCount)`
- `System.Void Validate()`

### `public sealed class Dreamine.SecsGem.Interop.Runtime.Templates.MessageTemplateV1`

- `Dreamine.Secs.Abstractions.Model.SecsItem BuildItem(Dreamine.SecsGem.Interop.Runtime.Templates.MessageTemplateLimits limits)`
- `Dreamine.Secs.Abstractions.Model.SecsMessage BuildMessage(Dreamine.Secs.Abstractions.Model.SecsSessionId sessionId, Dreamine.Secs.Abstractions.Model.SecsSystemBytes systemBytes, Dreamine.SecsGem.Interop.Runtime.Templates.MessageTemplateLimits limits)`
- `Dreamine.SecsGem.Interop.Runtime.Templates.MessageTemplateDirection Direction { get; set; }`
- `Dreamine.SecsGem.Interop.Runtime.Templates.MessageTemplateKind Kind { get; set; }`
- `Dreamine.SecsGem.Interop.Runtime.Templates.MessageTemplateV1 <Clone>$()`
- `Dreamine.SecsGem.Interop.Runtime.Templates.MessageTemplateV1 CloneDeep(Dreamine.SecsGem.Interop.Runtime.Templates.MessageTemplateLimits limits)`
- `Dreamine.SecsGem.Interop.Runtime.Templates.MessageTemplateV1 FromReceivedMessage(System.String name, Dreamine.SecsGem.Interop.Runtime.Templates.MessageTemplateDirection direction, Dreamine.Secs.Abstractions.Model.SecsMessage message)`
- `Dreamine.SecsGem.Interop.Runtime.Templates.SecsItemTemplateNode Root { get; set; }`
- `Dreamine.SecsGem.Interop.Runtime.Templates.TemplateBodyLogPolicy BodyLogPolicy { get; set; }`
- `MessageTemplateV1()`
- `System.Boolean Equals(Dreamine.SecsGem.Interop.Runtime.Templates.MessageTemplateV1 other)`
- `System.Boolean Equals(System.Object obj)`
- `System.Boolean WaitBit { get; set; }`
- `System.Byte Function { get; set; }`
- `System.Byte Stream { get; set; }`
- `System.Int32 GetHashCode()`
- `System.String Description { get; set; }`
- `System.String Name { get; set; }`
- `System.String ToString()`
- `System.Void Validate(Dreamine.SecsGem.Interop.Runtime.Templates.MessageTemplateLimits limits)`

### `public static class Dreamine.SecsGem.Interop.Runtime.Templates.MessageTemplateWireCaptureAdapter`

- `Dreamine.Secs.Abstractions.Hsms.HsmsWireCaptureRule ToHsmsWireCaptureRule(Dreamine.SecsGem.Interop.Runtime.Templates.MessageTemplateV1 template, Dreamine.Secs.Abstractions.Enums.SecsRole localRole, System.Int32 maximumFullBodyBytes)`
- `Dreamine.Secs.Abstractions.Hsms.HsmsWireObservationOptions CreateObservationOptions(Dreamine.Secs.Abstractions.Enums.SecsRole localRole, System.Collections.Generic.IEnumerable<Dreamine.SecsGem.Interop.Runtime.Templates.MessageTemplateV1> templates, System.Int32 queueCapacity, System.Int32 maximumFullBodyBytes, System.Int32 maximumDecodedCharacters)`

### `public enum Dreamine.SecsGem.Interop.Runtime.Templates.MultipleRootHandling`

- `const Dreamine.SecsGem.Interop.Runtime.Templates.MultipleRootHandling Reject = 0`
- `const Dreamine.SecsGem.Interop.Runtime.Templates.MultipleRootHandling WrapInList = 1`

### `public sealed class Dreamine.SecsGem.Interop.Runtime.Templates.PendingPrimaryReply`

- `Dreamine.Secs.Abstractions.Model.SecsConnectionIdentity SourceIdentity { get; }`
- `Dreamine.Secs.Abstractions.Model.SecsFunction PrimaryFunction { get; }`
- `Dreamine.Secs.Abstractions.Model.SecsFunction SecondaryFunction { get; }`
- `Dreamine.Secs.Abstractions.Model.SecsSessionId SessionId { get; }`
- `Dreamine.Secs.Abstractions.Model.SecsStream Stream { get; }`
- `Dreamine.Secs.Abstractions.Model.SecsSystemBytes SystemBytes { get; }`
- `Dreamine.SecsGem.Interop.Runtime.Templates.MessageTemplateV1 CreateSecondaryDraft(System.String name)`
- `Dreamine.SecsGem.Interop.Runtime.Templates.MessageTemplateV1 InboundPrimary { get; }`
- `Dreamine.SecsGem.Interop.Runtime.Templates.PendingPrimaryReply Capture(Dreamine.Secs.Abstractions.Interfaces.ISecsPrimaryContext context, System.String inboundTemplateName)`
- `System.Boolean ReplyAttempted { get; }`
- `System.Threading.Tasks.ValueTask ReplyAsync(Dreamine.SecsGem.Interop.Runtime.Templates.MessageTemplateV1 secondaryTemplate, System.Threading.CancellationToken cancellationToken, Dreamine.SecsGem.Interop.Runtime.Templates.MessageTemplateLimits limits)`

### `public sealed class Dreamine.SecsGem.Interop.Runtime.Templates.SecsItemTemplateNode`

- `Dreamine.Secs.Abstractions.Model.SecsItem BuildItem(Dreamine.SecsGem.Interop.Runtime.Templates.MessageTemplateLimits limits)`
- `Dreamine.Secs.Abstractions.Model.SecsItemFormat Format { get; set; }`
- `Dreamine.SecsGem.Interop.Runtime.Templates.SecsItemTemplateNode CloneDeep(Dreamine.SecsGem.Interop.Runtime.Templates.MessageTemplateLimits limits)`
- `Dreamine.SecsGem.Interop.Runtime.Templates.SecsItemTemplateNode FromSecsItem(Dreamine.Secs.Abstractions.Model.SecsItem item)`
- `Dreamine.SecsGem.Interop.Runtime.Templates.SecsItemTemplateNode ImportLegacyRoots(System.Collections.Generic.IEnumerable<Dreamine.SecsGem.Interop.Runtime.Templates.SecsItemTemplateNode> roots, Dreamine.SecsGem.Interop.Runtime.Templates.MultipleRootHandling handling)`
- `SecsItemTemplateNode()`
- `SecsItemTemplateNode(Dreamine.Secs.Abstractions.Model.SecsItemFormat format, System.Collections.Generic.IEnumerable<System.String> values)`
- `System.Boolean IsSensitive { get; set; }`
- `System.Boolean MoveChildDown(System.Int32 index)`
- `System.Boolean MoveChildUp(System.Int32 index)`
- `System.Boolean RemoveChild(Dreamine.SecsGem.Interop.Runtime.Templates.SecsItemTemplateNode child)`
- `System.Collections.Generic.List<Dreamine.SecsGem.Interop.Runtime.Templates.SecsItemTemplateNode> Children { get; set; }`
- `System.Collections.Generic.List<System.String> Values { get; set; }`
- `System.Void AddChild(Dreamine.SecsGem.Interop.Runtime.Templates.SecsItemTemplateNode child)`

### `public enum Dreamine.SecsGem.Interop.Runtime.Templates.TemplateBodyLogPolicy`

- `const Dreamine.SecsGem.Interop.Runtime.Templates.TemplateBodyLogPolicy Excluded = 1`
- `const Dreamine.SecsGem.Interop.Runtime.Templates.TemplateBodyLogPolicy FullBodyExplicit = 2`
- `const Dreamine.SecsGem.Interop.Runtime.Templates.TemplateBodyLogPolicy HeaderOnly = 0`

### `public sealed class Dreamine.SecsGem.Interop.Runtime.Templates.TemplateValidationException`

- `TemplateValidationException(System.String message)`
- `TemplateValidationException(System.String message, System.Exception innerException)`
