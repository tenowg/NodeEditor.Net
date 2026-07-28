using System.Reflection;
using NodeEditor.Net.Services;
using NodeEditor.Net.Services.Execution;
using NodeEditor.Net.Services.Execution.StandardNodes;

namespace NodeEditor.Net.Services.Registry;

public class NodeRegistryService : INodeRegistryService
{
    private readonly NodeDiscoveryService _discovery;
    private readonly List<NodeDefinition> _definitions = [];
    private readonly object _lock = new();
    private bool _initialized;

    public NodeRegistryService(NodeDiscoveryService discovery)
    {
        _discovery = discovery;
    }

    public event EventHandler? RegistryChanged;

    public IReadOnlyList<NodeDefinition> Definitions
    {
        get
        {
            EnsureInitialized();
            return _definitions;
        }
    }

    public void EnsureInitialized(IEnumerable<Assembly>? assemblies = null)
    {
        lock (_lock)
        {
            if (_initialized)
            {
                return;
            }

            var scanAssemblies = assemblies?.ToArray() ?? AppDomain.CurrentDomain.GetAssemblies();
            var discovered = _discovery.DiscoverFromAssemblies(scanAssemblies);
            MergeDefinitions(discovered);
            RegisterDefinitions(StandardNodeRegistration.GetInlineDefinitions());
            _initialized = true;
        }
    }

    public void RegisterFromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var discovered = _discovery.DiscoverFromAssemblies([assembly]);
        RegisterDefinitions(discovered);
    }

    public void RegisterPluginAssembly(Assembly assembly)
    {
        PlatformGuard.ThrowIfPluginLoadingUnsupported();
        RegisterFromAssembly(assembly);
    }

    public void RegisterDefinitions(IEnumerable<NodeDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        MergeDefinitions(definitions);
        _initialized = true;
    }

    public int RemoveDefinitions(IEnumerable<NodeDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        var ids = new HashSet<string>(definitions.Select(d => d.Id), StringComparer.Ordinal);
        if (ids.Count == 0)
        {
            return 0;
        }

        var removed = 0;
        lock (_lock)
        {
            for (var i = _definitions.Count - 1; i >= 0; i--)
            {
                if (!ids.Contains(_definitions[i].Id))
                {
                    continue;
                }

                _definitions.RemoveAt(i);
                removed++;
            }
        }

        if (removed > 0)
        {
            RegistryChanged?.Invoke(this, EventArgs.Empty);
        }

        return removed;
    }

    public int RemoveDefinitionsFromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var discovered = _discovery.DiscoverFromAssemblies([assembly]);
        return RemoveDefinitions(discovered);
    }

    public NodeCatalog GetCatalog(string? search = null)
    {
        var definitions = Definitions;
        if (!string.IsNullOrWhiteSpace(search))
        {
            var query = search.Trim();
            definitions = [.. definitions.Where(def => MatchesQuery(def, query))];
        }

        return NodeCatalog.Create(definitions);
    }

    private void MergeDefinitions(IEnumerable<NodeDefinition> definitions)
    {
        var added = false;
        lock (_lock)
        {
            foreach (var definition in definitions)
            {
                if (_definitions.Any(d => d.Id.Equals(definition.Id, StringComparison.Ordinal)))
                {
                    continue;
                }

                _definitions.Add(definition);
                added = true;
            }
        }

        if (added)
        {
            RegistryChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private static bool MatchesQuery(NodeDefinition definition, string query)
    {
        return definition.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
               || definition.Category.Contains(query, StringComparison.OrdinalIgnoreCase)
               || definition.Description.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    public void RegisterNodeByType<T>() where T : NodeBase, new()
    {
        var definition = _discovery.BuildDefinitionFromType(typeof(T)) ?? throw new ArgumentNullException(nameof(T));
        var definitions = new List<NodeDefinition> { definition };
        RegisterDefinitions(definitions);
    }
}
