using NodeEditor.Net.Attributes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace NodeEditor.Blazor.Services.Editors
{
    //[NodeEditorHint("Bool")]

    //[NodeEditorHint("Button")]
    [NodeEditorHint("Dropdown", typeof(DropdownOptions))]
    public record DropdownOptions(List<string> options)
    {
        public List<string> options { get; init; } = options;
    }

    //[NodeEditorHint("Image")]

    [JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true)]
    [JsonSerializable(typeof(DropdownOptions))]
    public partial class NodeEditorNetBlazerOptionsContext : JsonSerializerContext { }
}
