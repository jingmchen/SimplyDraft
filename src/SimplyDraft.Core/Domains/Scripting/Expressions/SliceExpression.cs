using Avalonia.Controls.Shapes;

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

    public SliceExpression(int line, int column, Expression target, Expression? start, Expression? end) : base(line, column)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
        Start = start;
        End = end;
    }
}