namespace SimplyDraft.Core.Domains.Scripting.Statements;

public abstract class Statement : Node
{
    protected Statement(int line, int column) : base(line, column) { }
}