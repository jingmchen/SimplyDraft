namespace SimplyDraft.Core.Domains.Document.Segments;

public sealed class ScriptSegment : Segment
{
    public string Source {get;} = "";

    public ScriptSegment(int line, int column, string source) : base(line, column)
        => Source = source ?? throw new ArgumentNullException(nameof(source));
}