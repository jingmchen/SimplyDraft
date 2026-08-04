namespace SimplyDraft.Core.Domains.Scripting.Expressions;

public sealed class ConditionalExpression : Expression
{
    public Expression Condition {get;}
    public Expression Then {get;}
    public Expression Else {get;}
    public override IEnumerable<Expression> SubExpressions => [Condition, Then, Else];

    public ConditionalExpression(int line, int column, Expression condition, Expression then, Expression el) : base(line, column)
    {
        Condition = condition ?? throw new ArgumentNullException(nameof(condition));
        Then = then ?? throw new ArgumentNullException(nameof(then));
        Else = el ?? throw new ArgumentNullException(nameof(el));
    }
}