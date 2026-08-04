namespace SimplyDraft.Core.Domains.Scripting.Expressions;

public abstract class Expression : Node
{
    public virtual IEnumerable<Expression> SubExpressions {get;} = [];

    protected Expression(int line, int column) : base(line, column) { }
}