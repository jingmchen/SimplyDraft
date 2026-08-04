namespace SimplyDraft.Core.Domains.Scripting.Expressions;

public sealed class CallExpression : Expression
{
    public string Name {get;} = "";
    public IReadOnlyList<Expression> Args {get;} = [];
    public override IEnumerable<Expression> SubExpressions => Args;

    public CallExpression(int line, int column, string name, IReadOnlyList<Expression> args) : base(line, column)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        Args = args ?? [];
    }
}