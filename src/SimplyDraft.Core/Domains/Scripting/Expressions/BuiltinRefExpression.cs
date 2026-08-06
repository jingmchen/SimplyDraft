// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Core.Domains.Scripting.Expressions;

public sealed class BuiltinRefExpression : Expression
{
    public string Namespace {get;}
    public string Member {get;}

    public BuiltinRefExpression(string ns, string member, int line, int column) : base(line, column)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ns);
        ArgumentException.ThrowIfNullOrWhiteSpace(member);
        Namespace = ns;
        Member = member;
    }
}