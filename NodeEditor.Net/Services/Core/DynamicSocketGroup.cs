using NodeEditor.Net.Models;
using NodeEditor.Net.Records;
using NodeEditor.Net.Services.Execution;

namespace NodeEditor.Net.Services;

/// <summary>
/// Names, compacting, and list assembly for opt-in dynamic list input sockets.
/// Item sockets are named <c>Group[index]</c> (for example <c>Items[0]</c>).
/// </summary>
public static class DynamicSocketGroup
{
    public static string FormatName(string group, int index) => $"{group}[{index}]";

    public static bool TryParse(string? socketName, out string group, out int index)
    {
        group = string.Empty;
        index = -1;
        if (string.IsNullOrEmpty(socketName))
        {
            return false;
        }

        var open = socketName.LastIndexOf('[');
        if (open <= 0 || socketName[^1] != ']')
        {
            return false;
        }

        var indexSpan = socketName.AsSpan(open + 1, socketName.Length - open - 2);
        if (!int.TryParse(indexSpan, out index) || index < 0)
        {
            return false;
        }

        group = socketName[..open];
        return group.Length > 0;
    }

    public static bool IsDynamic(SocketData socket) =>
        socket.DynamicGroup is { Length: > 0 } && socket.DynamicIndex is >= 0;

    public static bool HasValue(SocketValue? value) => value?.Json is not null;

    public static bool IsFilled(SocketData socket, bool isConnected) =>
        isConnected || HasValue(socket.Value);

    public static bool HasGroup(IEnumerable<SocketData> inputs, string group) =>
        inputs.Any(socket => IsDynamic(socket) &&
                             string.Equals(socket.DynamicGroup, group, StringComparison.Ordinal));

    public static IReadOnlyList<string> GetGroups(IEnumerable<SocketData> inputs) =>
        inputs
            .Where(IsDynamic)
            .Select(socket => socket.DynamicGroup!)
            .Distinct(StringComparer.Ordinal)
            .ToList();

    public static SocketData CreateItemSocket(
        string group,
        int index,
        string itemTypeName,
        SocketEditorHint? editorHint = null,
        CustomEditorHint? customEditor = null)
    {
        return new SocketData(
            Name: FormatName(group, index),
            TypeName: itemTypeName,
            IsInput: true,
            IsExecution: false,
            Value: null,
            EditorHint: editorHint,
            CustomEditor: customEditor,
            DynamicGroup: group,
            DynamicIndex: index,
            DynamicItemTypeName: itemTypeName);
    }

    /// <summary>
    /// Inserts definition seed sockets for any dynamic group missing from <paramref name="persisted"/>.
    /// </summary>
    public static IReadOnlyList<SocketData> SeedMissingGroups(
        IReadOnlyList<SocketData> persisted,
        IReadOnlyList<SocketData> definitionInputs)
    {
        var result = persisted.ToList();
        var existingGroups = new HashSet<string>(
            persisted.Where(IsDynamic).Select(socket => socket.DynamicGroup!),
            StringComparer.Ordinal);

        foreach (var group in GetGroups(definitionInputs))
        {
            if (existingGroups.Contains(group))
            {
                continue;
            }

            var seeds = definitionInputs.Where(socket =>
                    IsDynamic(socket) &&
                    string.Equals(socket.DynamicGroup, group, StringComparison.Ordinal))
                .ToList();
            if (seeds.Count == 0)
            {
                continue;
            }

            var insertAt = FindInsertIndex(result, definitionInputs, group);
            result.InsertRange(insertAt, seeds);
            existingGroups.Add(group);
        }

        return result;
    }

    public static DynamicSocketSyncResult Sync(
        IReadOnlyList<SocketData> inputs,
        IReadOnlyList<ConnectionData> connections,
        string nodeId,
        IReadOnlyList<SocketData>? definitionInputs = null)
    {
        var workingInputs = definitionInputs is null
            ? inputs.ToList()
            : SeedMissingGroups(inputs, definitionInputs).ToList();

        var connected = new HashSet<string>(
            connections
                .Where(connection =>
                    string.Equals(connection.InputNodeId, nodeId, StringComparison.Ordinal) &&
                    !connection.IsExecution)
                .Select(connection => connection.InputSocketName),
            StringComparer.Ordinal);

        var nameMap = new Dictionary<string, string>(StringComparer.Ordinal);
        var groups = GetGroups(workingInputs);
        if (groups.Count == 0)
        {
            return new DynamicSocketSyncResult(workingInputs, connections, nameMap, Changed: !SameSockets(inputs, workingInputs));
        }

        foreach (var group in groups)
        {
            CompactGroup(workingInputs, group, connected, nameMap);
        }

        var remappedConnections = RemapConnections(connections, nodeId, nameMap);
        var changed = !SameSockets(inputs, workingInputs) || !connections.SequenceEqual(remappedConnections);
        return new DynamicSocketSyncResult(workingInputs, remappedConnections, nameMap, changed);
    }

    public static bool IsListType(Type type) => type == typeof(SerializableList);

    public static bool TryAssemble<T>(
        NodeData node,
        string group,
        INodeRuntimeStorage storage,
        out T value)
    {
        value = default!;
        if (!IsListType(typeof(T)) || !HasGroup(node.Inputs, group))
        {
            return false;
        }

        var list = AssembleList(
            node.Inputs,
            group,
            name => storage.TryGetSocketValue(node.Id, name, out _),
            name => storage.TryGetSocketValue(node.Id, name, out var resolved) ? resolved : null);

        value = (T)(object)list;
        return true;
    }

    public static void CacheAssembledLists(NodeData node, INodeRuntimeStorage storage)
    {
        foreach (var group in GetGroups(node.Inputs))
        {
            var list = AssembleList(
                node.Inputs,
                group,
                name => storage.TryGetSocketValue(node.Id, name, out _),
                name => storage.TryGetSocketValue(node.Id, name, out var resolved) ? resolved : null);
            storage.SetSocketValue(node.Id, group, list);
        }
    }

    public static SerializableList AssembleList(
        IReadOnlyList<SocketData> inputs,
        string group,
        Func<string, bool> tryGetResolved,
        Func<string, object?> getResolved)
    {
        var list = new SerializableList();
        var items = inputs
            .Where(socket =>
                IsDynamic(socket) &&
                string.Equals(socket.DynamicGroup, group, StringComparison.Ordinal))
            .OrderBy(socket => socket.DynamicIndex);

        foreach (var socket in items)
        {
            if (tryGetResolved(socket.Name))
            {
                list.Add(getResolved(socket.Name)!);
                continue;
            }

            if (HasValue(socket.Value))
            {
                list.Add(MaterializeValue(socket.Value!)!);
            }
        }

        return list;
    }

    private static void CompactGroup(
        List<SocketData> inputs,
        string group,
        HashSet<string> connected,
        Dictionary<string, string> nameMap)
    {
        var indices = new List<int>();
        for (var i = 0; i < inputs.Count; i++)
        {
            if (IsDynamic(inputs[i]) &&
                string.Equals(inputs[i].DynamicGroup, group, StringComparison.Ordinal))
            {
                indices.Add(i);
            }
        }

        if (indices.Count == 0)
        {
            return;
        }

        var existing = indices.Select(i => inputs[i]).OrderBy(socket => socket.DynamicIndex ?? int.MaxValue).ToList();
        var template = existing[0];
        var itemTypeName = template.DynamicItemTypeName ?? template.TypeName;
        var filled = existing.Where(socket => IsFilled(socket, connected.Contains(socket.Name))).ToList();

        var rebuilt = new List<SocketData>(filled.Count + 1);
        for (var i = 0; i < filled.Count; i++)
        {
            var source = filled[i];
            var newName = FormatName(group, i);
            if (!string.Equals(source.Name, newName, StringComparison.Ordinal))
            {
                nameMap[source.Name] = newName;
            }

            rebuilt.Add(source with
            {
                Name = newName,
                DynamicGroup = group,
                DynamicIndex = i,
                DynamicItemTypeName = itemTypeName
            });
        }

        rebuilt.Add(CreateItemSocket(group, filled.Count, itemTypeName, template.EditorHint, template.CustomEditor));

        var insertAt = indices[0];
        for (var i = indices.Count - 1; i >= 0; i--)
        {
            inputs.RemoveAt(indices[i]);
        }

        inputs.InsertRange(insertAt, rebuilt);
    }

    private static IReadOnlyList<ConnectionData> RemapConnections(
        IReadOnlyList<ConnectionData> connections,
        string nodeId,
        IReadOnlyDictionary<string, string> nameMap)
    {
        if (nameMap.Count == 0)
        {
            return connections;
        }

        var result = new List<ConnectionData>(connections.Count);
        var changed = false;
        foreach (var connection in connections)
        {
            if (string.Equals(connection.InputNodeId, nodeId, StringComparison.Ordinal) &&
                nameMap.TryGetValue(connection.InputSocketName, out var newName))
            {
                result.Add(connection with { InputSocketName = newName });
                changed = true;
            }
            else
            {
                result.Add(connection);
            }
        }

        return changed ? result : connections;
    }

    private static int FindInsertIndex(
        IReadOnlyList<SocketData> current,
        IReadOnlyList<SocketData> definitionInputs,
        string group)
    {
        var firstGroupIndex = -1;
        for (var i = 0; i < definitionInputs.Count; i++)
        {
            if (IsDynamic(definitionInputs[i]) &&
                string.Equals(definitionInputs[i].DynamicGroup, group, StringComparison.Ordinal))
            {
                firstGroupIndex = i;
                break;
            }
        }

        if (firstGroupIndex <= 0)
        {
            return Math.Min(current.Count, CountLeadingExecutionInputs(current));
        }

        var insertAfter = -1;
        for (var i = 0; i < firstGroupIndex; i++)
        {
            var name = definitionInputs[i].Name;
            for (var j = 0; j < current.Count; j++)
            {
                if (string.Equals(current[j].Name, name, StringComparison.Ordinal))
                {
                    insertAfter = Math.Max(insertAfter, j);
                }
            }
        }

        return insertAfter >= 0 ? insertAfter + 1 : Math.Min(current.Count, CountLeadingExecutionInputs(current));
    }

    private static int CountLeadingExecutionInputs(IReadOnlyList<SocketData> inputs)
    {
        var count = 0;
        while (count < inputs.Count && inputs[count].IsExecution)
        {
            count++;
        }

        return count;
    }

    internal static object? MaterializeValue(SocketValue value)
    {
        if (value.Json is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(value.TypeName))
        {
            var type = Type.GetType(value.TypeName);
            if (type is not null)
            {
                return System.Text.Json.JsonSerializer.Deserialize(value.Json.Value.GetRawText(), type);
            }
        }

        return value.ToObject<object>();
    }

    private static bool SameSockets(IReadOnlyList<SocketData> left, IReadOnlyList<SocketData> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        for (var i = 0; i < left.Count; i++)
        {
            if (left[i] != right[i])
            {
                return false;
            }
        }

        return true;
    }
}

public sealed record DynamicSocketSyncResult(
    IReadOnlyList<SocketData> Inputs,
    IReadOnlyList<ConnectionData> Connections,
    IReadOnlyDictionary<string, string> RenamedSockets,
    bool Changed);
