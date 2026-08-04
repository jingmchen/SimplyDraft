namespace SimplyDraft.Core.Domains.Document.Segments;

public abstract class Segment
{
    public int Line {get; set;}
    public int Column {get; set;}

    protected Segment(int line, int column)
    {
        Line = line;
        Column = column;
    }
}