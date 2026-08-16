namespace NodeEditor.Net.Services.Execution;

/// <summary>
/// Low-level runtime storage for node execution state.
/// Used internally by the execution engine. Most node implementations
/// should use INodeExecutionContext's high-level APIs instead.
/// </summary>
public interface INodeRuntimeStorage
{
    bool TryGetSocketValue(string nodeId, string socketName, out object? value);
    object? GetSocketValue(string nodeId, string socketName);
    void SetSocketValue(string nodeId, string socketName, object? value);

    bool IsNodeExecuted(string nodeId);
    void MarkNodeExecuted(string nodeId);
    void ClearNodeExecuted(string nodeId);

    object? GetVariable(string key);
    void SetVariable(string key, object? value);

    /// <summary>Node IDs marked executed in this storage layer (local only).</summary>
    IReadOnlyCollection<string> GetExecutedNodeIds();

    /// <summary>Socket values stored in this storage layer (local only).</summary>
    IReadOnlyList<(string NodeId, string SocketName, object? Value)> GetSocketEntries();

    /// <summary>Variables stored in this storage layer (local only).</summary>
    IReadOnlyList<(string Key, object? Value)> GetVariableEntries();

    int CurrentGeneration { get; }
    void PushGeneration();
    void PopGeneration();
    void ClearExecutedForNodes(IEnumerable<string> nodeIds);

    INodeRuntimeStorage CreateChild(string scopeName, bool inheritVariables = true);

    ExecutionEventBus EventBus { get; }

    /// <summary>
    /// Host-seeded bag of typed objects visible to every node for the lifetime of this storage.
    /// Shared across group children and parallel layered scopes. Reusing this storage
    /// on a later <c>ExecuteAsync</c> keeps both the executed-node cache and this bag.
    /// </summary>
    IGraphSharedContext Shared { get; }
}
