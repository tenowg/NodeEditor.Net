using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace NodeEditor.Net.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class NodeEditorHintAttribute : Attribute
    {
        public string Name { get; }
        public Type? OptionsType { get; }   // the record type the generator will create / the developer points to

        public NodeEditorHintAttribute(string name, Type? optionsType = null)
        {
            Name = name;
            OptionsType = optionsType;
        }
    }

    public interface IOptionRecord { }

    public static class OptionsSerialization
    {
        public static JsonSerializerOptions CreateOptions(params JsonSerializerContext[] contexts)
        {
            var resolvers = contexts
                .Select(c => (IJsonTypeInfoResolver)c)
                .Append(new DefaultJsonTypeInfoResolver()) // fallback if needed
                .ToArray();

            return new JsonSerializerOptions
            {
                TypeInfoResolver = JsonTypeInfoResolver.Combine(resolvers),
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
                // add any other global settings
            };
        }
    }
}
