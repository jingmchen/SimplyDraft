// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Core.Domains.Scripting.Expressions;

public sealed class CallExpression : Expression
{
    public string Name {get;}
    public IReadOnlyList<Expression> Args {get;}
    public override IEnumerable<Expression> SubExpressions => Args;

    public CallExpression(string name, IReadOnlyList<Expression> args, int line, int column) : base(line, column)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        Args = args ?? [];
    }
}