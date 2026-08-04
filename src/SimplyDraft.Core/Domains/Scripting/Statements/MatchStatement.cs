using SimplyDraft.Core.Domains.Scripting.Expressions;

namespace SimplyDraft.Core.Domains.Scripting.Statements;

public sealed class MatchStatement : Statement
{
    public Expression Subject {get;}
    public IReadOnlyList<MatchCase> Cases {get;}

    public MatchStatement(int line, int column, Expression subject, IReadOnlyList<MatchCase> cases) : base(line, column)
    {
        Subject = subject ?? throw new ArgumentNullException(nameof(subject));
        Cases = cases ?? throw new ArgumentNullException(nameof(cases));
    }
}