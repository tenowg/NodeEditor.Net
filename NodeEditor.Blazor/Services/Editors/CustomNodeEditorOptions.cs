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
        public string? test { get; set; } = test;
    }

    //[NodeEditorHint("Error", typeof(ErrorOptions))]
    //public record ErrorOptions
    //{
    //    public string Message { get; set; } = "Error";

    //    // 1. Explicitly override the strongly-typed virtual Equals
    //    public virtual bool Equals(ErrorOptions? other)
    //    {
    //        if (other is null) return false;
    //        if (ReferenceEquals(this, other)) return true;

    //        // Custom rule: Users are equal if they share the same ID
    //        return true;
    //    }

    //    // 2. Always override GetHashCode when overriding Equals
    //    public override int GetHashCode()
    //    {
    //        return this.GetHashCode();
    //    }
    //}

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
