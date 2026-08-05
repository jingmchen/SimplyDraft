// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using SimplyDraft.Core.Domains.Scripting.Expressions;

namespace SimplyDraft.Core.Domains.Scripting.Statements;

public sealed class AssignmentStatement : Statement
{
    public string Name {get;}
    public Expression Value {get;}

    public AssignmentStatement(string name, Expression value, int line, int column) : base(line, column)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }
}