namespace NodeEditor.Net.Models;

/// <summary>
/// Serializable snapshot of executed-node cache, socket values, and runtime variables.
/// Does not include <c>Shared</c>, the event bus, or loop generation state.
/// </summary>
public sealed record class RuntimeStorageSnapshot(
    List<string> ExecutedNodeIds,
    List<RuntimeSocketEntry> Sockets,
    List<RuntimeVariableEntry> Variables);

public sealed record class RuntimeSocketEntry(
    string NodeId,
    string SocketName,
    SocketValue? Value);

public sealed record class RuntimeVariableEntry(
    string Key,
    SocketValue? Value);
