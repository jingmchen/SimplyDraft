using SimplyDraft.Core.Enums;

namespace SimplyDraft.Core.Domains.Scripting.Expressions;

public sealed class UnaryExpression : Expression
{
    public UnaryOperator Op {get;}
    public Expression Operand {get;}
    public override IEnumerable<Expression> SubExpressions => [Operand];

    public UnaryExpression(int line, int column, UnaryOperator op, Expression operand) : base(line, column)
    {
        Op = op;
        Operand = operand ?? throw new ArgumentNullException(nameof(operand));
    }
}