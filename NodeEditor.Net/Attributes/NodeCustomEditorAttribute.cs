using System;
using System.Collections.Generic;
using System.Text;

namespace NodeEditor.Net.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class NodeCustomEditorAttribute(string HintType) : Attribute
    {
        public string HintType { get; set; } = HintType;
    }
}
