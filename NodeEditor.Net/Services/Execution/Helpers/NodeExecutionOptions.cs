namespace NodeEditor.Net.Services.Execution;

public sealed record NodeExecutionOptions(
    ExecutionMode Mode,
    bool AllowBackground,
    int MaxDegreeOfParallelism,
    object? NodeRegistryKey = null,
    StreamMode StreamMode = StreamMode.Sequential,
    int MaxCallDepth = 512)
{
    public static NodeExecutionOptions Default { get; } = new(
        ExecutionMode.Sequential,
        AllowBackground: false,
        MaxDegreeOfParallelism: Environment.ProcessorCount);
    public static NodeExecutionOptions DefaultWithNodeRegistryKey(object? key) => new(
        ExecutionMode.Sequential,
        AllowBackground: false,
        MaxDegreeOfParallelism: Environment.ProcessorCount, key);
}
