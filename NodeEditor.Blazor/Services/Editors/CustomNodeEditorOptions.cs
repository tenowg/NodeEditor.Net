using NodeEditor.Net.Attributes;
using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Text;
using System.Text.Json.Serialization;

namespace NodeEditor.Blazor.Services.Editors
{
    [NodeEditorHint("Bool", typeof(BoolOption))]
    public record BoolOption(string? test = null) {
        public string? test = test;
    }

    [NodeEditorHint("Button", typeof(ButtonOptions))]
    public record ButtonOptions { }

    [NodeEditorHint("Dropdown", typeof(DropdownOptions))]
    public record DropdownOptions(List<string>? options = null)
    {
        public List<string>? options { get; init; } = options;
    }

    [NodeEditorHint("Image", typeof(ImageOptions))]
    public record ImageOptions { }

    //[JsonSourceGenerationOptions(
    //PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    //WriteIndented = true)]
    //[JsonSerializable(typeof(DropdownOptions))]
    //[JsonSerializable(typeof(BoolOption))]
    //[JsonSerializable(typeof(ButtonOptions))]
    //[JsonSerializable(typeof(ImageOptions))]
    //public partial class NodeEditorNetBlazerOptionsContext : JsonSerializerContext { }
}
