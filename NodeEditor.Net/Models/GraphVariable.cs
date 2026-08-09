namespace NodeEditor.Net.Models;

/// <summary>
/// Represents a user-defined graph variable.
/// Variables are graph-scoped named values that can be read/written by Get/Set Variable nodes.
/// Multiple nodes can reference the same variable and stay in sync through the execution context.
/// </summary>
public sealed record class GraphVariable(
    string Id,
    string Name,
    string TypeName,
    SocketValue? DefaultValue = null,
    bool IsReadOnly = false)
{
    /// <summary>
    /// Well-known definition ID prefix for Get Variable nodes.
    /// </summary>
    public const string GetDefinitionPrefix = "variable.get.";

    /// <summary>
    /// Well-known definition ID prefix for Set Variable nodes.
    /// </summary>
    public const string SetDefinitionPrefix = "variable.set.";

    /// <summary>
    /// Gets the node definition ID for the Get Variable node of this variable.
    /// </summary>
    public string GetDefinitionId => GetDefinitionPrefix + Id;

    /// <summary>
    /// Gets the node definition ID for the Set Variable node of this variable.
    /// </summary>
    public string SetDefinitionId => SetDefinitionPrefix + Id;

    /// <summary>
    /// Creates a new variable with a generated ID.
    /// </summary>
    /// <param name="name">The name of the Variable</param>
    /// <param name="typeName">The type of the Variable represents</typeparam>
    /// <param name="defaultValue">The default value</param>
    /// <param name="isReadOnly">Is this variable readonly</param>
    /// <returns></returns>
    [Obsolete("Please use one of the other overrides for the method", false)]
    public static GraphVariable Create(string name, string typeName, SocketValue? defaultValue = null, bool isReadOnly = false)
    {
        return new GraphVariable(Guid.NewGuid().ToString("N"), name, typeName, defaultValue, isReadOnly);
    }

    /// <summary>
    /// Creates a new variable with a generated ID.
    /// </summary>
    /// <typeparam name="T">The type of the Variable represents</typeparam>
    /// <param name="name">The name of the Variable</param>
    /// <param name="defaultValue">The default value</param>
    /// <param name="isReadOnly">Is this variable readonly</param>
    /// <returns></returns>
    public static GraphVariable Create<T>(string name, SocketValue? defaultValue = null, bool isReadOnly = false)
    {
        return new GraphVariable(Guid.NewGuid().ToString("N"), name, typeof(T).FullName ?? "System.Object", defaultValue, isReadOnly);
    }

    /// <summary>
    /// Creates a new variable with a generated ID.
    /// </summary>
    /// <param name="name">The name of the Variable</param>
    /// <param name="type">The type of the Variable represents</typeparam>
    /// <param name="defaultValue">The default value</param>
    /// <param name="isReadOnly">Is this variable readonly</param>
    /// <returns></returns>
    public static GraphVariable Create(string name, Type type, SocketValue? defaultValue = null, bool isReadOnly = false)
    {
        return new GraphVariable(Guid.NewGuid().ToString("N"), name, type.FullName ?? "System.Object", defaultValue, isReadOnly);
    }
}
