namespace NodeEditor.Net.Records
{
    public record CustomEditorHint(string EditorHint)
    {
        public string EditorHint { get; set; } = EditorHint;
    }
    public record CustomEditorHint<TMeta>(string EditorHint, TMeta Metadata) : CustomEditorHint(EditorHint)
    {
        public TMeta Metadata { get; set; } = Metadata;
    }
}
