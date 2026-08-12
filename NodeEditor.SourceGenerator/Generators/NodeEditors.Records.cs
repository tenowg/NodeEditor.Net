using Microsoft.CodeAnalysis;
using NodeEditor.SourceGenerator.Helpers;
using NodeEditor.SourceGenerator.Models;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace NodeEditor.SourceGenerator.Generators
{
    internal partial class NodeEditors
    {
        private void BuildCustomNodeRegistry(SourceProductionContext context, (ImmutableArray<CustomEditorModel?> Left, PropsModel Right) tuple)
        {
            var models = tuple.Left;
            var rootNamespace = tuple.Right;

            if (models.Length == 0) return;

            foreach (var model in models)
            {
                if (model == null) continue;
                if (string.IsNullOrWhiteSpace(model.OptionsTypeName)) continue;

                var sb = $@"
#nullable enable
namespace {model!.ContainingNamespace};
public sealed class CustomEditorHint{model.HintTypeName}OptionsConverter
    : global::System.Text.Json.Serialization.JsonConverter<global::NodeEditor.Net.Records.CustomEditorHint<{model.OptionsTypeName}>>
{{
    public override global::NodeEditor.Net.Records.CustomEditorHint<{model.OptionsTypeName}> Read(
        ref global::System.Text.Json.Utf8JsonReader reader,
        Type typeToConvert,
        global::System.Text.Json.JsonSerializerOptions options)
    {{
        string? editorHint = null;
        {model.OptionsTypeName}? metadata = null;

        using var doc = global::System.Text.Json.JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (root.TryGetProperty(""editorHint"", out var hintProp))
            editorHint = hintProp.GetString();

        if (root.TryGetProperty(""metadata"", out var metaProp))
            metadata = global::System.Text.Json.JsonSerializer.Deserialize<{model.OptionsTypeName}>(metaProp.GetRawText(), options);

        return new global::NodeEditor.Net.Records.CustomEditorHint<{model.OptionsTypeName}>(editorHint!, metadata!);
    }}

    public override void Write(
        global::System.Text.Json.Utf8JsonWriter writer,
        global::NodeEditor.Net.Records.CustomEditorHint<{model.OptionsTypeName}> value,
        global::System.Text.Json.JsonSerializerOptions options)
    {{
        writer.WriteStartObject();
        writer.WriteString(""editorHint"", value.EditorHint);
        writer.WritePropertyName(""metadata"");
        global::System.Text.Json.JsonSerializer.Serialize(writer, value.Metadata, options);
        writer.WriteEndObject();
    }}
}}
";
                context.AddSource($"{rootNamespace.RootNamespace.Replace(".", "_")}_{model.HintTypeName}NodeEditors.g.cs", sb);
            }

            if (models.Where(x => x is not null).Any(x => !string.IsNullOrWhiteSpace(x!.HintTypeName)))
            {
                var initializer = new IndentedStringBuilder();
                initializer.AppendLine("#nullable enable");
                initializer.AppendLine($"public static class {rootNamespace.RootNamespace.Replace(".", "")}CustomEditorHintGeneratedRegistration");
                using (initializer.Block())
                {
                    initializer.AppendLine("[global::System.Runtime.CompilerServices.ModuleInitializer]");
                    initializer.AppendLine("public static void Register()");
                    using (initializer.Block())
                    {
                        foreach (var model in models)
                        {
                            if (string.IsNullOrWhiteSpace(model!.OptionsTypeName)) { continue; }

                            initializer.AppendLine("global::NodeEditor.Net.Services.Serialization.CustomEditorHintRegistry.Register(");
                            initializer.AppendLine($"    \"{model.HintTypeName}\",");
                            initializer.AppendLine($"new global::{model.ContainingNamespace}.CustomEditorHint{model.HintTypeName}OptionsConverter());");
                        }
                    }


                    initializer.AppendLine("extension(global::NodeEditor.Net.Records.CustomEditorHint hint)");
                    using (initializer.Block())
                    {
                        foreach (var model in models)
                        {
                            if (model == null) continue; 
                            initializer.AppendLine($"public static global::NodeEditor.Net.Records.CustomEditorHint<{model.OptionsTypeName}> {model.HintTypeName}({model.OptionsTypeName}? defaultValue = null) => new global::NodeEditor.Net.Records.CustomEditorHint<{model.OptionsTypeName}>(\"Bool\", defaultValue ?? new {model.OptionsTypeName}());");
                        }
                    }
                }

                context.AddSource($"{rootNamespace.RootNamespace.Replace(".", "_")}_{rootNamespace.RootNamespace.Replace(".", "")}NodeEditorsRegister.g.cs", initializer.ToString());
            }
        }
    }
}
