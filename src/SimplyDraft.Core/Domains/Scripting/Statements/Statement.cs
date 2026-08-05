// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Core.Domains.Scripting.Statements;

public abstract class Statement : Node
{
    protected Statement(int line, int column) : base(line, column) { }
}