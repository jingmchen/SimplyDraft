// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Core.Domains.Scripting.Expressions;

public sealed class ConditionalExpression : Expression
{
    public Expression Condition {get;}
    public Expression Then {get;}
    public Expression Else {get;}
    public override IEnumerable<Expression> SubExpressions => [Condition, Then, Else];

    public ConditionalExpression(Expression condition, Expression then, Expression el, int line, int column) : base(line, column)
    {
        Condition = condition ?? throw new ArgumentNullException(nameof(condition));
        Then = then ?? throw new ArgumentNullException(nameof(then));
        Else = el ?? throw new ArgumentNullException(nameof(el));
    }
}