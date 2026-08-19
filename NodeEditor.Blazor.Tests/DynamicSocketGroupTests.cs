using NodeEditor.Net.Models;
using NodeEditor.Net.Services;

namespace NodeEditor.Blazor.Tests;

public sealed class DynamicSocketGroupTests
{
    private static readonly string ObjectType = typeof(object).FullName!;

    [Theory]
    [InlineData("Items[0]", "Items", 0)]
    [InlineData("Items[12]", "Items", 12)]
    [InlineData("Characters[3]", "Characters", 3)]
    public void TryParse_ValidNames(string name, string group, int index)
    {
        Assert.True(DynamicSocketGroup.TryParse(name, out var parsedGroup, out var parsedIndex));
        Assert.Equal(group, parsedGroup);
        Assert.Equal(index, parsedIndex);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Items")]
    [InlineData("[0]")]
    [InlineData("Items[]")]
    [InlineData("Items[-1]")]
    public void TryParse_InvalidNames(string? name)
    {
        Assert.False(DynamicSocketGroup.TryParse(name, out _, out _));
    }

    [Fact]
    public void Sync_EmptyGroup_KeepsSingleEmptySocket()
    {
        var inputs = new[] { DynamicSocketGroup.CreateItemSocket("Items", 0, ObjectType) };

        var result = DynamicSocketGroup.Sync(inputs, Array.Empty<ConnectionData>(), "n1");

        Assert.False(result.Changed);
        Assert.Single(result.Inputs);
        Assert.Equal("Items[0]", result.Inputs[0].Name);
        Assert.False(DynamicSocketGroup.HasValue(result.Inputs[0].Value));
    }

    [Fact]
    public void Sync_ConnectingEmptySlot_AddsTrailingSocket()
    {
        var inputs = new[] { DynamicSocketGroup.CreateItemSocket("Items", 0, ObjectType) };
        var connections = new[] { new ConnectionData("src", "n1", "Out", "Items[0]", false) };

        var result = DynamicSocketGroup.Sync(inputs, connections, "n1");

        Assert.True(result.Changed);
        Assert.Equal(2, result.Inputs.Count);
        Assert.Equal("Items[0]", result.Inputs[0].Name);
        Assert.Equal("Items[1]", result.Inputs[1].Name);
        Assert.False(DynamicSocketGroup.HasValue(result.Inputs[1].Value));
    }

    [Fact]
    public void Sync_ValueOnEmptySlot_AddsTrailingSocket()
    {
        var inputs = new[]
        {
            DynamicSocketGroup.CreateItemSocket("Items", 0, ObjectType) with
            {
                Value = SocketValue.FromObject("hello")
            }
        };

        var result = DynamicSocketGroup.Sync(inputs, Array.Empty<ConnectionData>(), "n1");

        Assert.True(result.Changed);
        Assert.Equal(2, result.Inputs.Count);
        Assert.Equal("hello", result.Inputs[0].Value?.ToObject<string>());
        Assert.Equal("Items[1]", result.Inputs[1].Name);
    }

    [Fact]
    public void Sync_ClearingMiddleSlot_CompactsAndRemapsConnections()
    {
        var inputs = new[]
        {
            DynamicSocketGroup.CreateItemSocket("Items", 0, ObjectType) with { Value = SocketValue.FromObject("A") },
            DynamicSocketGroup.CreateItemSocket("Items", 1, ObjectType),
            DynamicSocketGroup.CreateItemSocket("Items", 2, ObjectType)
        };
        var connections = new[] { new ConnectionData("src", "n1", "Out", "Items[2]", false) };

        var result = DynamicSocketGroup.Sync(inputs, connections, "n1");

        Assert.True(result.Changed);
        Assert.Equal(3, result.Inputs.Count);
        Assert.Equal("Items[0]", result.Inputs[0].Name);
        Assert.Equal("A", result.Inputs[0].Value?.ToObject<string>());
        Assert.Equal("Items[1]", result.Inputs[1].Name);
        Assert.Equal("Items[2]", result.Inputs[2].Name);
        Assert.False(DynamicSocketGroup.HasValue(result.Inputs[2].Value));
        Assert.Equal("Items[1]", result.Connections[0].InputSocketName);
        Assert.True(result.RenamedSockets.ContainsKey("Items[2]"));
        Assert.Equal("Items[1]", result.RenamedSockets["Items[2]"]);
    }

    [Fact]
    public void Sync_PreservesNonDynamicSockets()
    {
        var inputs = new[]
        {
            new SocketData("Enter", "exec", true, true),
            DynamicSocketGroup.CreateItemSocket("Items", 0, ObjectType) with { Value = SocketValue.FromObject(1) },
            new SocketData("Note", typeof(string).FullName!, true, false)
        };

        var result = DynamicSocketGroup.Sync(inputs, Array.Empty<ConnectionData>(), "n1");

        Assert.Equal("Enter", result.Inputs[0].Name);
        Assert.Equal("Items[0]", result.Inputs[1].Name);
        Assert.Equal("Items[1]", result.Inputs[2].Name);
        Assert.Equal("Note", result.Inputs[3].Name);
    }

    [Fact]
    public void SeedMissingGroups_InsertsDefinitionSeed()
    {
        var persisted = new[] { new SocketData("Enter", "exec", true, true) };
        var definition = new[]
        {
            new SocketData("Enter", "exec", true, true),
            DynamicSocketGroup.CreateItemSocket("Items", 0, ObjectType)
        };

        var seeded = DynamicSocketGroup.SeedMissingGroups(persisted, definition);

        Assert.Equal(2, seeded.Count);
        Assert.Equal("Enter", seeded[0].Name);
        Assert.Equal("Items[0]", seeded[1].Name);
        Assert.Equal("Items", seeded[1].DynamicGroup);
    }

    [Fact]
    public void AssembleList_SkipsEmptyTrailingSlot()
    {
        var inputs = new[]
        {
            DynamicSocketGroup.CreateItemSocket("Items", 0, ObjectType) with { Value = SocketValue.FromObject("A") },
            DynamicSocketGroup.CreateItemSocket("Items", 1, ObjectType) with { Value = SocketValue.FromObject("B") },
            DynamicSocketGroup.CreateItemSocket("Items", 2, ObjectType)
        };

        var list = DynamicSocketGroup.AssembleList(inputs, "Items", _ => false, _ => null);

        Assert.Equal(2, list.Count);
        Assert.Equal("A", list.Snapshot()[0]);
        Assert.Equal("B", list.Snapshot()[1]);
    }
}
