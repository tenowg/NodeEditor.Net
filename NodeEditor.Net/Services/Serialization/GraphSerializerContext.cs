using System.Text.Json.Serialization;
using NodeEditor.Net.Models;
using NodeEditor.Net.Records;

namespace NodeEditor.Net.Services.Serialization;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    Converters = [typeof(CustomEditorHintJsonConverter)],
    WriteIndented = true)]
[JsonSerializable(typeof(GraphDto))]
[JsonSerializable(typeof(NodeDto))]
[JsonSerializable(typeof(ConnectionDto))]
[JsonSerializable(typeof(ViewportDto))]
[JsonSerializable(typeof(SocketData))]
[JsonSerializable(typeof(SocketEditorHint))]
[JsonSerializable(typeof(SocketEditorKind))]
[JsonSerializable(typeof(SocketValue))]
[JsonSerializable(typeof(GraphVariableDto))]
[JsonSerializable(typeof(PluginDependencyDto))]
[JsonSerializable(typeof(GraphEventDto))]
[JsonSerializable(typeof(OverlayDto))]
[JsonSerializable(typeof(CustomEditorHint))]
[JsonSerializable(typeof(RuntimeStorageSnapshot))]
[JsonSerializable(typeof(RuntimeSocketEntry))]
[JsonSerializable(typeof(RuntimeVariableEntry))]
public partial class GraphSerializerContext : JsonSerializerContext
{
}
