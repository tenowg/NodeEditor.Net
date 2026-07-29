using System;
using System.Collections.Generic;
using System.Text;

namespace NodeEditor.Net.Records
{
    public record CustomEditorRecord(string EditorHint);

    public static class CustomEditorExtension
    {
        extension(CustomEditorRecord record)
        {
            public static CustomEditorRecord Test => new CustomEditorRecord("test");
        }
    }
}
