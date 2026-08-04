namespace SimplyDraft.Core.Domains.Document.Segments;

public sealed class LiteralSegment : Segment
{
    public string Text {get; set;} = "";

    public LiteralSegment(int line, int column, string text) : base(line, column)
        => Text = text ?? throw new ArgumentNullException(nameof(text));
}