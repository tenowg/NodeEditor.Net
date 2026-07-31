using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace NodeEditor.Net.Services.Serialization
{
    public interface IGraphSerializerPlugin
    {
        void Configure(JsonSerializerOptions options);
    }
}
