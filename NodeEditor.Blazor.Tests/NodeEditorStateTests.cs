using NodeEditor.Net.Models;
using NodeEditor.Net.Services;
using NodeEditor.Net.ViewModels;

namespace NodeEditor.Blazor.Tests;

public sealed class NodeEditorStateTests
{
    [Fact]
    public void NodeEditorState_StartsEmpty()
    {
        var state = new NodeEditorState();

        Assert.Empty(state.Nodes);
        Assert.Empty(state.Connections);
        Assert.Empty(state.SelectedNodeIds);
        Assert.Null(state.SelectedConnection);
    }

    [Fact]
    public void NodeEditorState_DefaultViewport_IsEmptyRect()
    {
        var state = new NodeEditorState();

        Assert.Equal(new Rect2D(0, 0, 0, 0), state.Viewport);
    }

    [Fact]
    public void RemoveConnectionsToInput_RemovesMatchingConnections()
    {
        var state = new NodeEditorState();

        var c1 = new ConnectionData("A", "B", "Out", "In1", false);
        var c2 = new ConnectionData("A", "B", "Out", "In2", false);

        state.AddConnection(c1);
        state.AddConnection(c2);

        state.RemoveConnectionsToInput("B", "In1");

        Assert.DoesNotContain(c1, state.Connections);
        Assert.Contains(c2, state.Connections);
    }

    [Fact]
    public void AddConnection_ToDynamicEmptySlot_AddsTrailingSocket()
    {
        var state = new NodeEditorState();
        var itemType = typeof(object).FullName!;
        var target = new NodeViewModel(new NodeData(
            "target",
            "List Create",
            false,
            false,
            false,
            new[] { DynamicSocketGroup.CreateItemSocket("Items", 0, itemType) },
            new[] { new SocketData("Result", typeof(SerializableList).FullName!, false, false) }));
        var source = new NodeViewModel(new NodeData(
            "source",
            "Const",
            false,
            false,
            false,
            Array.Empty<SocketData>(),
            new[] { new SocketData("Out", typeof(string).FullName!, false, false) }));

        state.AddNode(source);
        state.AddNode(target);
        state.AddConnection(new ConnectionData("source", "target", "Out", "Items[0]", false));

        Assert.Equal(2, target.Inputs.Count);
        Assert.Equal("Items[0]", target.Inputs[0].Data.Name);
        Assert.Equal("Items[1]", target.Inputs[1].Data.Name);
        Assert.Equal(1, target.InputsVersion);
    }

    [Fact]
    public void RemoveConnection_FromDynamicSlot_CompactsAndRemaps()
    {
        var state = new NodeEditorState();
        var itemType = typeof(object).FullName!;
        var target = new NodeViewModel(new NodeData(
            "target",
            "List Create",
            false,
            false,
            false,
            new[] { DynamicSocketGroup.CreateItemSocket("Items", 0, itemType) },
            Array.Empty<SocketData>()));
        var sourceA = CreateConst("a");
        var sourceB = CreateConst("b");

        state.AddNode(sourceA);
        state.AddNode(sourceB);
        state.AddNode(target);
        state.AddConnection(new ConnectionData("a", "target", "Out", "Items[0]", false));
        state.AddConnection(new ConnectionData("b", "target", "Out", "Items[1]", false));

        Assert.Equal(3, target.Inputs.Count);

        var first = state.Connections.First(c => c.InputSocketName == "Items[0]");
        state.RemoveConnection(first);

        Assert.Equal(2, target.Inputs.Count);
        Assert.Equal("Items[0]", target.Inputs[0].Data.Name);
        Assert.Equal("Items[1]", target.Inputs[1].Data.Name);
        Assert.Single(state.Connections);
        Assert.Equal("Items[0]", state.Connections[0].InputSocketName);
        Assert.Equal("b", state.Connections[0].OutputNodeId);
    }

    [Fact]
    public void SetSocketValue_OnEmptyDynamicSlot_AddsTrailingSocket()
    {
        var state = new NodeEditorState();
        var target = new NodeViewModel(new NodeData(
            "target",
            "List Create",
            false,
            false,
            false,
            new[] { DynamicSocketGroup.CreateItemSocket("Items", 0, typeof(object).FullName!) },
            Array.Empty<SocketData>()));
        state.AddNode(target);

        state.SetSocketValue("target", "Items[0]", "hello");

        Assert.Equal(2, target.Inputs.Count);
        Assert.Equal("hello", target.Inputs[0].Data.Value?.ToObject<string>());
        Assert.Equal("Items[1]", target.Inputs[1].Data.Name);
    }

    [Fact]
    public void LoadFromGraphData_SeedsMissingDynamicGroupFromPersistedSockets()
    {
        var state = new NodeEditorState();
        var graph = new GraphData(
            new[]
            {
                new GraphNodeData(
                    new NodeData(
                        "create",
                        "List Create",
                        false,
                        false,
                        false,
                        new[]
                        {
                            DynamicSocketGroup.CreateItemSocket("Items", 0, typeof(object).FullName!) with
                            {
                                Value = SocketValue.FromObject("only")
                            }
                        },
                        new[] { new SocketData("Result", typeof(SerializableList).FullName!, false, false) },
                        "List Create"),
                    Point2D.Zero,
                    new Size2D(180, 60))
            },
            Array.Empty<ConnectionData>(),
            Array.Empty<GraphVariable>(),
            Array.Empty<GraphEvent>(),
            Array.Empty<OverlayData>());

        state.LoadFromGraphData(graph);

        var node = Assert.Single(state.Nodes);
        Assert.Equal(2, node.Inputs.Count);
        Assert.Equal("only", node.Inputs[0].Data.Value?.ToObject<string>());
        Assert.Equal("Items[1]", node.Inputs[1].Data.Name);
    }

    private static NodeViewModel CreateConst(string id) =>
        new(new NodeData(
            id,
            "Const",
            false,
            false,
            false,
            Array.Empty<SocketData>(),
            new[] { new SocketData("Out", typeof(string).FullName!, false, false) }));
}
