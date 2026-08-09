using System.Text.Json.Serialization;

namespace NodeEditor.Net.Records
{
    public record CustomEditorHint(string EditorHint)
    {
        public string EditorHint { get; set; } = EditorHint;
        //public virtual object? Metadata { get; set; }
    }
    public record CustomEditorHint<TMeta>(string EditorHint, TMeta Metadata) : CustomEditorHint(EditorHint)
    {
        public TMeta Metadata { get; set; } = Metadata;
    }


    public static class DefaultCustomEditors
    {
        extension(CustomEditorHint hint)
        {
            public static CustomEditorHint Error => new CustomEditorHint("Error");
        }
    }
}
