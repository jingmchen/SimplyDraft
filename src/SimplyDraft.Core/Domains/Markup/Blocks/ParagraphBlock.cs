// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using SimplyDraft.Core.Domains.Markup.Inlines;
using SimplyDraft.Core.Enums;

namespace SimplyDraft.Core.Domains.Markup.Blocks;

public sealed class ParagraphBlock : Block
{
    public ParagraphKind Kind {get;}
    public int ListLevel {get;}
    public int Number {get;}
    public string HeadingNumber {get;} // string for e.g., "2.1"
    public bool Centered {get;}
    public List<Inline> Term {get;} = [];
    public List<Inline> Inlines {get;} = [];

    public ParagraphBlock(
        ParagraphKind kind,
        int listLevel = 0,
        int number = 0,
        bool centered = false,
        string headingNumber = "")
    {
        ArgumentNullException.ThrowIfNull(headingNumber);
        Kind = kind;
        ListLevel = listLevel;
        Number = number;
        Centered = centered;
        HeadingNumber = headingNumber;
    }
}