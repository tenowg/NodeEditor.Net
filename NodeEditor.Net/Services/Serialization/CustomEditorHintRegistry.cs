using NodeEditor.Net.Records;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace NodeEditor.Net.Services.Serialization
{
    public static class CustomEditorHintRegistry
    {
        private static readonly Dictionary<string, Func<JsonElement, JsonSerializerOptions, CustomEditorHint>> Builders = new();
        private static readonly List<JsonConverter> AdditionalConverters = [];
        private static readonly List<IJsonTypeInfoResolver> AdditionalResolvers = new();

        public static CustomEditorHint Deserialize(
            JsonElement json,
            JsonSerializerOptions options)
        {
            if (!json.TryGetProperty("editorHint", out var hintProperty))
            {
                throw new JsonException(
                    "Custom editor hint JSON does not contain 'editorHint'.");
            }

            var editorHint = hintProperty.GetString();

            if (string.IsNullOrWhiteSpace(editorHint))
            {
                throw new JsonException(
                    "Custom editor hint contains an empty 'editorHint'.");
            }

            if (!Builders.TryGetValue(editorHint, out var builder))
            {
                throw new JsonException(
                    $"No metadata type has been registered for editor hint '{editorHint}'.");
            }

            return builder(json, options);
        }

        public static void Register(IJsonTypeInfoResolver resolver)
        {
            ArgumentNullException.ThrowIfNull(resolver);

            AdditionalResolvers.Add(resolver);
        }

        public static void Register<TMeta>(
            string editorHint,
            JsonConverter<CustomEditorHint<TMeta>> converter)
        {
            // store for the base-type dispatcher converter
            Builders[editorHint] = (json, opts) =>
                json.Deserialize<CustomEditorHint<TMeta>>(opts)
                ?? throw new JsonException($"Could not deserialize '{editorHint}'.");

            AdditionalConverters.Add(converter);
        }

        public static JsonSerializerOptions CreateOptions()
        {
            var resolvers = new List<IJsonTypeInfoResolver>
            {
                GraphSerializerContext.Default
            };
            resolvers.AddRange(AdditionalResolvers);

            if (JsonSerializer.IsReflectionEnabledByDefault)
            {
                resolvers.Add(new DefaultJsonTypeInfoResolver());
            }

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                TypeInfoResolver = JsonTypeInfoResolver.Combine(
                    resolvers.ToArray()
                )
            };

            foreach (var converter in AdditionalConverters)
                options.Converters.Add(converter);

            options.Converters.Add(new CustomEditorHintJsonConverter()); // base-type dispatcher

            return options;
        }
    }


}
