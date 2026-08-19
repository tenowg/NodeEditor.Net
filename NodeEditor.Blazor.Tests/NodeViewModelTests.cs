using NodeEditor.Net.Models;
using NodeEditor.Net.ViewModels;

namespace NodeEditor.Blazor.Tests;

public sealed class NodeViewModelTests
{
    [Fact]
    public void NodeViewModel_BuildsSocketViewModels()
    {
        var data = new NodeData(
            "node-1",
            "Test",
            false,
            false,
            false,
            new[] { new SocketData("In", "System.Int32", true, false) },
            new[] { new SocketData("Out", "System.Int32", false, false) });

        var viewModel = new NodeViewModel(data);

        Assert.Single(viewModel.Inputs);
        Assert.Single(viewModel.Outputs);
        Assert.Equal("In", viewModel.Inputs[0].Data.Name);
        Assert.Equal("Out", viewModel.Outputs[0].Data.Name);
    }

    [Fact]
    public void ReplaceInputs_ReusesViewModelWhenNameUnchanged()
    {
        var original = new SocketData("Items[0]", "System.Object", true, false);
        var viewModel = new NodeViewModel(new NodeData(
            "node-1",
            "List Create",
            false,
            false,
            false,
            new[] { original },
            Array.Empty<SocketData>()));
        var first = viewModel.Inputs[0];

        viewModel.ReplaceInputs(new[]
        {
            original with { Value = SocketValue.FromObject("A") },
            new SocketData("Items[1]", "System.Object", true, false)
        });

        Assert.Equal(2, viewModel.Inputs.Count);
        Assert.Same(first, viewModel.Inputs[0]);
        Assert.Equal("A", viewModel.Inputs[0].Data.Value?.ToObject<string>());
        Assert.Equal(1, viewModel.InputsVersion);
    }
}
