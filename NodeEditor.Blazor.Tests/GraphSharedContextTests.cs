using NodeEditor.Net.Services.Execution;

namespace NodeEditor.Blazor.Tests;

public sealed class GraphSharedContextTests
{
    private sealed record RunState(string Name);

    [Fact]
    public void TypeKeyed_SetGetReplaceRemove()
    {
        var bag = new GraphSharedContext();

        bag.Set(new RunState("first"));
        Assert.True(bag.Contains<RunState>());
        Assert.Equal("first", bag.Get<RunState>().Name);

        bag.Set(new RunState("second"));
        Assert.Equal("second", bag.Get<RunState>().Name);

        Assert.True(bag.Remove<RunState>());
        Assert.False(bag.Contains<RunState>());
        Assert.False(bag.Remove<RunState>());
    }

    [Fact]
    public void TypeKeyed_Get_Missing_Throws()
    {
        var bag = new GraphSharedContext();

        var ex = Assert.Throws<InvalidOperationException>(() => bag.Get<RunState>());
        Assert.Contains(typeof(RunState).FullName!, ex.Message);
    }

    [Fact]
    public void TypeKeyed_TryGet_MissingAndPresent()
    {
        var bag = new GraphSharedContext();

        Assert.False(bag.TryGet<RunState>(out var missing));
        Assert.Null(missing);

        bag.Set(new RunState("ok"));
        Assert.True(bag.TryGet<RunState>(out var found));
        Assert.Equal("ok", found!.Name);
    }

    [Fact]
    public void Named_SetGetReplaceRemove()
    {
        var bag = new GraphSharedContext();

        bag.Set("userId", "alice");
        Assert.True(bag.Contains("userId"));
        Assert.Equal("alice", bag.Get<string>("userId"));

        bag.Set("userId", "bob");
        Assert.Equal("bob", bag.Get<string>("userId"));

        Assert.True(bag.Remove("userId"));
        Assert.False(bag.Contains("userId"));
        Assert.False(bag.Remove("userId"));
    }

    [Fact]
    public void Named_Get_Missing_Throws()
    {
        var bag = new GraphSharedContext();

        var ex = Assert.Throws<InvalidOperationException>(() => bag.Get<string>("missing"));
        Assert.Contains("missing", ex.Message);
    }

    [Fact]
    public void Named_TryGet_WrongType_ReturnsFalse()
    {
        var bag = new GraphSharedContext();
        bag.Set("count", 42);

        Assert.True(bag.Contains("count"));
        Assert.False(bag.TryGet<string>("count", out _));
        Assert.True(bag.TryGet<int>("count", out var value));
        Assert.Equal(42, value);
    }

    [Fact]
    public void NamedKey_DoesNotCollideWithTypeSlot()
    {
        var bag = new GraphSharedContext();
        var typeName = typeof(RunState).FullName!;

        bag.Set(new RunState("typed"));
        bag.Set(typeName, new RunState("named"));

        Assert.Equal("typed", bag.Get<RunState>().Name);
        Assert.Equal("named", bag.Get<RunState>(typeName).Name);

        bag.Remove<RunState>();
        Assert.False(bag.Contains<RunState>());
        Assert.True(bag.Contains(typeName));
        Assert.Equal("named", bag.Get<RunState>(typeName).Name);
    }

    [Fact]
    public void Named_NullOrWhitespaceKey_Throws()
    {
        var bag = new GraphSharedContext();

        Assert.Throws<ArgumentException>(() => bag.Set(" ", "x"));
        Assert.Throws<ArgumentNullException>(() => bag.Set(null!, "x"));
        Assert.Throws<ArgumentException>(() => bag.Contains(""));
    }

    [Fact]
    public void TypeKeyed_NullValue_IsRetrievable()
    {
        var bag = new GraphSharedContext();
        bag.Set<string?>(null);

        Assert.True(bag.Contains<string?>());
        Assert.True(bag.TryGet<string?>(out var value));
        Assert.Null(value);
        Assert.Null(bag.Get<string?>());
    }
}
