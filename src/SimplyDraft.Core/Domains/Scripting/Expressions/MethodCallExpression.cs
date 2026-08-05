// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Core.Domains.Scripting.Expressions;

public sealed class MethodCallExpression : Expression
{
    public string Name {get;}
    public Expression Receiver {get;}
    public IReadOnlyList<Expression> Args {get;}
    public override IEnumerable<Expression> SubExpressions => [Receiver, .. Args];

    public MethodCallExpression(string name, Expression receiver, IReadOnlyList<Expression> args, int line, int column) : base(line, column)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        Receiver = receiver ?? throw new ArgumentNullException(nameof(receiver));
        Args = args ?? throw new ArgumentNullException(nameof(args));
    }
}