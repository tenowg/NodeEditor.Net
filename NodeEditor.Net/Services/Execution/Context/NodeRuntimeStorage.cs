using System.Collections.Concurrent;

namespace NodeEditor.Net.Services.Execution;

public sealed class NodeRuntimeStorage : INodeRuntimeStorage
{
    private readonly ConcurrentDictionary<string, object?> _socketValues;
    private readonly ConcurrentDictionary<string, bool> _executedNodes;
    private readonly ConcurrentDictionary<string, object?> _variables;
    private readonly Stack<int> _generationStack = new();
    private int _currentGeneration;

    public ExecutionEventBus EventBus { get; }

    public IGraphSharedContext Shared { get; }

    public NodeRuntimeStorage()
        : this(new GraphSharedContext())
    {
    }

    public NodeRuntimeStorage(IGraphSharedContext shared)
    {
        ArgumentNullException.ThrowIfNull(shared);
        _socketValues = new ConcurrentDictionary<string, object?>(StringComparer.Ordinal);
        _executedNodes = new ConcurrentDictionary<string, bool>(StringComparer.Ordinal);
        _variables = new ConcurrentDictionary<string, object?>(StringComparer.Ordinal);
        EventBus = new ExecutionEventBus();
        Shared = shared;
    }

    private NodeRuntimeStorage(
        ConcurrentDictionary<string, object?> socketValues,
        ConcurrentDictionary<string, bool> executedNodes,
        ConcurrentDictionary<string, object?> variables,
        ExecutionEventBus eventBus,
        IGraphSharedContext shared)
    {
        _socketValues = socketValues;
        _executedNodes = executedNodes;
        _variables = variables;
        EventBus = eventBus;
        Shared = shared;
    }

    public bool TryGetSocketValue(string nodeId, string socketName, out object? value)
    {
        return _socketValues.TryGetValue(BuildSocketKey(nodeId, socketName), out value);
    }

    public object? GetSocketValue(string nodeId, string socketName)
    {
        _socketValues.TryGetValue(BuildSocketKey(nodeId, socketName), out var value);
        return value;
    }

    public void SetSocketValue(string nodeId, string socketName, object? value)
    {
        _socketValues[BuildSocketKey(nodeId, socketName)] = value;
    }

    public bool IsNodeExecuted(string nodeId)
    {
        return _executedNodes.ContainsKey(nodeId);
    }

    public void MarkNodeExecuted(string nodeId)
    {
        _executedNodes[nodeId] = true;
    }

    public void ClearNodeExecuted(string nodeId)
    {
        _executedNodes.TryRemove(nodeId, out _);
    }

    public object? GetVariable(string key)
    {
        _variables.TryGetValue(key, out var value);
        return value;
    }

    public void SetVariable(string key, object? value)
    {
        _variables[key] = value;
    }

    public IReadOnlyCollection<string> GetExecutedNodeIds()
    {
        return _executedNodes.Keys.ToArray();
    }

    public IReadOnlyList<(string NodeId, string SocketName, object? Value)> GetSocketEntries()
    {
        var entries = new List<(string, string, object?)>(_socketValues.Count);
        foreach (var kvp in _socketValues)
        {
            if (!TrySplitSocketKey(kvp.Key, out var nodeId, out var socketName))
                continue;

            entries.Add((nodeId, socketName, kvp.Value));
        }

        return entries;
    }

    public IReadOnlyList<(string Key, object? Value)> GetVariableEntries()
    {
        var entries = new List<(string, object?)>(_variables.Count);
        foreach (var kvp in _variables)
            entries.Add((kvp.Key, kvp.Value));

        return entries;
    }

    // ── Iteration generation (for loop body scoping) ──

    public int CurrentGeneration => _currentGeneration;

    public void PushGeneration()
    {
        _generationStack.Push(_currentGeneration);
        _currentGeneration++;
    }

    public void PopGeneration()
    {
        if (_generationStack.Count > 0)
            _currentGeneration = _generationStack.Pop();
    }

    public void ClearExecutedForNodes(IEnumerable<string> nodeIds)
    {
        foreach (var id in nodeIds)
            _executedNodes.TryRemove(id, out _);
    }

    public INodeRuntimeStorage CreateChild(string scopeName, bool inheritVariables = true)
    {
        var socketValues = new ConcurrentDictionary<string, object?>(StringComparer.Ordinal);
        var executedNodes = new ConcurrentDictionary<string, bool>(StringComparer.Ordinal);
        var variables = inheritVariables
            ? new ConcurrentDictionary<string, object?>(_variables, StringComparer.Ordinal)
            : new ConcurrentDictionary<string, object?>(StringComparer.Ordinal);

        return new NodeRuntimeStorage(socketValues, executedNodes, variables, EventBus, Shared);
    }

    private static string BuildSocketKey(string nodeId, string socketName)
    {
        return string.Concat(nodeId, "::", socketName);
    }

    private static bool TrySplitSocketKey(string key, out string nodeId, out string socketName)
    {
        var separator = key.IndexOf("::", StringComparison.Ordinal);
        if (separator <= 0 || separator + 2 >= key.Length)
        {
            nodeId = string.Empty;
            socketName = string.Empty;
            return false;
        }

        nodeId = key[..separator];
        socketName = key[(separator + 2)..];
        return true;
    }
}
