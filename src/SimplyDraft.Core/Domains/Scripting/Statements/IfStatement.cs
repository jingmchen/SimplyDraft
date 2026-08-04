using SimplyDraft.Core.Domains.Scripting.Expressions;

namespace SimplyDraft.Core.Domains.Scripting.Statements;

public sealed class IfStatement : Statement
{
    public List<(Expression? Condition, List<Statement> Body)> Branches {get;} = [];

    public IfStatement(int line, int column) : base(line, column) { }
}