namespace SimplyDraft.Core.Domains.Scripting;

public abstract class Node
{
    public int Line {get; set;}
    public int Column {get; set;}

    protected Node(int line, int column)
    {
        Line = line;
        Column = column;
    }
}