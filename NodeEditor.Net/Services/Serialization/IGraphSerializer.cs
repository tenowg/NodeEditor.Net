using NodeEditor.Net.Models;
using NodeEditor.Net.Services;
using NodeEditor.Net.Services.Execution;

namespace NodeEditor.Net.Services.Serialization;

public interface IGraphSerializer
{
    GraphData ExportToGraphData(INodeEditorState state);
    void Import(INodeEditorState state, GraphData graphData);

    GraphDto Export(INodeEditorState state);
    GraphImportResult Import(INodeEditorState state, GraphDto dto);

    string SerializeGraphData(GraphData graphData);
    GraphData DeserializeToGraphData(string json);

    string Serialize(GraphDto dto);
    GraphDto Deserialize(string json);

    RuntimeStorageSnapshot ExportRuntimeStorage(INodeRuntimeStorage storage, ICollection<string>? warnings = null);
    void ImportRuntimeStorage(INodeRuntimeStorage storage, RuntimeStorageSnapshot snapshot, ISocketTypeResolver? typeResolver = null);

    string SerializeRuntimeStorage(RuntimeStorageSnapshot snapshot);
    RuntimeStorageSnapshot DeserializeRuntimeStorage(string json);
}
