# Headless Execution

NodeEditor.Net supports running node graphs without any UI. The `HeadlessGraphRunner` service loads graphs from JSON or `GraphData` objects and executes them in any .NET host—console apps, APIs, background services, or unit tests.

## Why Headless Execution?

| Use Case | Description |
|----------|-------------|
| **CI/CD Pipelines** | Run data processing graphs as part of build automation |
| **API Endpoints** | Execute graphs in response to HTTP requests |
| **Background Services** | Schedule graph execution on a timer or queue |
| **Unit Testing** | Validate graph behavior programmatically |
| **MCP Server** | The MCP `execution.run_json` ability uses headless execution |
| **Batch Processing** | Process multiple datasets through the same graph |

## Architecture

```
HeadlessGraphRunner (scoped)
├── GraphSerializer — loads JSON into GraphData
├── NodeExecutionService — executes the plan
├── NodeContextRegistry — resolves node implementations
├── SocketTypeResolver — maps type names to CLR types
└── ExecutionPlanner — builds topological execution order
```

The `HeadlessGraphRunner` bypasses all Blazor components and ViewModels. It works directly with the model layer (`NodeData`, `ConnectionData`, `GraphData`) and the execution layer.

## Usage

### Basic Execution from JSON

```csharp
using NodeEditor.Net.Services.Execution;

// Get the runner from DI
var runner = serviceProvider.GetRequiredService<HeadlessGraphRunner>();

// Load and execute a graph from a JSON file
var json = File.ReadAllText("my-graph.json");
var result = await runner.ExecuteFromJsonAsync(json, cancellationToken);
```

### Execution with Options

```csharp
var options = new NodeExecutionOptions
{
    Mode = ExecutionMode.Parallel,
    MaxDegreeOfParallelism = 8
};

var result = await runner.ExecuteFromJsonAsync(json, options, cancellationToken);
```

### Execution from GraphData

```csharp
var serializer = serviceProvider.GetRequiredService<GraphSerializer>();
var graphData = serializer.Deserialize(json);

var result = await runner.ExecuteAsync(graphData, options, cancellationToken);
```

### Using Custom Node Contexts

If your graph uses nodes defined in custom `INodeContext` classes, register them before execution:

```csharp
// Register custom node contexts
var registry = serviceProvider.GetRequiredService<NodeRegistryService>();
registry.RegisterFromAssembly(typeof(MyCustomNodes).Assembly);

// Execute
var result = await runner.ExecuteFromJsonAsync(json, cancellationToken);
```

## Console App Example

```csharp
using Microsoft.Extensions.DependencyInjection;
using NodeEditor.Blazor.Services;
using NodeEditor.Net.Services.Execution;

var services = new ServiceCollection();
services.AddNodeEditor();
var provider = services.BuildServiceProvider();

using var scope = provider.CreateScope();
var runner = scope.ServiceProvider.GetRequiredService<HeadlessGraphRunner>();

var json = File.ReadAllText(args[0]);
await runner.ExecuteFromJsonAsync(json, CancellationToken.None);

Console.WriteLine("Graph executed successfully.");
```

## Integration with MCP

The MCP server's `execution.run_json` ability uses `HeadlessGraphRunner` internally. When an AI assistant sends a graph as JSON via MCP, it's executed headlessly without affecting the canvas state:

```
MCP Client → execute_ability("execution.run_json", { json: "..." })
    → ExecutionAbilityProvider
    → HeadlessGraphRunner.ExecuteFromJsonAsync()
    → Returns results to MCP client
```

## Variables in Headless Execution

Graph variables are supported in headless execution. The `VariableNodeExecutor` seeds variables from their default values before execution begins, and Get/Set nodes read and write the shared execution context.

## Shared context

Every `INodeRuntimeStorage` exposes a typed object bag on `Shared` (`IGraphSharedContext`). The host seeds it before execution; every node sees the same instance through `INodeExecutionContext.Shared` with no extra sockets.

```csharp
var storage = new NodeRuntimeStorage();
storage.Shared.Set(mySession);
storage.Shared.Set("userId", currentUserId);

await runner.ExecuteAsync(graphData, storage);

// Any node:
var session = context.Shared.Get<MySession>();
context.Shared.Set(updatedSession);
```

This is separate from graph variables and from `IServiceProvider`. Writes replace. Named keys never collide with type-keyed slots.

`Shared` is the **same instance** on group children (`CreateChild`) and parallel layered scopes. It is **not** serialized — it holds live runtime objects.

Reuse the same `INodeRuntimeStorage` for later turns to keep both this bag and the executed-node cache. Non-callable nodes already executed are skipped; callable nodes run again and can read the mutated bag. The visual editor still creates a fresh storage on each Run click.

## Persisting the executed-node cache

In-process reuse is enough when the storage instance stays alive. To resume after a process or login restart, export a **companion snapshot** of the executed-node cache (plus socket values and runtime variables) and store it **alongside** the graph JSON. Default graph export does **not** include this snapshot — editor saves stay reusable templates.

The snapshot does **not** include `Shared`, the event bus, or loop generation state. Re-seed live objects on resume.

```csharp
// turn N ends
var snapshot = serializer.ExportRuntimeStorage(storage);
var snapshotJson = serializer.SerializeRuntimeStorage(snapshot);
// save graph JSON + snapshot JSON in the host database

// later session
var storage = new NodeRuntimeStorage();
storage.Shared.Set(mySession);          // live objects
var snapshot = serializer.DeserializeRuntimeStorage(snapshotJson);
serializer.ImportRuntimeStorage(storage, snapshot, typeResolver);
await runner.ExecuteAsync(graphData, storage);
// non-callable nodes already in the snapshot are skipped
```

Unserializable socket or variable values (delegates, live service objects) are omitted from the snapshot. Those belong in `Shared`. Pass an optional `warnings` collection to `ExportRuntimeStorage` to see what was skipped.

## Service Registration

`HeadlessGraphRunner` is registered as a **scoped** service by `AddNodeEditor()`. In non-Blazor hosts, create a scope for each execution:

```csharp
using var scope = provider.CreateScope();
var runner = scope.ServiceProvider.GetRequiredService<HeadlessGraphRunner>();
await runner.ExecuteFromJsonAsync(json, token);
```

## Namespaces

| Type | Namespace |
|------|-----------|
| `HeadlessGraphRunner` | `NodeEditor.Net.Services.Execution` |
| `NodeExecutionOptions` | `NodeEditor.Net.Services.Execution` |
| `ExecutionMode` | `NodeEditor.Net.Services.Execution` |
| `NodeExecutionContext` | `NodeEditor.Net.Services.Execution` |
| `RuntimeStorageSnapshot` | `NodeEditor.Net.Models` |
| `IGraphSerializer` | `NodeEditor.Net.Services.Serialization` |
