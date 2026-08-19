using NodeEditor.Net.Models;
using System.Collections.ObjectModel;
using System.Linq;

namespace NodeEditor.Net.ViewModels;

public sealed class NodeViewModel : ViewModelBase
{
    private Point2D _position;
    private Size2D _size;
    private bool _isSelected;
    private bool _isExecuting;
    private bool _isError;

    private readonly List<SocketViewModel> _inputs;

    public NodeViewModel(NodeData data)
    {
        Data = data;
        _size = new Size2D(180, 60);
        _inputs = (data.Inputs ?? Array.Empty<SocketData>())
            .Select(socket => new SocketViewModel(socket))
            .ToList();
        Outputs = new ReadOnlyCollection<SocketViewModel>((data.Outputs ?? Array.Empty<SocketData>())
            .Select(socket => new SocketViewModel(socket))
            .ToList());
    }

    public NodeData Data { get; }

    public IReadOnlyList<SocketViewModel> Inputs => _inputs;

    public IReadOnlyList<SocketViewModel> Outputs { get; }

    public int InputsVersion { get; private set; }

    public void ReplaceInputs(
        IReadOnlyList<SocketData> inputs,
        IReadOnlyDictionary<string, string>? oldToNewNames = null)
    {
        var byOldName = _inputs.ToDictionary(socket => socket.Data.Name, StringComparer.Ordinal);
        var used = new HashSet<SocketViewModel>();
        var next = new List<SocketViewModel>(inputs.Count);

        Dictionary<string, string>? newToOld = null;
        if (oldToNewNames is { Count: > 0 })
        {
            newToOld = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var pair in oldToNewNames)
            {
                newToOld[pair.Value] = pair.Key;
            }
        }

        foreach (var socketData in inputs)
        {
            SocketViewModel? viewModel = null;
            if (byOldName.TryGetValue(socketData.Name, out var sameName))
            {
                viewModel = sameName;
            }
            else if (newToOld is not null &&
                     newToOld.TryGetValue(socketData.Name, out var oldName) &&
                     byOldName.TryGetValue(oldName, out var remapped))
            {
                viewModel = remapped;
            }

            if (viewModel is not null && used.Add(viewModel))
            {
                viewModel.ReplaceData(socketData);
                next.Add(viewModel);
            }
            else
            {
                next.Add(new SocketViewModel(socketData));
            }
        }

        _inputs.Clear();
        _inputs.AddRange(next);
        InputsVersion++;
        OnInputsChanged();
    }

    private void OnInputsChanged()
    {
        RaisePropertyChanged(nameof(Inputs));
        RaisePropertyChanged(nameof(InputsVersion));
    }

    public Point2D Position
    {
        get => _position;
        set => SetProperty(ref _position, value);
    }

    public Size2D Size
    {
        get => _size;
        set => SetProperty(ref _size, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public bool IsExecuting
    {
        get => _isExecuting;
        set => SetProperty(ref _isExecuting, value);
    }

    public bool IsError
    {
        get => _isError;
        set => SetProperty(ref _isError, value);
    }
}
