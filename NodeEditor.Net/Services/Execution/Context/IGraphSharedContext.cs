namespace NodeEditor.Net.Services.Execution;

/// <summary>
/// Generic, typed bag of objects shared by every node for the lifetime of a graph execution.
/// Hosts seed it before <c>ExecuteAsync</c>; nodes read and replace items without wiring sockets.
/// Not serialized — holds live runtime objects only.
/// </summary>
public interface IGraphSharedContext
{
    /// <summary>Stores or replaces the type-keyed slot for <typeparamref name="T"/>.</summary>
    void Set<T>(T value);

    /// <summary>Returns the type-keyed item. Throws if the slot is missing or the stored value is the wrong type.</summary>
    T Get<T>();

    /// <summary>Tries to read the type-keyed slot. Returns false if missing or the stored value is the wrong type.</summary>
    bool TryGet<T>(out T? value);

    /// <summary>Removes the type-keyed slot. Returns true if it existed.</summary>
    bool Remove<T>();

    /// <summary>Returns true if a type-keyed slot for <typeparamref name="T"/> exists (including a stored null).</summary>
    bool Contains<T>();

    /// <summary>Stores or replaces a named slot. Named keys never collide with type-keyed slots.</summary>
    void Set<T>(string key, T value);

    /// <summary>Returns the named item. Throws if the key is missing or the stored value is the wrong type.</summary>
    T Get<T>(string key);

    /// <summary>Tries to read a named slot. Returns false if missing or the stored value is the wrong type.</summary>
    bool TryGet<T>(string key, out T? value);

    /// <summary>Removes a named slot. Returns true if it existed.</summary>
    bool Remove(string key);

    /// <summary>Returns true if a named slot exists (including a stored null).</summary>
    bool Contains(string key);
}
