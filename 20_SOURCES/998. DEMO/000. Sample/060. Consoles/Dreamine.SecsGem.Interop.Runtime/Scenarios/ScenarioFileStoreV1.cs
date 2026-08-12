using Dreamine.SecsGem.Interop.Runtime.Persistence;

namespace Dreamine.SecsGem.Interop.Runtime.Scenarios;

/// <summary>
/// Persists the exact scenario v1 schema through the shared bounded, atomic JSON store.
/// Named-equipment lookup remains a caller-owned execution-binding adapter.
/// </summary>
public sealed class ScenarioFileStoreV1
{
    private readonly VersionedJsonFileStore<ScenarioDefinitionV1> _store;

    public ScenarioFileStoreV1()
    {
        _store = new VersionedJsonFileStore<ScenarioDefinitionV1>(
            ScenarioDefinitionV1.SchemaName,
            ScenarioDefinitionV1.CurrentSchemaVersion,
            static scenario => scenario.Validate(),
            new JsonPersistenceLimits(
                ScenarioLimitsV1.MaximumFileSizeBytes,
                ScenarioLimitsV1.MaximumJsonDepth,
                ScenarioLimitsV1.MaximumJsonNodes));
    }

    public Task<ScenarioDefinitionV1> LoadAsync(string path, CancellationToken cancellationToken = default) =>
        _store.LoadAsync(path, cancellationToken);

    public Task SaveAsync(
        string path,
        ScenarioDefinitionV1 scenario,
        CancellationToken cancellationToken = default) =>
        _store.SaveAsync(path, scenario, cancellationToken);
}
