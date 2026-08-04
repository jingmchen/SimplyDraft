using SimplyDraft.Core.Domains.Scripting.Expressions;

namespace SimplyDraft.Core.Domains.Scripting.Statements;

public sealed class AssignmentStatement : Statement
{
    public string Name {get;}
    public Expression Value {get;}

    public AssignmentStatement(int line, int column, string name, Expression value) : base(line, column)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }
}