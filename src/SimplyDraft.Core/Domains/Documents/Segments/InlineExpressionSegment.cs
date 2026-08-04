namespace SimplyDraft.Core.Domains.Document.Segments;

public sealed class InlineExpressionSegment : Segment
{
    public string Source {get; set;} = "";

    public InlineExpressionSegment(int line, int column, string source) : base(line, column)
        => Source = source ?? throw new ArgumentNullException(nameof(source));
}