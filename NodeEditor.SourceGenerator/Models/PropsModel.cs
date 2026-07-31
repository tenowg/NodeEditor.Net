using System;
using System.Collections.Generic;
using System.Text;

namespace NodeEditor.SourceGenerator.Models
{
    internal sealed record PropsModel
    {
        public string RootNamespace { get; set; } = string.Empty;
        public string AssemblyName { get; set; } = string.Empty;
    }
}
