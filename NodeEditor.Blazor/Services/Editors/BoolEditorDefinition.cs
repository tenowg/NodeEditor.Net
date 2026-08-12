using Microsoft.AspNetCore.Components;
using NodeEditor.Blazor.Components.Editors;
using NodeEditor.Net.Attributes;
using NodeEditor.Net.Models;
using NodeEditor.Net.Records;

namespace NodeEditor.Blazor.Services.Editors;

public sealed class BoolEditorDefinition : INodeCustomEditor
{
    public bool CanEdit(SocketData socket)
    {
        if (socket.IsExecution || !socket.IsInput)
        {
            return false;
        }

        //var test = CustomEditorHint.Bool();
        var customEditorHint = socket.CustomEditor?.EditorHint;
        if (customEditorHint is not null && customEditorHint == CustomEditorHint.Bool().EditorHint)
        {
            return false;
        }

        var hint = socket.EditorHint?.Kind;
        if (hint is not null && hint != SocketEditorKind.Bool)
        {
            return false;
        }
        
        var typeName = socket.TypeName ?? string.Empty;
        return typeName.Equals("bool", StringComparison.OrdinalIgnoreCase)
               || typeName.Equals("boolean", StringComparison.OrdinalIgnoreCase)
               || typeName.Equals(typeof(bool).FullName, StringComparison.OrdinalIgnoreCase)
               || typeName.Equals("System.Boolean", StringComparison.OrdinalIgnoreCase);
    }

    public RenderFragment Render(SocketEditorContext context)
        => builder =>
        {
            builder.OpenComponent<BoolEditor>(0);
            builder.AddAttribute(1, nameof(BoolEditor.Context), context);
            builder.CloseComponent();
        };
}
