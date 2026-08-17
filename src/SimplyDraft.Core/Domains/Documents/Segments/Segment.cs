// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Core.Domains.Documents.Segments;

public abstract class Segment
{
    public int Line {get; set;}
    public int Column {get; set;}

    protected Segment(int line, int column)
    {
        Line = line;
        Column = column;
    }
}