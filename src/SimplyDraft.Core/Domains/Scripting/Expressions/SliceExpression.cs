// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Core.Domains.Scripting.Expressions;

public sealed class SliceExpression : Expression
{
    public Expression Target {get;}
    public Expression? Start {get;}
    public Expression? End {get;}
    public override IEnumerable<Expression> SubExpressions
    {
        get
        {
            yield return Target;
            if (Start != null) yield return Start;
            if (End != null) yield return End;
        }
    }

    public SliceExpression(Expression target, Expression? start, Expression? end, int line, int column) : base(line, column)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
        Start = start;
        End = end;
    }
}