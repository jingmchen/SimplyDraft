// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using SimplyDraft.Core.Enums;

namespace SimplyDraft.Core.Domains.Scripting.Expressions;

public sealed class BinaryExpression : Expression
{
    public BinaryOperator Op {get;}
    public Expression Left {get;}
    public Expression Right {get;}
    public override IEnumerable<Expression> SubExpressions => [Left, Right];

    public BinaryExpression(BinaryOperator op, Expression left, Expression right, int line, int column) : base(line, column)
    {
        Op = op;
        Left = left ?? throw new ArgumentNullException(nameof(left));
        Right = right ?? throw new ArgumentNullException(nameof(right));
    }
}