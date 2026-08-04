namespace SimplyDraft.Core.Domains.Scripting.Expressions;

public sealed class IndexExpression : Expression
{
    public Expression Target {get;}
    public Expression Index {get;}
    public override IEnumerable<Expression> SubExpressions => [Target, Index];

    public IndexExpression(int line, int column, Expression target, Expression index) : base(line, column)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
        Index = index ?? throw new ArgumentNullException(nameof(index));
    }
}