namespace SimplyDraft.Core.Domains.Scripting.Expressions;

public sealed class LiteralExpression : Expression
{
    public Value Value {get;}

    public LiteralExpression(int line, int column, Value value) : base(line, column)
        => Value = value ?? throw new ArgumentNullException(nameof(value));
}