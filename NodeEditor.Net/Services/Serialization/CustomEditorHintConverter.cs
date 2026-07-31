using NodeEditor.Net.Records;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;    

namespace NodeEditor.Net.Services.Serialization
{

    public sealed class CustomEditorHintJsonConverter
        : JsonConverter<CustomEditorHint>
    {
        public override CustomEditorHint Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            using var document = JsonDocument.ParseValue(ref reader);

            return CustomEditorHintRegistry.Deserialize(
                document.RootElement,
                options);
        }

        public override void Write(
            Utf8JsonWriter writer,
            CustomEditorHint value,
            JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            var runtimeType = value.GetType();

            JsonSerializer.Serialize(
                writer,
                value,
                runtimeType,
                options);
        }
    }
}
