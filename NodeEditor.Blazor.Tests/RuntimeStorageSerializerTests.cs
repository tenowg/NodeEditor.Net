using System.Reflection;
using System.Text.Json;
using NodeEditor.Net.Models;
using NodeEditor.Net.Services;
using NodeEditor.Net.Services.Execution;
using NodeEditor.Net.Services.Infrastructure;
using NodeEditor.Net.Services.Registry;
using NodeEditor.Net.Services.Serialization;

namespace NodeEditor.Blazor.Tests;

public sealed class RuntimeStorageSerializerTests
{
    [Fact]
    public void EmptyStorage_RoundTrips()
    {
        var serializer = CreateSerializer();
        var source = new NodeRuntimeStorage();

        var snapshot = serializer.ExportRuntimeStorage(source);
        var json = serializer.SerializeRuntimeStorage(snapshot);
        var rehydrated = serializer.DeserializeRuntimeStorage(json);

        var target = new NodeRuntimeStorage();
        serializer.ImportRuntimeStorage(target, rehydrated);

        Assert.Empty(target.GetExecutedNodeIds());
        Assert.Empty(target.GetSocketEntries());
        Assert.Empty(target.GetVariableEntries());
    }

    [Fact]
    public void ExportImport_RestoresExecutedSocketsAndVariables()
    {
        var serializer = CreateSerializer();
        var source = new NodeRuntimeStorage();
        source.MarkNodeExecuted("add");
        source.MarkNodeExecuted("print");
        source.SetSocketValue("add", "Result", 7);
        source.SetSocketValue("print", "Value", "hello");
        source.SetVariable("count", 3);
        source.SetVariable("title", "story");

        var snapshot = serializer.ExportRuntimeStorage(source);
        var json = serializer.SerializeRuntimeStorage(snapshot);
        var rehydrated = serializer.DeserializeRuntimeStorage(json);

        var target = new NodeRuntimeStorage();
        serializer.ImportRuntimeStorage(target, rehydrated);

        Assert.True(target.IsNodeExecuted("add"));
        Assert.True(target.IsNodeExecuted("print"));
        Assert.Equal(7, Convert.ToInt32(target.GetSocketValue("add", "Result")));
        Assert.Equal("hello", target.GetSocketValue("print", "Value"));
        Assert.Equal(3, Convert.ToInt32(target.GetVariable("count")));
        Assert.Equal("story", target.GetVariable("title"));
    }

    [Fact]
    public async Task RestoredStorage_SkipsNonCallableNodes()
    {
        var executions = 0;
        var service = CreateService(out var registry);

        var countDefinition = NodeBuilder.Create("CountOnce")
            .Category("Test")
            .Output<int>("Result")
            .OnExecute((ctx, _) =>
            {
                executions++;
                ctx.SetOutput("Result", executions);
                return Task.CompletedTask;
            })
            .Build();
        registry.RegisterDefinitions(new[] { countDefinition });

        var start = NodeFromDef(registry, "Start", "start");
        var count = countDefinition.Factory() with { Id = "count" };
        var consume = NodeFromDef(registry, "Consume", "consume");

        var nodes = new List<NodeData> { start, count, consume };
        var connections = new List<ConnectionData>
        {
            TestConnections.Exec("start", "Exit", "consume", "Enter"),
            TestConnections.Data("count", "Result", "consume", "Value")
        };

        var source = new NodeRuntimeStorage();
        await service.ExecuteAsync(nodes, connections, source, null!, NodeExecutionOptions.Default, CancellationToken.None);

        Assert.Equal(1, executions);
        Assert.True(source.IsNodeExecuted("count"));
        Assert.Equal(1, Convert.ToInt32(source.GetSocketValue("count", "Result")));

        var serializer = CreateSerializer();
        var snapshot = serializer.ExportRuntimeStorage(source);
        var target = new NodeRuntimeStorage();
        serializer.ImportRuntimeStorage(target, snapshot);

        await service.ExecuteAsync(nodes, connections, target, null!, NodeExecutionOptions.Default, CancellationToken.None);

        Assert.Equal(1, executions);
        Assert.True(target.IsNodeExecuted("count"));
        Assert.Equal(1, Convert.ToInt32(target.GetSocketValue("count", "Result")));
    }

    [Fact]
    public void Import_DoesNotRestoreSharedContext()
    {
        var serializer = CreateSerializer();
        var source = new NodeRuntimeStorage();
        source.Shared.Set("userId", "alice");
        source.Shared.Set(99);
        source.MarkNodeExecuted("n1");

        var target = new NodeRuntimeStorage();
        serializer.ImportRuntimeStorage(target, serializer.ExportRuntimeStorage(source));

        Assert.True(target.IsNodeExecuted("n1"));
        Assert.False(target.Shared.Contains("userId"));
        Assert.False(target.Shared.Contains<int>());
    }

    [Fact]
    public void Export_SkipsUnserializableValues_WithWarning()
    {
        var serializer = CreateSerializer();
        var source = new NodeRuntimeStorage();
        source.MarkNodeExecuted("keep");
        source.SetSocketValue("keep", "Result", 1);
        source.SetSocketValue("bad", "Fn", (Func<int>)(() => 1));
        source.SetVariable("ok", "yes");
        source.SetVariable("badVar", (Func<string>)(() => "no"));

        var warnings = new List<string>();
        var snapshot = serializer.ExportRuntimeStorage(source, warnings);
        var json = serializer.SerializeRuntimeStorage(snapshot);

        Assert.Contains(warnings, w => w.Contains("bad::Fn", StringComparison.Ordinal));
        Assert.Contains(warnings, w => w.Contains("badVar", StringComparison.Ordinal));
        Assert.DoesNotContain(snapshot.Sockets, s => s.NodeId == "bad");
        Assert.DoesNotContain(snapshot.Variables, v => v.Key == "badVar");

        var target = new NodeRuntimeStorage();
        serializer.ImportRuntimeStorage(target, serializer.DeserializeRuntimeStorage(json));

        Assert.True(target.IsNodeExecuted("keep"));
        Assert.Equal(1, Convert.ToInt32(target.GetSocketValue("keep", "Result")));
        Assert.Equal("yes", target.GetVariable("ok"));
        Assert.False(target.TryGetSocketValue("bad", "Fn", out _));
        Assert.Null(target.GetVariable("badVar"));
    }

    [Fact]
    public void SerializeGraphData_DoesNotIncludeRuntimePayload()
    {
        var serializer = CreateSerializer();
        var graphData = new GraphData(
            Array.Empty<GraphNodeData>(),
            Array.Empty<ConnectionData>(),
            Array.Empty<GraphVariable>(),
            SchemaVersion: GraphSerializer.CurrentVersion);

        var json = serializer.SerializeGraphData(graphData);

        Assert.DoesNotContain("executedNodeIds", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ExecutedNodeIds", json, StringComparison.Ordinal);
    }

    private static GraphSerializer CreateSerializer()
    {
        var registry = new NodeRegistryService(new NodeDiscoveryService());
        registry.EnsureInitialized(Array.Empty<Assembly>());
        var resolver = new SocketTypeResolver();
        var validator = new ConnectionValidator(resolver);
        var migrator = new GraphSchemaMigrator();
        return new GraphSerializer(registry, validator, migrator, new JsonSerializerOptions());
    }

    private static NodeExecutionService CreateService(out NodeRegistryService registry)
    {
        registry = new NodeRegistryService(new NodeDiscoveryService());
        registry.EnsureInitialized();
        return new NodeExecutionService(new ExecutionPlanner(), registry, new MinimalServiceProvider());
    }

    private static NodeData NodeFromDef(NodeRegistryService registry, string defName, string id)
    {
        var def = registry.Definitions.First(d => d.Name == defName && (d.NodeType is not null || d.InlineExecutor is not null));
        return def.Factory() with { Id = id };
    }
}
