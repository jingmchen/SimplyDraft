// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using SimplyDraft.Core.Enums;

namespace SimplyDraft.Core.Domains.Scripting.Expressions;

public sealed class UnaryExpression : Expression
{
    public UnaryOperator Op {get;}
    public Expression Operand {get;}
    public override IEnumerable<Expression> SubExpressions => [Operand];

    public UnaryExpression(UnaryOperator op, Expression operand, int line, int column) : base(line, column)
    {
        Op = op;
        Operand = operand ?? throw new ArgumentNullException(nameof(operand));
    }
}