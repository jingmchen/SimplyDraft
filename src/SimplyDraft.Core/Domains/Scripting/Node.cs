// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Core.Domains.Scripting;

public abstract class Node
{
    public int Line {get; set;}
    public int Column {get; set;}

    protected Node(int line, int column)
    {
        Line = line;
        Column = column;
    }
}