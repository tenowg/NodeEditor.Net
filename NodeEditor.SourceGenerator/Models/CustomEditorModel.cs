using System;
using System.Collections.Generic;
using System.Text;

namespace NodeEditor.SourceGenerator.Models
{
    internal sealed record CustomEditorModel
    {
        public string HintTypeName { get; init; } = string.Empty;
    }
}
