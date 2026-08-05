// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Core.Domains.Scripting.Expressions;

public sealed class NameExpression : Expression
{
    public string Name {get;}

    public NameExpression(string name, int line, int column) : base(line, column)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }
}