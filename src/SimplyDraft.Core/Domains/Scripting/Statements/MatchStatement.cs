// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using SimplyDraft.Core.Domains.Scripting.Expressions;

namespace SimplyDraft.Core.Domains.Scripting.Statements;

public sealed class MatchStatement : Statement
{
    public Expression Subject {get;}
    public IReadOnlyList<MatchCase> Cases {get;}

    public MatchStatement(Expression subject, IReadOnlyList<MatchCase> cases, int line, int column) : base(line, column)
    {
        Subject = subject ?? throw new ArgumentNullException(nameof(subject));
        Cases = cases ?? throw new ArgumentNullException(nameof(cases));
    }
}