namespace SimplyDraft.Core.Domains.Scripting.Expressions;

public sealed class BuiltinRefExpression : Expression
{
    public string Namespace {get;} = "";
    public string Member {get;} = "";

    public BuiltinRefExpression(int line, int column, string ns, string member) : base(line, column)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ns);
        ArgumentException.ThrowIfNullOrWhiteSpace(member);
        Namespace = ns;
        Member = member;
    }
}