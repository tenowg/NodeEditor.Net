using Microsoft.AspNetCore.Components;
using NodeEditor.Blazor.Components.Editors;
using NodeEditor.Net.Models;
using NodeEditor.Net.Records;
using System;
using System.Collections.Generic;
using System.Text;

namespace NodeEditor.Blazor.Services.Editors
{
    internal class ErrorNodeEditorDefinition : INodeCustomEditor
    {
        public bool CanEdit(SocketData socket)
        {
            return socket.CustomEditor == CustomEditorHint.Error();
        }

        public RenderFragment Render(SocketEditorContext context)
            => builder =>
            {
                builder.OpenComponent<ErrorEditor>(0);
                builder.AddAttribute(1, nameof(ErrorEditor.Context), context);
                builder.CloseComponent();
            };
    }
}
