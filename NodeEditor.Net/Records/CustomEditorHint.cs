using NodeEditor.Net.Attributes;
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

    [NodeEditorHint("Error", typeof(ErrorOptions))]
    public record ErrorOptions
    {
        public string Message { get; set; } = "Error";

        // 1. Explicitly override the strongly-typed virtual Equals
        public virtual bool Equals(ErrorOptions? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            // Custom rule: Users are equal if they share the same ID
            return true;
        }

        // 2. Always override GetHashCode when overriding Equals
        public override int GetHashCode()
        {
            return this.GetHashCode();
        }
    }
}
