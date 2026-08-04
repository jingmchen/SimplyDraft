namespace SimplyDraft.Core.Domains.Scripting.Expressions;

public sealed class NameExpression : Expression
{
    public string Name {get;} = "";

    public NameExpression(int line, int column, string name) : base(line, column)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }
}