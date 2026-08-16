using System.Collections.Concurrent;

namespace NodeEditor.Net.Services.Execution;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="IGraphSharedContext"/>.
/// Type-keyed and named items live in separate maps so a user key cannot collide with a type name.
/// </summary>
public sealed class GraphSharedContext : IGraphSharedContext
{
    // ConcurrentDictionary does not allow null values, so wrap.
    private readonly record struct Entry(object? Value);

    private readonly ConcurrentDictionary<Type, Entry> _byType = new();
    private readonly ConcurrentDictionary<string, Entry> _byName = new(StringComparer.Ordinal);

    public void Set<T>(T value)
    {
        _byType[typeof(T)] = new Entry(value);
    }

    public T Get<T>()
    {
        if (TryGet<T>(out var value))
            return value!;

        throw new InvalidOperationException(
            $"Shared context does not contain an item of type '{typeof(T).FullName}'.");
    }

    public bool TryGet<T>(out T? value)
    {
        if (_byType.TryGetValue(typeof(T), out var entry) && TryCast(entry.Value, out value))
            return true;

        value = default;
        return false;
    }

    public bool Remove<T>() => _byType.TryRemove(typeof(T), out _);

    public bool Contains<T>() => _byType.ContainsKey(typeof(T));

    public void Set<T>(string key, T value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _byName[key] = new Entry(value);
    }

    public T Get<T>(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (TryGet<T>(key, out var value))
            return value!;

        throw new InvalidOperationException(
            $"Shared context does not contain an item named '{key}' of type '{typeof(T).FullName}'.");
    }

    public bool TryGet<T>(string key, out T? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (_byName.TryGetValue(key, out var entry) && TryCast(entry.Value, out value))
            return true;

        value = default;
        return false;
    }

    public bool Remove(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return _byName.TryRemove(key, out _);
    }

    public bool Contains(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return _byName.ContainsKey(key);
    }

    private static bool TryCast<T>(object? stored, out T? value)
    {
        if (stored is T typed)
        {
            value = typed;
            return true;
        }

        if (stored is null)
        {
            value = default;
            return true;
        }

        value = default;
        return false;
    }
}
