// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Core.Domains.Scripting.Expressions;

public sealed class LiteralExpression : Expression
{
    public Value Value {get;}

    public LiteralExpression(Value value, int line, int column) : base(line, column)
        => Value = value ?? throw new ArgumentNullException(nameof(value));
}