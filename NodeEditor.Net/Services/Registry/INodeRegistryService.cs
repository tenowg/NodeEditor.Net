using System.Reflection;
using NodeEditor.Net.Models;
using NodeEditor.Net.Services.Execution;

namespace NodeEditor.Net.Services.Registry;

public interface INodeRegistryService
{
    event EventHandler? RegistryChanged;

    IReadOnlyList<NodeDefinition> Definitions { get; }

    void EnsureInitialized(IEnumerable<Assembly>? assemblies = null);
    void RegisterFromAssembly(Assembly assembly);
    void RegisterPluginAssembly(Assembly assembly);
    void RegisterDefinitions(IEnumerable<NodeDefinition> definitions);
    void RegisterNodeByType<T>() where T : NodeBase, new();
    int RemoveDefinitions(IEnumerable<NodeDefinition> definitions);
    int RemoveDefinitionsFromAssembly(Assembly assembly);
    NodeCatalog GetCatalog(string? search = null);
}
