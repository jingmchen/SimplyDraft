// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Core.Domains.Scripting.Expressions;

public sealed class IndexExpression : Expression
{
    public Expression Target {get;}
    public Expression Index {get;}
    public override IEnumerable<Expression> SubExpressions => [Target, Index];

    public IndexExpression(Expression target, Expression index, int line, int column) : base(line, column)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
        Index = index ?? throw new ArgumentNullException(nameof(index));
    }
}